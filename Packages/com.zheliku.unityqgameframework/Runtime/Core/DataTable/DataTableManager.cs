//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace UnityQGameFramework.Core.DataTable
{
    /// <summary>
    /// 数据表管理器。
    /// </summary>
    internal sealed class DataTableManager : FrameworkModule, IDataTableManager
    {
        private readonly Dictionary<TypeNamePair, DataTableBase> m_DataTables;

        /// <summary>
        /// 初始化数据表管理器的新实例。
        /// </summary>
        public DataTableManager()
        {
            m_DataTables = new Dictionary<TypeNamePair, DataTableBase>();
        }

        /// <summary>
        /// 获取数据表数量。
        /// </summary>
        public int Count => m_DataTables.Count;

        /// <summary>
        /// 数据表管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
        }

        /// <summary>
        /// 关闭并清理数据表管理器。
        /// </summary>
        internal override void Shutdown()
        {
            foreach (KeyValuePair<TypeNamePair, DataTableBase> dataTable in m_DataTables)
            {
                dataTable.Value.Shutdown();
            }

            m_DataTables.Clear();
        }

        /// <summary>Validate the entire batch before exposing any table to callers.</summary>
        public void RegisterDataTables(IEnumerable<DataTableBase> tables)
        {
            if (tables == null) throw new ArgumentNullException(nameof(tables));
            var pending = new Dictionary<TypeNamePair, DataTableBase>();
            foreach (DataTableBase table in tables)
            {
                if (table == null) throw new FrameworkException("Data table is invalid.");
                var key = new TypeNamePair(table.Type, table.Name);
                if (m_DataTables.ContainsKey(key) || pending.ContainsKey(key))
                    throw new FrameworkException("Data table already exists: " + key);
                pending.Add(key, table);
            }
            foreach (var table in pending) m_DataTables.Add(table.Key, table.Value);
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable<T>() where T : IDataRow
        {
            return InternalHasDataTable(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable(Type dataRowType)
        {
            if (dataRowType == null)
            {
                throw new FrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new FrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalHasDataTable(new TypeNamePair(dataRowType));
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable<T>(string name) where T : IDataRow
        {
            return InternalHasDataTable(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable(Type dataRowType, string name)
        {
            if (dataRowType == null)
            {
                throw new FrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new FrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalHasDataTable(new TypeNamePair(dataRowType, name));
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>要获取的数据表。</returns>
        public IDataTable<T> GetDataTable<T>() where T : IDataRow
        {
            return (IDataTable<T>)InternalGetDataTable(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>要获取的数据表。</returns>
        public DataTableBase GetDataTable(Type dataRowType)
        {
            if (dataRowType == null)
            {
                throw new FrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new FrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalGetDataTable(new TypeNamePair(dataRowType));
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>要获取的数据表。</returns>
        public IDataTable<T> GetDataTable<T>(string name) where T : IDataRow
        {
            return (IDataTable<T>)InternalGetDataTable(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>要获取的数据表。</returns>
        public DataTableBase GetDataTable(Type dataRowType, string name)
        {
            if (dataRowType == null)
            {
                throw new FrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new FrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalGetDataTable(new TypeNamePair(dataRowType, name));
        }

        /// <summary>
        /// 获取所有数据表。
        /// </summary>
        /// <returns>所有数据表。</returns>
        public DataTableBase[] GetAllDataTables()
        {
            int index = 0;
            DataTableBase[] results = new DataTableBase[m_DataTables.Count];
            foreach (KeyValuePair<TypeNamePair, DataTableBase> dataTable in m_DataTables)
            {
                results[index++] = dataTable.Value;
            }

            return results;
        }

        /// <summary>
        /// 获取所有数据表。
        /// </summary>
        /// <param name="results">所有数据表。</param>
        public void GetAllDataTables(List<DataTableBase> results)
        {
            if (results == null)
            {
                throw new FrameworkException("Results is invalid.");
            }

            results.Clear();
            foreach (KeyValuePair<TypeNamePair, DataTableBase> dataTable in m_DataTables)
            {
                results.Add(dataTable.Value);
            }
        }

        /// <summary>
        /// 创建数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>要创建的数据表。</returns>
        public IDataTable<T> CreateDataTable<T>() where T : class, IDataRow
        {
            return CreateDataTable<T>(string.Empty);
        }

        /// <summary>
        /// 创建数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>要创建的数据表。</returns>
        public DataTableBase CreateDataTable(Type dataRowType)
        {
            return CreateDataTable(dataRowType, string.Empty);
        }

        /// <summary>
        /// 创建数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>要创建的数据表。</returns>
        public IDataTable<T> CreateDataTable<T>(string name) where T : class, IDataRow
        {
            TypeNamePair typeNamePair = new TypeNamePair(typeof(T), name);
            if (HasDataTable<T>(name))
            {
                throw new FrameworkException(Utility.Text.Format("Already exist data table '{0}'.", typeNamePair));
            }

            DataTable<T> dataTable = new DataTable<T>(name);
            m_DataTables.Add(typeNamePair, dataTable);
            return dataTable;
        }

        /// <summary>
        /// 创建数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>要创建的数据表。</returns>
        public DataTableBase CreateDataTable(Type dataRowType, string name)
        {
            if (dataRowType == null)
            {
                throw new FrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new FrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            TypeNamePair typeNamePair = new TypeNamePair(dataRowType, name);
            if (HasDataTable(dataRowType, name))
            {
                throw new FrameworkException(Utility.Text.Format("Already exist data table '{0}'.", typeNamePair));
            }

            Type dataTableType = typeof(DataTable<>).MakeGenericType(dataRowType);
            DataTableBase dataTable = (DataTableBase)Activator.CreateInstance(dataTableType, new object[] { name, null });
            m_DataTables.Add(typeNamePair, dataTable);
            return dataTable;
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        public bool DestroyDataTable<T>() where T : IDataRow
        {
            return InternalDestroyDataTable(new TypeNamePair(typeof(T)));
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable(Type dataRowType)
        {
            if (dataRowType == null)
            {
                throw new FrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new FrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalDestroyDataTable(new TypeNamePair(dataRowType));
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        public bool DestroyDataTable<T>(string name) where T : IDataRow
        {
            return InternalDestroyDataTable(new TypeNamePair(typeof(T), name));
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable(Type dataRowType, string name)
        {
            if (dataRowType == null)
            {
                throw new FrameworkException("Data row type is invalid.");
            }

            if (!typeof(IDataRow).IsAssignableFrom(dataRowType))
            {
                throw new FrameworkException(Utility.Text.Format("Data row type '{0}' is invalid.", dataRowType.FullName));
            }

            return InternalDestroyDataTable(new TypeNamePair(dataRowType, name));
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="dataTable">要销毁的数据表。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable<T>(IDataTable<T> dataTable) where T : IDataRow
        {
            if (dataTable == null)
            {
                throw new FrameworkException("Data table is invalid.");
            }

            return InternalDestroyDataTable(new TypeNamePair(typeof(T), dataTable.Name));
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <param name="dataTable">要销毁的数据表。</param>
        /// <returns>是否销毁数据表成功。</returns>
        public bool DestroyDataTable(DataTableBase dataTable)
        {
            if (dataTable == null)
            {
                throw new FrameworkException("Data table is invalid.");
            }

            return InternalDestroyDataTable(new TypeNamePair(dataTable.Type, dataTable.Name));
        }

        private bool InternalHasDataTable(TypeNamePair typeNamePair)
        {
            return m_DataTables.ContainsKey(typeNamePair);
        }

        private DataTableBase InternalGetDataTable(TypeNamePair typeNamePair)
        {
            DataTableBase dataTable = null;
            if (m_DataTables.TryGetValue(typeNamePair, out dataTable))
            {
                return dataTable;
            }

            return null;
        }

        private bool InternalDestroyDataTable(TypeNamePair typeNamePair)
        {
            DataTableBase dataTable = null;
            if (m_DataTables.TryGetValue(typeNamePair, out dataTable))
            {
                dataTable.Shutdown();
                return m_DataTables.Remove(typeNamePair);
            }

            return false;
        }
    }
}
