using System.Collections.Generic;
using UnityQGameFramework.Core.DataTable;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 由 Luban 生成的 Tables 实现。
    /// </summary>
    public abstract class LubanTableSet
    {
        /// <summary>
        /// 获取生成代码的结构版本。
        /// </summary>
        public abstract string SchemaHash
        {
            get;
        }

        /// <summary>
        /// 获取已完成跨表引用解析的所有数据表。
        /// </summary>
        public abstract IReadOnlyList<DataTableBase> DataTables
        {
            get;
        }
    }
}
