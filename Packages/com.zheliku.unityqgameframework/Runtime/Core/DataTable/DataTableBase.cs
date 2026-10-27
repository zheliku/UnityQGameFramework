using System;
using System.Collections;

namespace UnityQGameFramework.Core.DataTable
{
    /// <summary>
    /// 可登记的数据表；具体键和索引由 Luban schema 决定。
    /// </summary>
    public abstract class DataTableBase : IEnumerable
    {
        /// <summary>
        /// 初始化数据表的新实例。
        /// </summary>
        /// <param name="name">数据表名称。</param>
        protected DataTableBase(string name = null)
        {
            Name = name ?? string.Empty;
        }

        /// <summary>
        /// 获取数据表名称。
        /// </summary>
        public string Name
        {
            get;
        }

        /// <summary>
        /// 获取数据表完整名称。
        /// </summary>
        public string FullName
        {
            get
            {
                return new TypeNamePair(Type, Name).ToString();
            }
        }

        /// <summary>
        /// 表类型，例如 TbItem；同一行结构可以用于多张表。
        /// </summary>
        public Type Type
        {
            get
            {
                return GetType();
            }
        }

        /// <summary>
        /// 获取数据表行的类型。
        /// </summary>
        public abstract Type RowType
        {
            get;
        }

        /// <summary>
        /// 获取数据表行数。
        /// </summary>
        public abstract int Count
        {
            get;
        }

        /// <summary>
        /// 获取数据表记录的枚举器。
        /// </summary>
        /// <returns>按当前表顺序遍历记录的枚举器。</returns>
        protected abstract IEnumerator GetRowEnumerator();

        /// <summary>
        /// 获取数据表的行枚举器。
        /// </summary>
        /// <returns>用于遍历数据行的枚举器。</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetRowEnumerator();
        }
    }
}
