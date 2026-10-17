using System;
using System.Collections.Generic;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.DataTable;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Luban;

namespace UnityQGameFramework.Runtime
{
    /// <summary>QGF data-table facade, backed by generated Luban data.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Unity Q Game Framework/Data Table")]
    public sealed class DataTableComponent : FrameworkComponent
    {
        private IDataTableManager m_DataTableManager;
        private ResourceComponent m_ResourceComponent;
        private EventComponent m_EventComponent;
        private int m_LoadSerial;

        [SerializeField] private LubanDataTableCatalog m_LubanCatalog;
        public LubanDataTableCatalog Catalog => m_LubanCatalog;

        public int Count => m_DataTableManager?.Count ?? 0;
        public bool IsLoading { get; private set; }
        public LubanTableSet LubanTables { get; private set; }

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

        /// <summary>Load the catalog assigned in the Inspector. Its asset references survive folder moves.</summary>
        public void LoadDataTables<T>(Func<Func<string, ByteBuf>, T> factory, object userData = null) where T : LubanTableSet
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (m_LubanCatalog == null) throw new InvalidOperationException("Assign a Luban catalog on the DataTable component.");
            if (m_EventComponent == null) throw new InvalidOperationException("DataTableComponent has not started.");
            if (IsLoading) throw new InvalidOperationException("A data-table load is already in progress.");
            if (LubanTables != null) throw new InvalidOperationException("Unload the current table set before loading another one.");
            try
            {
                PublishTables(m_LubanCatalog, factory);
            }
            catch (Exception exception)
            {
                Fail(m_LubanCatalog.name, exception.Message, userData);
                return;
            }
            m_EventComponent.Fire(this, LoadDataTableSuccessEventArgs.Create(m_LubanCatalog.name, 0f, userData));
        }

        private void PublishTables<T>(LubanDataTableCatalog catalog, Func<Func<string, ByteBuf>, T> factory) where T : LubanTableSet
        {
            Dictionary<string, TextAsset> assets = catalog.GetAssets();
            var buffers = new List<ByteBuf>();
            T tables = factory(file =>
            {
                if (!assets.TryGetValue(file, out TextAsset bytes))
                    throw new InvalidOperationException("Missing Luban data file: " + file);
                var buffer = new ByteBuf(bytes.bytes);
                buffers.Add(buffer);
                return buffer;
            });
            if (tables == null || tables.SchemaHash != catalog.SchemaHash)
                throw new InvalidOperationException("Luban schema and generated C# do not match. Regenerate the configuration.");
            foreach (ByteBuf buffer in buffers)
                if (buffer.NotEmpty) throw new InvalidOperationException("Trailing bytes in Luban data. Check the generated C# version.");
            m_DataTableManager.RegisterDataTables(tables.DataTables);
            LubanTables = tables;
        }

