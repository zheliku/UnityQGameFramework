using System;
using System.Collections.Generic;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.DataTable;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Luban;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 加载并登记 Luban 生成表，提供统一的强类型查询入口。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    [AddComponentMenu("Unity Q Game Framework/Data Table")]
    public sealed class DataTableComponent : FrameworkComponent
    {
        private IDataTableManager m_DataTableManager;
        private ResourceComponent m_ResourceComponent;
        private EventComponent m_EventComponent;
        private int m_LoadSerial;
        private LubanTableSet m_LoadedTables;

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
        public bool IsLoading { get; private set; }

        /// <summary>
        /// 是否已发布完整表集；业务查询仍通过 GetDataTable。
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                return m_LoadedTables != null;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            m_DataTableManager = FrameworkEntry.GetModule<IDataTableManager>();
        }

        private void Start()
        {
            m_ResourceComponent = GameEntry.GetComponent<ResourceComponent>();
            m_EventComponent = GameEntry.GetComponent<EventComponent>();
        }

        /// <summary>
        /// 从 Inspector 引用同步构造并发布表，结果通知通过 Event 队列分发。
        /// </summary>
        /// <typeparam name="T">数据表的类型。</typeparam>
        /// <param name="factory">数据表集合的构造方法。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>已加载的数据表集合，解析失败时返回空。</returns>
        public T LoadDataTables<T>(Func<Func<string, ByteBuf>, T> factory, object userData = null) where T : LubanTableSet
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
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
            T tables;
            try
            {
                tables = PublishTables(m_LubanCatalog, factory);
            }
            catch (Exception exception)
            {
                Fail(m_LubanCatalog.name, exception.Message, userData);
                return null;
            }
            m_EventComponent.Fire(this, LoadDataTableSuccessEventArgs.Create(m_LubanCatalog.name, 0f, userData));
            return tables;
        }

        private T PublishTables<T>(LubanDataTableCatalog catalog, Func<Func<string, ByteBuf>, T> factory) where T : LubanTableSet
        {
            Dictionary<string, TextAsset> assets = catalog.GetAssets();
            var buffers = new List<ByteBuf>();
            T tables = factory(file =>
            {
                if (!assets.TryGetValue(file, out TextAsset bytes))
                {
                    throw new InvalidOperationException("Missing Luban data file: " + file);
                }
                var buffer = new ByteBuf(bytes.bytes);
                buffers.Add(buffer);
                return buffer;
            });
            if (tables == null || tables.SchemaHash != catalog.SchemaHash)
            {
                throw new InvalidOperationException("Luban schema and generated C# do not match. Regenerate the configuration.");
            }
            foreach (ByteBuf buffer in buffers)
                if (buffer.NotEmpty)
                {
                    throw new InvalidOperationException("Trailing bytes in Luban data. Check the generated C# version.");
                }
            m_DataTableManager.RegisterDataTables(tables.DataTables);
            m_LoadedTables = tables;
            return tables;
        }

        /// <summary>
        /// 整批登记已构造的表对象；任意表冲突时不登记整批。
        /// </summary>
        /// <param name="tables">要登记的数据表。</param>
        public void RegisterDataTables(IEnumerable<DataTableBase> tables)
        {
            m_DataTableManager.RegisterDataTables(tables);
        }

        /// <summary>
        /// 异步加载 Catalog，解析 Luban 引用后发布整组表。
        /// </summary>
        /// <typeparam name="T">数据表的类型。</typeparam>
        /// <param name="catalogAssetName">数据表目录资源名称。</param>
        /// <param name="factory">数据表集合的构造方法。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <param name="priority">加载资源的优先级。</param>
        /// <returns>本次加载请求的序号。</returns>
        public int LoadDataTables<T>(string catalogAssetName, Func<Func<string, ByteBuf>, T> factory,
            object userData = null, int priority = 0) where T : LubanTableSet
        {
            if (string.IsNullOrWhiteSpace(catalogAssetName))
            {
                throw new ArgumentException("Catalog asset name is required.", nameof(catalogAssetName));
            }
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
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
            IsLoading = true;
            ResourceComponent resource = m_ResourceComponent;
            try
            {
                resource.LoadAsset(catalogAssetName, typeof(LubanDataTableCatalog), priority, new LoadAssetCallbacks(
                (assetName, asset, duration, context) =>
                {
                    try
                    {
                        if (this == null || serial != m_LoadSerial)
                        {
                            return;
                        }
                        var catalog = asset as LubanDataTableCatalog;
                        if (catalog == null)
                        {
                            throw new InvalidOperationException("The asset is not a Luban data-table catalog.");
                        }
                        PublishTables(catalog, factory);
                        IsLoading = false;
                        m_EventComponent.Fire(this, LoadDataTableSuccessEventArgs.Create(assetName, duration, userData));
                    }
                    catch (Exception exception)
                    {
                        if (this != null && serial == m_LoadSerial)
                        {
                            Fail(assetName, exception.Message, userData);
                        }
                    }
                    finally { resource.UnloadAsset(asset); }
                },
                (assetName, status, error, context) =>
                {
                    if (this != null && serial == m_LoadSerial)
                    {
                        Fail(assetName, error, userData);
                    }
                }), userData);
            }
            catch
            {
                IsLoading = false;
                throw;
            }
            return serial;
        }

        /// <summary>
        /// 使待完成请求失效；迟到回调释放资产，不再发布表或结果事件。
        /// </summary>
        public void CancelLoadDataTables()
        {
            ++m_LoadSerial;
            IsLoading = false;
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
            foreach (DataTableBase table in m_LoadedTables.DataTables)
            {
                if (ReferenceEquals(m_DataTableManager.GetDataTable(table.Type, table.Name), table))
                    m_DataTableManager.DestroyDataTable(table);
            }
            m_LoadedTables = null;
        }

        private void OnDestroy()
        {
            UnloadDataTables();
        }

        private void Fail(string assetName, string error, object userData)
        {
            IsLoading = false;
            Log.Warning("Load Luban data tables failed: {0}: {1}", assetName, error);
            m_EventComponent.Fire(this, LoadDataTableFailureEventArgs.Create(assetName, error, userData));
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
        /// <returns>要获取的数据表，未登记时返回空。</returns>
        public T GetDataTable<T>(string name = null) where T : DataTableBase
        {
            return m_DataTableManager.GetDataTable<T>(name);
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <param name="tableType">数据表类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>要获取的数据表，未登记时返回空。</returns>
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
        /// <param name="results">所有数据表。</param>
        public void GetAllDataTables(List<DataTableBase> results)
        {
            m_DataTableManager.GetAllDataTables(results);
        }

        /// <summary>
        /// 移除登记，不改写外部持有的表及记录；切换整组配置用 UnloadDataTables。
        /// </summary>
        /// <typeparam name="T">数据表的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否成功移除数据表登记。</returns>
        public bool DestroyDataTable<T>(string name = null) where T : DataTableBase
        {
            return m_DataTableManager.DestroyDataTable<T>(name);
        }

        /// <summary>
        /// 移除数据表登记。
        /// </summary>
        /// <param name="tableType">数据表类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否成功移除数据表登记。</returns>
        public bool DestroyDataTable(Type tableType, string name = null)
        {
            return m_DataTableManager.DestroyDataTable(tableType, name);
        }

        /// <summary>
        /// 移除指定实例的数据表登记。
        /// </summary>
        /// <param name="table">要移除的数据表实例。</param>
        /// <returns>是否成功移除数据表登记。</returns>
        public bool DestroyDataTable(DataTableBase table)
        {
            return m_DataTableManager.DestroyDataTable(table);
        }
    }
}
