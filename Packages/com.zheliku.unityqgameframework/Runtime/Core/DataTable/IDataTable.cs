using System;
using System.Collections.Generic;

namespace UnityQGameFramework.Core.DataTable
{
    /// <summary>
    /// 所有表共有的遍历、条件查询和排序；键查询保留生成的强类型签名。
    /// </summary>
    /// <typeparam name="T">数据表行的类型。</typeparam>
    public interface IDataTable<T> : IEnumerable<T>
    {
        /// <summary>
        /// 获取数据表名称。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 获取数据表完整名称。
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// 获取数据表类型。
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// 获取数据表行的类型。
        /// </summary>
        Type RowType { get; }

        /// <summary>
        /// 获取数据表行数。
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 检查是否存在符合条件的数据表行。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>是否存在符合条件的数据表行。</returns>
        bool HasDataRow(Predicate<T> condition);

        /// <summary>
        /// 返回首条匹配记录；未匹配时返回 default。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>首条符合条件的数据表行，不存在时返回默认值。</returns>
        T GetDataRow(Predicate<T> condition);

        /// <summary>
        /// 获取符合条件的数据表行。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>新建的结果数组，记录本身仍由原表持有。</returns>
        T[] GetDataRows(Predicate<T> condition);

        /// <summary>
        /// 使用调用方列表获取符合条件的数据表行，不创建结果数组。
        /// </summary>
        /// <param name="condition">筛选条件；复用委托可避免调用方分配。</param>
        /// <param name="results">填充前清空的结果列表；容量不足时仍会扩容，记录仍由原表持有。</param>
        void GetDataRowsNonAlloc(Predicate<T> condition, List<T> results);

        /// <summary>
        /// 获取排序后的数据表行。
        /// </summary>
        /// <param name="comparison">数据表行的排序比较器。</param>
        /// <returns>排序后的新结果数组，不改变源表顺序和记录。</returns>
        T[] GetDataRows(Comparison<T> comparison);

        /// <summary>
        /// 使用调用方列表获取排序后的数据表行，不创建结果数组。
        /// </summary>
        /// <param name="comparison">排序比较器；复用委托可避免调用方分配。</param>
        /// <param name="results">填充前清空的结果列表；容量不足时仍会扩容，排序不改变源表顺序。</param>
        void GetDataRowsNonAlloc(Comparison<T> comparison, List<T> results);

        /// <summary>
        /// 获取排序后的符合条件的数据表行。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <param name="comparison">数据表行的排序比较器。</param>
        /// <returns>筛选并排序后的新结果数组，不改变源表顺序和记录。</returns>
        T[] GetDataRows(Predicate<T> condition, Comparison<T> comparison);

        /// <summary>
        /// 使用调用方列表筛选并排序数据表行，不创建结果数组。
        /// </summary>
        /// <param name="condition">筛选条件；复用委托可避免调用方分配。</param>
        /// <param name="comparison">排序比较器；复用委托可避免调用方分配。</param>
        /// <param name="results">填充前清空的结果列表；容量不足时仍会扩容，排序不改变源表顺序。</param>
        void GetDataRowsNonAlloc(Predicate<T> condition, Comparison<T> comparison, List<T> results);

        /// <summary>
        /// 获取所有数据表行。
        /// </summary>
        /// <returns>按源表顺序排列的新结果数组，记录本身仍由原表持有。</returns>
        T[] GetAllDataRows();

        /// <summary>
        /// 使用调用方列表获取所有数据表行，不创建结果数组。
        /// </summary>
        /// <param name="results">填充前清空的结果列表；容量不足时仍会扩容，记录仍由原表持有。</param>
        void GetAllDataRowsNonAlloc(List<T> results);
    }
}