        /// <summary>Load one catalog, resolve all Luban references, then publish the complete set.</summary>
        /// <returns>A request serial ID. Cancellation ignores late callbacks and still releases their assets.</returns>
        public int LoadDataTables<T>(string catalogAssetName, Func<Func<string, ByteBuf>, T> factory,
            object userData = null, int priority = 0) where T : LubanTableSet
        {
            if (string.IsNullOrWhiteSpace(catalogAssetName)) throw new ArgumentException("Catalog asset name is required.", nameof(catalogAssetName));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (m_ResourceComponent == null || m_EventComponent == null) throw new InvalidOperationException("DataTableComponent has not started.");
            if (IsLoading) throw new InvalidOperationException("A data-table load is already in progress.");
            if (LubanTables != null) throw new InvalidOperationException("Unload the current table set before loading another one.");
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
                        if (this == null || serial != m_LoadSerial) return;
                        var catalog = asset as LubanDataTableCatalog;
                        if (catalog == null) throw new InvalidOperationException("The asset is not a Luban data-table catalog.");
                        PublishTables(catalog, factory);
                        IsLoading = false;
                        m_EventComponent.Fire(this, LoadDataTableSuccessEventArgs.Create(assetName, duration, userData));
                    }
                    catch (Exception exception)
                    {
                        if (this != null && serial == m_LoadSerial) Fail(assetName, exception.Message, userData);
                    }
                    finally { resource.UnloadAsset(asset); }
                },
                (assetName, status, error, context) =>
                {
                    if (this != null && serial == m_LoadSerial) Fail(assetName, error, userData);
                }), userData);
            }
            catch
            {
                IsLoading = false;
                throw;
            }
            return serial;
        }

        public void CancelLoadDataTables()
        {
            ++m_LoadSerial;
            IsLoading = false;
        }

        /// <summary>Access advanced Luban indexes through the same component.</summary>
        public T GetLubanTables<T>() where T : LubanTableSet => LubanTables as T;

        /// <summary>Release the complete set, including native cross-table references.</summary>
        public void UnloadDataTables()
        {
            CancelLoadDataTables();
            if (LubanTables == null) return;
            foreach (DataTableBase table in LubanTables.DataTables)
            {
                if (ReferenceEquals(m_DataTableManager.GetDataTable(table.Type, table.Name), table))
                    m_DataTableManager.DestroyDataTable(table);
            }
            LubanTables = null;
        }

        private void OnDestroy() { UnloadDataTables(); }

        private void Fail(string assetName, string error, object userData)
        {
            IsLoading = false;
            Log.Warning("Load Luban data tables failed: {0}: {1}", assetName, error);
            m_EventComponent.Fire(this, LoadDataTableFailureEventArgs.Create(assetName, error, userData));
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable<T>() where T : IDataRow
        {
            return m_DataTableManager.HasDataTable<T>();
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable(Type dataRowType)
        {
            return m_DataTableManager.HasDataTable(dataRowType);
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable<T>(string name) where T : IDataRow
        {
            return m_DataTableManager.HasDataTable<T>(name);
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable(Type dataRowType, string name)
        {
            return m_DataTableManager.HasDataTable(dataRowType, name);
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>要获取的数据表。</returns>
        public IDataTable<T> GetDataTable<T>() where T : IDataRow
        {
            return m_DataTableManager.GetDataTable<T>();
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>要获取的数据表。</returns>
        public DataTableBase GetDataTable(Type dataRowType)
        {
            return m_DataTableManager.GetDataTable(dataRowType);
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>要获取的数据表。</returns>
        public IDataTable<T> GetDataTable<T>(string name) where T : IDataRow
        {
            return m_DataTableManager.GetDataTable<T>(name);
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>要获取的数据表。</returns>
        public DataTableBase GetDataTable(Type dataRowType, string name)
        {
            return m_DataTableManager.GetDataTable(dataRowType, name);
        }

        /// <summary>
        /// 获取所有数据表。
        /// </summary>
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
        /// 创建数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>要创建的数据表。</returns>
        public IDataTable<T> CreateDataTable<T>() where T : class, IDataRow
        {
            return CreateDataTable<T>(null);
        }

        /// <summary>
        /// 创建数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>要创建的数据表。</returns>
        public DataTableBase CreateDataTable(Type dataRowType)
        {
            return CreateDataTable(dataRowType, null);
        }

        /// <summary>
        /// 创建数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>要创建的数据表。</returns>
        public IDataTable<T> CreateDataTable<T>(string name) where T : class, IDataRow
        {
            return m_DataTableManager.CreateDataTable<T>(name);
        }

        public DataTableBase CreateDataTable(Type dataRowType, string name)
        {
            return m_DataTableManager.CreateDataTable(dataRowType, name);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable<T>() where T : IDataRow
        {
            return m_DataTableManager.DestroyDataTable<T>();
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable(Type dataRowType)
        {
            return m_DataTableManager.DestroyDataTable(dataRowType);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable<T>(string name) where T : IDataRow
        {
            return m_DataTableManager.DestroyDataTable<T>(name);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable(Type dataRowType, string name)
        {
            return m_DataTableManager.DestroyDataTable(dataRowType, name);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="dataTable">要销毁的数据表。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable<T>(IDataTable<T> dataTable) where T : IDataRow
        {
            return m_DataTableManager.DestroyDataTable(dataTable);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <param name="dataTable">要销毁的数据表。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable(DataTableBase dataTable)
        {
            return m_DataTableManager.DestroyDataTable(dataTable);
        }

    }
}
