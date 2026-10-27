using System;
using System.Collections.Generic;
using UnityQGameFramework.Core.DataTable;
using UnityQGameFramework.Luban;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 数据表加载器，由生成代码实现。
    /// </summary>
    public interface IDataTableLoader
    {
        /// <summary>
        /// 获取生成代码的结构版本。
        /// </summary>
        string SchemaHash { get; }

        /// <summary>
        /// 加载数据表并解析表间引用。
        /// </summary>
        /// <param name="loader">读取指定数据文件的加载方法。</param>
        /// <returns>已完成解析的数据表。</returns>
        IReadOnlyList<DataTableBase> Load(Func<string, ByteBuf> loader);
    }
}
