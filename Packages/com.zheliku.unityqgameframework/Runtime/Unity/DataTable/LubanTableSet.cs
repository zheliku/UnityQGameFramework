using System.Collections.Generic;
using UnityQGameFramework.Core.DataTable;

namespace UnityQGameFramework.Runtime
{
    /// <summary>由 Luban 生成的 Tables 实现，提供结构版本与可登记的只读视图。</summary>
    public abstract class LubanTableSet
    {
        public abstract string SchemaHash { get; }
        public abstract IReadOnlyList<DataTableBase> DataTables { get; }
    }
}
