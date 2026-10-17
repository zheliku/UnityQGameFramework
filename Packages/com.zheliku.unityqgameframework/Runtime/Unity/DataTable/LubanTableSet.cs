using System.Collections.Generic;
using UnityQGameFramework.Core.DataTable;

namespace UnityQGameFramework.Runtime
{
    /// <summary>Implemented by the generated Luban Tables class; no game types enter the package.</summary>
    public abstract class LubanTableSet
    {
        public abstract string SchemaHash { get; }
        public abstract IReadOnlyList<DataTableBase> DataTables { get; }
    }
}
