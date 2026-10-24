//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityQGameFramework.Core.DataTable
{
    /// <summary>
    /// Luban 表的公共查询实现，不再另外持有一份字典或记录。
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
            foreach (T row in this) if (condition(row)) return true;
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
            foreach (T row in this) if (condition(row)) return row;
            return default;
        }

        /// <summary>
        /// 获取符合条件的数据表行。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <returns>符合查询条件的数据表行。</returns>
        public T[] GetDataRows(Predicate<T> condition)
        {
            var results = new List<T>();
            GetDataRows(condition, results);
            return results.ToArray();
        }

        /// <summary>
        /// 获取符合条件的数据表行。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <param name="results">接收数据表行的结果列表。</param>
        public void GetDataRows(Predicate<T> condition, List<T> results)
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
        /// <param name="comparison">要排序的条件。</param>
        /// <returns>符合查询条件的数据表行。</returns>
        public T[] GetDataRows(Comparison<T> comparison)
        {
            var results = new List<T>(Count);
            GetDataRows(comparison, results);
            return results.ToArray();
        }

        /// <summary>
        /// 获取排序后的数据表行。
        /// </summary>
        /// <param name="comparison">要排序的条件。</param>
        /// <param name="results">接收数据表行的结果列表。</param>
        public void GetDataRows(Comparison<T> comparison, List<T> results)
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
        /// <param name="comparison">要排序的条件。</param>
        /// <returns>符合查询条件的数据表行。</returns>
        public T[] GetDataRows(Predicate<T> condition, Comparison<T> comparison)
        {
            var results = new List<T>();
            GetDataRows(condition, comparison, results);
            return results.ToArray();
        }

        /// <summary>
        /// 获取排序后的符合条件的数据表行。
        /// </summary>
        /// <param name="condition">要检查的条件。</param>
        /// <param name="comparison">要排序的条件。</param>
        /// <param name="results">接收数据表行的结果列表。</param>
        public void GetDataRows(Predicate<T> condition, Comparison<T> comparison, List<T> results)
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
        /// <returns>所有数据表行。</returns>
        public T[] GetAllDataRows()
        {
            var results = new List<T>(Count);
            Collect(null, results);
            return results.ToArray();
        }

        /// <summary>
        /// 获取所有数据表行。
        /// </summary>
        /// <param name="results">接收数据表行的结果列表。</param>
        public void GetAllDataRows(List<T> results)
        {
            Collect(null, results);
        }

        private void Collect(Predicate<T> condition, List<T> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }
            results.Clear();
            // 按生成表的原始顺序枚举；排序只改变结果列表。
            foreach (T row in this) if (condition == null || condition(row)) results.Add(row);
        }
    }
}
