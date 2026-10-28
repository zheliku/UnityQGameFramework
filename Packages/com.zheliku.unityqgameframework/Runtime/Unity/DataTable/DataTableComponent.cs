using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Luban;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 加载并登记 Luban 生成表，提供统一的强类型查询入口。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    [AddComponentMenu("Unity Q Game Framework/Data Table")]
    public sealed class DataTableComponent : GameFrameworkComponent
    {
        private IDataTableManager m_DataTableManager;
        private ResourceComponent m_ResourceComponent;
        private EventComponent m_EventComponent;
        private int m_LoadSerial;
        private IReadOnlyList<DataTableBase> m_LoadedTables;
        private LoadRequest m_LoadRequest;

        [SerializeField] private LubanDataTableCatalog m_LubanCatalog;

        /// <summary>
        /// Inspector 指定的 Catalog，引用随资产目录移动。
        /// </summary>
        public LubanDataTableCatalog Catalog
        {
            get
            {
                return m_LubanCatalog;
            }
        }

        /// <summary>
        /// 已登记的表数量，包含 map、list、one 和单独登记的具名表。
        /// </summary>
        public int Count
        {
            get
            {
                return m_DataTableManager?.Count ?? 0;
            }
        }

        /// <summary>
        /// 是否正在通过资源模块加载 Catalog。
        /// </summary>
        public bool IsLoading
        {
            get; private set;
        }

        /// <summary>
        /// 是否已完成整组表的加载。
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                return m_LoadedTables != null;
            }
        }

        /// <summary>
        /// 初始化数据表管理器并注册组件。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            m_DataTableManager = GameFrameworkEntry.GetModule<IDataTableManager>();
        }

        /// <summary>
        /// 获取数据表加载所需的资源和事件组件。
        /// </summary>
        private void Start()
        {
            m_ResourceComponent = GameEntry.GetComponent<ResourceComponent>();
            m_EventComponent = GameEntry.GetComponent<EventComponent>();
        }

        /// <summary>
        /// 同步加载 Inspector 指定的 Catalog，结果通知通过 Event 队列分发。
        /// </summary>
        /// <typeparam name="T">生成的数据表加载器类型。</typeparam>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否成功加载数据表。</returns>
        public bool LoadDataTables<T>(object userData = null) where T : IDataTableLoader, new()
        {
            if (m_LubanCatalog == null)
            {
                throw new InvalidOperationException("Assign a Luban catalog on the DataTable component.");
            }
            if (m_EventComponent == null)
            {
                throw new InvalidOperationException("DataTableComponent has not started.");
            }
            if (IsLoading)
            {
                throw new InvalidOperationException("A data-table load is already in progress.");
            }
            if (m_LoadedTables != null)
            {
                throw new InvalidOperationException("Unload the current table set before loading another one.");
            }
            try
            {
                LoadTablesFromCatalog<T>(m_LubanCatalog);
            }
            catch (Exception exception)
            {
                Fail(m_LubanCatalog.name, exception.Message, userData);
                return false;
            }
            m_EventComponent.Fire(this, LoadDataTableSuccessEventArgs.Create(m_LubanCatalog.name, 0f, userData));
            return true;
        }

        /// <summary>
        /// 解析并校验 Catalog 中的数据表，成功后一次性加入管理器。
        /// </summary>
        /// <typeparam name="T">Luban 生成的数据表加载器类型。</typeparam>
        /// <param name="catalog">生成的数据表目录及二进制数据。</param>
        /// <param name="request">当前异步加载请求；直接加载时可为空。</param>
        private void LoadTablesFromCatalog<T>(LubanDataTableCatalog catalog, LoadRequest request = null) where T : IDataTableLoader, new()
        {
            IDataTableLoader tableLoader = new T();
            if (tableLoader.SchemaHash != catalog.SchemaHash)
            {
                throw new InvalidOperationException("Luban schema and generated C# do not match. Regenerate the configuration.");
            }
            Dictionary<string, TextAsset> assets = catalog.GetAssets();
            var buffers = new List<ByteBuf>();
            IReadOnlyList<DataTableBase> tables = tableLoader.CreateTables(file =>
            {
                if (!assets.TryGetValue(file, out TextAsset bytes))
                {
                    throw new InvalidOperationException("Missing Luban data file: " + file);
                }
                var buffer = new ByteBuf(bytes.bytes);
                buffers.Add(buffer);
                return buffer;
            });
            if (tables == null)
            {
                throw new InvalidOperationException("The data-table loader returned no tables.");
            }
            foreach (ByteBuf buffer in buffers)
            {
                if (buffer.NotEmpty)
                {
                    throw new InvalidOperationException("Trailing bytes in Luban data. Check the generated C# version.");
                }
            }
            if (request != null)
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                if (!ReferenceEquals(m_LoadRequest, request))
                {
                    throw new OperationCanceledException("The data-table request was cancelled.");
                }
            }
            m_DataTableManager.AddDataTables(tables);
            m_LoadedTables = tables;
        }

        /// <summary>
        /// 添加已构造的数据表，存在冲突时不添加任何表。
        /// </summary>
        /// <param name="tables">已构造的数据表；不读取文件，不改变整组表的加载状态。</param>
        public void AddDataTables(IEnumerable<DataTableBase> tables)
        {
            m_DataTableManager.AddDataTables(tables);
        }

        /// <summary>
        /// 异步加载 Catalog，解析整组数据表及表间引用。
        /// </summary>
        /// <typeparam name="T">生成的数据表加载器类型。</typeparam>
        /// <param name="catalogAssetName">数据表目录资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="priority">加载资源的优先级。</param>
        /// <returns>本次加载请求的序号。</returns>
        public int LoadDataTables<T>(string catalogAssetName, object userData = null, int priority = 0) where T : IDataTableLoader, new()
        {
            return LoadDataTables<T>(catalogAssetName, userData, priority, new LoadRequest());
        }

        /// <summary>
        /// 异步加载并登记 Luban 数据表。
        /// </summary>
        /// <typeparam name="T">生成的数据表加载器类型。</typeparam>
        /// <param name="catalogAssetName">数据表目录资源名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="priority">加载资源的优先级。</param>
        /// <param name="cancellationToken">取消标记，取消后不登记本次加载的表。</param>
        /// <returns>数据表加载任务。</returns>
        public async UniTask LoadDataTablesAsync<T>(string catalogAssetName, object userData = null,
            int priority = 0, CancellationToken cancellationToken = default) where T : IDataTableLoader, new()
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = new LoadRequest
            {
                CancellationToken = cancellationToken,
                Completion = new UniTaskCompletionSource()
            };
            int serial = LoadDataTables<T>(catalogAssetName, userData, priority, request);
            try
            {
                await ResourceOperation.WithCancellation(request.Completion.Task, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await UniTask.SwitchToMainThread();
                if (this != null && serial == m_LoadSerial && ReferenceEquals(m_LoadRequest, request))
                {
                    CancelLoadDataTables();
                }
                throw;
            }
        }

        /// <summary>
        /// 加载数据表目录，完成后发布整组数据表。
        /// </summary>
        /// <typeparam name="T">Luban 生成的数据表加载器类型。</typeparam>
        /// <param name="catalogAssetName">数据表目录的资源地址。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="priority">资源加载优先级。</param>
        /// <param name="request">当前异步加载请求。</param>
        /// <returns>用于识别本次加载的序列编号。</returns>
        private int LoadDataTables<T>(string catalogAssetName, object userData, int priority, LoadRequest request) where T : IDataTableLoader, new()
        {
            if (string.IsNullOrWhiteSpace(catalogAssetName))
            {
                throw new ArgumentException("Catalog asset name is required.", nameof(catalogAssetName));
            }
            if (m_ResourceComponent == null || m_EventComponent == null)
            {
                throw new InvalidOperationException("DataTableComponent has not started.");
            }
            if (IsLoading)
            {
                throw new InvalidOperationException("A data-table load is already in progress.");
            }
            if (m_LoadedTables != null)
            {
                throw new InvalidOperationException("Unload the current table set before loading another one.");
            }
            int serial = ++m_LoadSerial;
            m_LoadRequest = request;
            IsLoading = true;
            ResourceComponent resource = m_ResourceComponent;
            try
            {
                resource.LoadAsset(catalogAssetName, typeof(LubanDataTableCatalog), priority, new LoadAssetCallbacks(
                (assetName, asset, duration, context) =>
                {
                    Exception error = null;
                    try
                    {
                        if (this == null || serial != m_LoadSerial)
                        {
                            return;
                        }
                        request.CancellationToken.ThrowIfCancellationRequested();
                        var catalog = asset as LubanDataTableCatalog;
                        if (catalog == null)
                        {
                            throw new InvalidOperationException("The asset is not a Luban data-table catalog.");
                        }
                        LoadTablesFromCatalog<T>(catalog, request);
                    }
                    catch (Exception exception)
                    {
                        error = exception;
                    }
                    finally
                    {
                        resource.UnloadAsset(asset);
                    }
                    if (this == null || serial != m_LoadSerial)
                    {
                        return;
                    }
                    if (error is OperationCanceledException)
                    {
                        CancelLoadDataTables();
                    }
                    else if (error != null)
                    {
                        Fail(assetName, error.Message, userData);
                    }
                    else
                    {
                        IsLoading = false;
                        m_LoadRequest = null;
                        m_EventComponent.Fire(this, LoadDataTableSuccessEventArgs.Create(assetName, duration, userData));
                        request.Completion?.TrySetResult();
                    }
                },
                (assetName, status, error, context) =>
                {
                    if (this != null && serial == m_LoadSerial)
                    {
                        if (request.CancellationToken.IsCancellationRequested)
                        {
                            CancelLoadDataTables();
                        }
                        else
                        {
                            Fail(assetName, error, userData);
                        }
                    }
                }), userData);
            }
            catch
            {
                IsLoading = false;
                m_LoadRequest = null;
                throw;
            }
            return serial;
        }

        /// <summary>
        /// 取消当前加载；迟到回调只释放资产，不再添加表或发送结果事件。
        /// </summary>
        public void CancelLoadDataTables()
        {
            ++m_LoadSerial;
            IsLoading = false;
            LoadRequest request = m_LoadRequest;
            m_LoadRequest = null;
            request?.Completion?.TrySetCanceled(request.CancellationToken);
        }

        /// <summary>
        /// 取消待完成请求，卸载当前整组表。
        /// </summary>
        public void UnloadDataTables()
        {
            CancelLoadDataTables();
            if (m_LoadedTables == null)
            {
                return;
            }
            foreach (DataTableBase table in m_LoadedTables)
            {
                if (ReferenceEquals(m_DataTableManager.GetDataTable(table.Type, table.Name), table))
                {
                    m_DataTableManager.RemoveDataTable(table);
                }
            }
            m_LoadedTables = null;
        }

        /// <summary>
        /// 取消未完成请求并卸载当前数据表。
        /// </summary>
        private void OnDestroy()
        {
            UnloadDataTables();
        }

        /// <summary>
        /// 结束数据表加载请求并通知失败。
        /// </summary>
        /// <param name="assetName">数据表目录资源名称。</param>
        /// <param name="error">错误信息。</param>
        /// <param name="userData">用户自定义数据。</param>
        private void Fail(string assetName, string error, object userData)
        {
            IsLoading = false;
            LoadRequest request = m_LoadRequest;
            m_LoadRequest = null;
            request?.Completion?.TrySetException(new GameFrameworkException(error));
            Log.Warning("Load Luban data tables failed: {0}: {1}", assetName, error);
            m_EventComponent.Fire(this, LoadDataTableFailureEventArgs.Create(assetName, error, userData));
        }

        /// <summary>
        /// 数据表加载请求。
        /// </summary>
        private sealed class LoadRequest
        {
            /// <summary>
            /// 本次表集加载的取消令牌。
            /// </summary>
            internal CancellationToken CancellationToken;

            /// <summary>
            /// 向异步调用方通知表集加载结果。
            /// </summary>
            internal UniTaskCompletionSource Completion;
        }

        /// <summary>
        /// 检查实际表类型，例如 HasDataTable&lt;TbItem&gt;()。
        /// </summary>
        /// <typeparam name="T">数据表的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable<T>(string name = null) where T : DataTableBase
        {
            return m_DataTableManager.HasDataTable<T>(name);
        }

        /// <summary>
        /// 检查是否存在数据表。
        /// </summary>
        /// <param name="tableType">数据表类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable(Type tableType, string name = null)
        {
            return m_DataTableManager.HasDataTable(tableType, name);
        }

        /// <summary>
        /// 取得生成的表对象本身；尚未加载或登记时返回 null。
        /// </summary>
        /// <typeparam name="T">数据表的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>已登记的数据表，未登记时返回 null。</returns>
        public T GetDataTable<T>(string name = null) where T : DataTableBase
        {
            return m_DataTableManager.GetDataTable<T>(name);
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <param name="tableType">数据表类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>已登记的数据表，未登记时返回 null。</returns>
        public DataTableBase GetDataTable(Type tableType, string name = null)
        {
            return m_DataTableManager.GetDataTable(tableType, name);
        }

        /// <summary>
        /// 获取所有数据表。
        /// </summary>
        /// <returns>所有数据表。</returns>
        public DataTableBase[] GetAllDataTables()
        {
            return m_DataTableManager.GetAllDataTables();
        }

        /// <summary>
        /// 获取所有数据表。
        /// </summary>
        /// <param name="results">接收所有数据表的结果列表；填充前清空，容量不足时仍会扩容。</param>
        public void GetAllDataTablesNonAlloc(List<DataTableBase> results)
        {
            m_DataTableManager.GetAllDataTablesNonAlloc(results);
        }

        /// <summary>
        /// 移除登记，不改写外部持有的表及记录；切换整组配置用 UnloadDataTables。
        /// </summary>
        /// <typeparam name="T">数据表的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否成功移除数据表登记。</returns>
        public bool RemoveDataTable<T>(string name = null) where T : DataTableBase
        {
            return m_DataTableManager.RemoveDataTable<T>(name);
        }

        /// <summary>
        /// 移除数据表登记。
        /// </summary>
        /// <param name="tableType">数据表类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否成功移除数据表登记。</returns>
        public bool RemoveDataTable(Type tableType, string name = null)
        {
            return m_DataTableManager.RemoveDataTable(tableType, name);
        }

        /// <summary>
        /// 移除指定实例的数据表登记。
        /// </summary>
        /// <param name="table">要移除的数据表实例。</param>
        /// <returns>是否成功移除数据表登记。</returns>
        public bool RemoveDataTable(DataTableBase table)
        {
            return m_DataTableManager.RemoveDataTable(table);
        }
    }
}
