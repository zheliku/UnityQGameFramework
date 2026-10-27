//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace UnityQGameFramework.Core.DataTable
{
    /// <summary>
    /// 管理已登记的数据表及其加载状态。
    /// </summary>
    internal sealed class DataTableManager : FrameworkModule, IDataTableManager
    {
        private readonly Dictionary<TypeNamePair, DataTableBase> m_DataTables = new Dictionary<TypeNamePair, DataTableBase>();

        /// <summary>
        /// 获取数据表数量。
        /// </summary>
        public int Count
        {
            get
            {
                return m_DataTables.Count;
            }
        }

        /// <summary>
        /// 更新数据表管理器。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
        }
        // 卸载解除管理器引用，不改写业务仍持有的记录及跨表引用。
        /// <summary>
        /// 清理已注册的数据表。
        /// </summary>
        internal override void Shutdown()
        {
            m_DataTables.Clear();
        }

        /// <summary>
        /// 整批登记数据表，存在冲突时不登记。
        /// </summary>
        /// <param name="tables">要登记的数据表。</param>
        public void RegisterDataTables(IEnumerable<DataTableBase> tables)
        {
            if (tables == null)
            {
                throw new ArgumentNullException(nameof(tables));
            }
            var pending = new Dictionary<TypeNamePair, DataTableBase>();
            foreach (DataTableBase table in tables)
            {
                if (table == null)
                {
                    throw new ArgumentException("A data table cannot be null.", nameof(tables));
                }
                var key = new TypeNamePair(table.Type, table.Name);
                if (m_DataTables.ContainsKey(key) || pending.ContainsKey(key))
                {
                    throw new FrameworkException("Data table already exists: " + key);
                }
                pending.Add(key, table);
            }
            foreach (var pair in pending)
            {
                m_DataTables.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// 检查是否存在数据表。
        /// </summary>
        /// <typeparam name="T">数据表的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable<T>(string name = null) where T : DataTableBase
        {
            return HasDataTable(typeof(T), name);
        }

        /// <summary>
        /// 检查是否存在数据表。
        /// </summary>
        /// <param name="tableType">数据表类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable(Type tableType, string name = null)
        {
            return m_DataTables.ContainsKey(Key(tableType, name));
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <typeparam name="T">数据表的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>已登记的数据表，未登记时返回 null。</returns>
        public T GetDataTable<T>(string name = null) where T : DataTableBase
        {
            return (T)GetDataTable(typeof(T), name);
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <param name="tableType">数据表类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>已登记的数据表，未登记时返回 null。</returns>
        public DataTableBase GetDataTable(Type tableType, string name = null)
        {
            return m_DataTables.TryGetValue(Key(tableType, name), out var table) ? table : null;
        }

        /// <summary>
        /// 获取所有数据表。
        /// </summary>
        /// <returns>所有数据表。</returns>
        public DataTableBase[] GetAllDataTables()
        {
            var results = new DataTableBase[Count];
            m_DataTables.Values.CopyTo(results, 0);
            return results;
        }

        /// <summary>
        /// 获取所有数据表。
        /// </summary>
        /// <param name="results">接收所有数据表的结果列表。</param>
        public void GetAllDataTables(List<DataTableBase> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }
            results.Clear();
            results.AddRange(m_DataTables.Values);
        }

        /// <summary>
        /// 移除数据表登记。
        /// </summary>
        /// <typeparam name="T">数据表的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否成功移除数据表登记。</returns>
        public bool DestroyDataTable<T>(string name = null) where T : DataTableBase
        {
            return DestroyDataTable(typeof(T), name);
        }

        /// <summary>
        /// 移除数据表登记。
        /// </summary>
        /// <param name="tableType">数据表类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否成功移除数据表登记。</returns>
        public bool DestroyDataTable(Type tableType, string name = null)
        {
            return m_DataTables.Remove(Key(tableType, name));
        }

        /// <summary>
        /// 移除数据表登记。
        /// </summary>
        /// <param name="table">要移除的数据表实例。</param>
        /// <returns>是否成功移除数据表登记。</returns>
        public bool DestroyDataTable(DataTableBase table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }
            var key = new TypeNamePair(table.Type, table.Name);
            return m_DataTables.TryGetValue(key, out var current) && ReferenceEquals(current, table) && m_DataTables.Remove(key);
        }

        /// <summary>
        /// 创建数据表的类型名称标识。
        /// </summary>
        /// <param name="tableType">数据表类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>数据表的类型名称标识。</returns>
        private static TypeNamePair Key(Type tableType, string name)
        {
            if (tableType == null)
            {
                throw new ArgumentNullException(nameof(tableType));
            }
            if (!typeof(DataTableBase).IsAssignableFrom(tableType))
            {
                throw new ArgumentException("Expected a data-table type, such as TbItem.", nameof(tableType));
            }
            return new TypeNamePair(tableType, name);
        }
    }
}
