using System;

namespace UnityQGameFramework.Core.DataTable
{
    /// <summary>A named, read-only view of configuration records.</summary>
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
