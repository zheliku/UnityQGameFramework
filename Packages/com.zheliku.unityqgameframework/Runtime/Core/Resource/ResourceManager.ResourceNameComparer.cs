//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;

namespace UnityQGameFramework.Core.Resource
{
    internal sealed partial class ResourceManager : FrameworkModule, IResourceManager
    {
        /// <summary>
        /// 资源名称比较器。
        /// </summary>
        private sealed class ResourceNameComparer : IComparer<ResourceName>, IEqualityComparer<ResourceName>
        {
            /// <summary>
            /// 比较两个资源名称的顺序。
            /// </summary>
            /// <param name="x">第一个资源名称。</param>
            /// <param name="y">第二个资源名称。</param>
            /// <returns>表示排序关系的有符号整数。</returns>
            public int Compare(ResourceName x, ResourceName y)
            {
                return x.CompareTo(y);
            }

            /// <summary>
            /// 比较两个资源名称是否相等。
            /// </summary>
            /// <param name="x">第一个资源名称。</param>
            /// <param name="y">第二个资源名称。</param>
            /// <returns>两个资源名称是否相等。</returns>
            public bool Equals(ResourceName x, ResourceName y)
            {
                return x.Equals(y);
            }

            /// <summary>
            /// 计算资源名称的哈希值。
            /// </summary>
            /// <param name="obj">资源名称。</param>
            /// <returns>资源名称的哈希值。</returns>
            public int GetHashCode(ResourceName obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
