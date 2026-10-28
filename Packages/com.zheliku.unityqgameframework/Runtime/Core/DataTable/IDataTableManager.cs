using System;
using System.Collections.Generic;

namespace UnityQGameFramework.Core
{
    /// <summary>
    /// 按实际表类型与可选名称管理数据表。
    /// </summary>
    public interface IDataTableManager
    {
        /// <summary>
        /// 获取数据表数量。
        /// </summary>
        int Count
        {
            get;
        }

        /// <summary>
        /// 添加已构造的数据表，存在冲突时不添加任何表。
        /// </summary>
        /// <param name="tables">已构造的数据表；此方法不读取文件或解析数据。</param>
        void AddDataTables(IEnumerable<DataTableBase> tables);

        /// <summary>
        /// 检查是否存在数据表。
        /// </summary>
        /// <typeparam name="T">数据表的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        bool HasDataTable<T>(string name = null) where T : DataTableBase;

        /// <summary>
        /// 检查是否存在数据表。
        /// </summary>
        /// <param name="tableType">数据表类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        bool HasDataTable(Type tableType, string name = null);

        /// <summary>
        /// 取得表对象本身；尚未登记时返回 null。
        /// </summary>
        /// <typeparam name="T">数据表的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>已登记的数据表，未登记时返回 null。</returns>
        T GetDataTable<T>(string name = null) where T : DataTableBase;

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <param name="tableType">数据表类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>已登记的数据表，未登记时返回 null。</returns>
        DataTableBase GetDataTable(Type tableType, string name = null);

        /// <summary>
        /// 获取所有数据表。
        /// </summary>
        /// <returns>所有数据表。</returns>
        DataTableBase[] GetAllDataTables();

        /// <summary>
        /// 获取所有数据表。
        /// </summary>
        /// <param name="results">接收所有数据表的结果列表；填充前清空，容量不足时仍会扩容。</param>
        void GetAllDataTablesNonAlloc(List<DataTableBase> results);

        /// <summary>
        /// 移除数据表登记。
        /// </summary>
        /// <typeparam name="T">数据表的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否成功移除数据表登记。</returns>
        bool RemoveDataTable<T>(string name = null) where T : DataTableBase;

        /// <summary>
        /// 移除数据表登记。
        /// </summary>
        /// <param name="tableType">数据表类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否成功移除数据表登记。</returns>
        bool RemoveDataTable(Type tableType, string name = null);

        /// <summary>
        /// 只移除相同的已登记实例，不影响同名替代对象。
        /// </summary>
        /// <param name="table">要移除的数据表实例。</param>
        /// <returns>是否成功移除数据表登记。</returns>
        bool RemoveDataTable(DataTableBase table);
    }
}
