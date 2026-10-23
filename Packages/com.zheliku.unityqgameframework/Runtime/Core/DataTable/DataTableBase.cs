using System;

namespace UnityQGameFramework.Core.DataTable
{
    /// <summary>按行类型与名称登记的只读表视图。</summary>
    public abstract class DataTableBase
    {
        protected DataTableBase(string name) { Name = name ?? string.Empty; }
        public string Name { get; }
        public string FullName => new TypeNamePair(Type, Name).ToString();
        public abstract Type Type { get; }
        public abstract int Count { get; }
        public abstract bool HasDataRow(int id);
        internal abstract void Shutdown();
    }
}
