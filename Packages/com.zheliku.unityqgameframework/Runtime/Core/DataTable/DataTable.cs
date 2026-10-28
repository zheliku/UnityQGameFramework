//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityQGameFramework.Core
{
    /// <summary>
    /// 数据表的公共查询实现。
    /// </summary>
    /// <typeparam name="T">数据表行的类型。</typeparam>
    public abstract class DataTable<T> : DataTableBase, IDataTable<T>
    {
        /// <summary>
        /// 初始化数据表的新实例。
        /// </summary>
        /// <param name="name">数据表名称。</param>
        protected DataTable(string name = null) : base(name)
        {
        }

        /// <summary>
        /// 获取数据表行的类型。
        /// </summary>
        public override Type RowType
        {
            get
            {
                return typeof(T);
            }
        }

        /// <summary>
        /// 返回循环访问数据表的枚举数。
        /// </summary>
        /// <returns>循环访问数据表的枚举数。</returns>
        public abstract IEnumerator<T> GetEnumerator();

        /// <summary>
        /// 按源表顺序获取指定位置的记录。
        /// </summary>
        /// <param name="index">从零开始的记录位置。</param>
        /// <returns>指定位置的记录。</returns>
        protected abstract T GetRow(int index);

        /// <summary>
        /// 获取数据表记录的枚举器。
        /// </summary>
        /// <returns>按当前表顺序遍历记录的枚举器。</returns>
        protected override IEnumerator GetRowEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 检查是否存在符合条件的数据表行。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>是否存在符合条件的数据表行。</returns>
        public bool HasDataRow(Predicate<T> condition)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }
            for (int i = 0; i < Count; i++)
            {
                T row = GetRow(i);
                if (condition(row))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取首条符合条件的数据表行，不存在时返回默认值。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>首条符合条件的数据表行，不存在时返回默认值。</returns>
        public T GetDataRow(Predicate<T> condition)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }
            for (int i = 0; i < Count; i++)
            {
                T row = GetRow(i);
                if (condition(row))
                {
                    return row;
                }
            }
            return default;
        }

        /// <summary>
        /// 获取符合条件的数据表行。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>新建的结果数组，记录本身仍由原表持有。</returns>
        public T[] GetDataRows(Predicate<T> condition)
        {
            var results = new List<T>();
            GetDataRowsNonAlloc(condition, results);
            return results.ToArray();
        }

        /// <summary>
        /// 使用调用方列表获取符合条件的数据表行，不创建结果数组。
        /// </summary>
        /// <param name="condition">筛选条件；复用委托可避免调用方分配。</param>
        /// <param name="results">填充前清空的结果列表；容量不足时仍会扩容，记录仍由原表持有。</param>
        public void GetDataRowsNonAlloc(Predicate<T> condition, List<T> results)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }
            Collect(condition, results);
        }

        /// <summary>
        /// 获取排序后的数据表行。
        /// </summary>
        /// <param name="comparison">数据表行的排序比较器。</param>
        /// <returns>排序后的新结果数组，不改变源表顺序和记录。</returns>
        public T[] GetDataRows(Comparison<T> comparison)
        {
            var results = new List<T>(Count);
            GetDataRowsNonAlloc(comparison, results);
            return results.ToArray();
        }

        /// <summary>
        /// 使用调用方列表获取排序后的数据表行，不创建结果数组。
        /// </summary>
        /// <param name="comparison">排序比较器；复用委托可避免调用方分配。</param>
        /// <param name="results">填充前清空的结果列表；容量不足时仍会扩容，排序不改变源表顺序。</param>
        public void GetDataRowsNonAlloc(Comparison<T> comparison, List<T> results)
        {
            if (comparison == null)
            {
                throw new ArgumentNullException(nameof(comparison));
            }
            Collect(null, results);
            results.Sort(comparison);
        }

        /// <summary>
        /// 获取排序后的符合条件的数据表行。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <param name="comparison">数据表行的排序比较器。</param>
        /// <returns>筛选并排序后的新结果数组，不改变源表顺序和记录。</returns>
        public T[] GetDataRows(Predicate<T> condition, Comparison<T> comparison)
        {
            var results = new List<T>();
            GetDataRowsNonAlloc(condition, comparison, results);
            return results.ToArray();
        }

        /// <summary>
        /// 使用调用方列表筛选并排序数据表行，不创建结果数组。
        /// </summary>
        /// <param name="condition">筛选条件；复用委托可避免调用方分配。</param>
        /// <param name="comparison">排序比较器；复用委托可避免调用方分配。</param>
        /// <param name="results">填充前清空的结果列表；容量不足时仍会扩容，排序不改变源表顺序。</param>
        public void GetDataRowsNonAlloc(Predicate<T> condition, Comparison<T> comparison, List<T> results)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }
            if (comparison == null)
            {
                throw new ArgumentNullException(nameof(comparison));
            }
            Collect(condition, results);
            results.Sort(comparison);
        }

        /// <summary>
        /// 获取所有数据表行。
        /// </summary>
        /// <returns>按源表顺序排列的新结果数组，记录本身仍由原表持有。</returns>
        public T[] GetAllDataRows()
        {
            var results = new List<T>(Count);
            Collect(null, results);
            return results.ToArray();
        }

        /// <summary>
        /// 使用调用方列表获取所有数据表行，不创建结果数组。
        /// </summary>
        /// <param name="results">填充前清空的结果列表；容量不足时仍会扩容，记录仍由原表持有。</param>
        public void GetAllDataRowsNonAlloc(List<T> results)
        {
            Collect(null, results);
        }

        /// <summary>
        /// 收集满足条件的数据行。
        /// </summary>
        /// <param name="condition">数据行筛选条件；为空时收集所有数据行。</param>
        /// <param name="results">接收数据行的列表，填充前会清空。</param>
        private void Collect(Predicate<T> condition, List<T> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }
            results.Clear();
            // 直接按索引读取，避免 IEnumerable<T> 路径的枚举器装箱。
            for (int i = 0; i < Count; i++)
            {
                T row = GetRow(i);
                if (condition == null || condition(row))
                {
                    results.Add(row);
                }
            }
        }
    }
}
