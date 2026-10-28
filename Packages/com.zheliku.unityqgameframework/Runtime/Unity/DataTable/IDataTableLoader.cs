using System;
using System.Collections.Generic;
using UnityQGameFramework.Core;
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
        string SchemaHash
        {
            get;
        }

        /// <summary>
        /// 构造数据表并解析表间引用；不读取资源、不修改 DataTableComponent 登记。
        /// </summary>
        /// <param name="loader">读取指定数据文件的加载方法。</param>
        /// <returns>已完成解析的数据表。</returns>
        IReadOnlyList<DataTableBase> CreateTables(Func<string, ByteBuf> loader);
    }
}
