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
        /// <returns>符合查询条件的数据表行。</returns>
        T[] GetDataRows(Predicate<T> condition);

        /// <summary>
        /// 获取符合条件的数据表行。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <param name="results">接收数据表行的结果列表。</param>
        void GetDataRows(Predicate<T> condition, List<T> results);

        /// <summary>
        /// 获取排序后的数据表行。
        /// </summary>
        /// <param name="comparison">数据表行的排序比较器。</param>
        /// <returns>排序后的数据表行。</returns>
        T[] GetDataRows(Comparison<T> comparison);

        /// <summary>
        /// 获取排序后的数据表行。
        /// </summary>
        /// <param name="comparison">数据表行的排序比较器。</param>
        /// <param name="results">接收数据表行的结果列表。</param>
        void GetDataRows(Comparison<T> comparison, List<T> results);

        /// <summary>
        /// 获取排序后的符合条件的数据表行。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <param name="comparison">数据表行的排序比较器。</param>
        /// <returns>按指定条件筛选并排序后的数据表行。</returns>
        T[] GetDataRows(Predicate<T> condition, Comparison<T> comparison);

        /// <summary>
        /// 获取排序后的符合条件的数据表行。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <param name="comparison">数据表行的排序比较器。</param>
        /// <param name="results">接收数据表行的结果列表。</param>
        void GetDataRows(Predicate<T> condition, Comparison<T> comparison, List<T> results);

        /// <summary>
        /// 获取所有数据表行。
        /// </summary>
        /// <returns>所有数据表行。</returns>
        T[] GetAllDataRows();

        /// <summary>
        /// 清空并填充结果列表；记录本身仍由原表持有。
        /// </summary>
        /// <param name="results">接收数据表行的结果列表。</param>
        void GetAllDataRows(List<T> results);
    }
}
