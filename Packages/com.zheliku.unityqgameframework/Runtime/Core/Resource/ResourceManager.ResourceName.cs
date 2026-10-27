//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityQGameFramework.Core.Resource
{
    internal sealed partial class ResourceManager : FrameworkModule, IResourceManager
    {
        /// <summary>
        /// 资源名称。
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        private struct ResourceName : IComparable, IComparable<ResourceName>, IEquatable<ResourceName>
        {
            private static readonly Dictionary<ResourceName, string> s_ResourceFullNames = new Dictionary<ResourceName, string>();

            /// <summary>
            /// 初始化资源名称的新实例。
            /// </summary>
            /// <param name="name">资源名称。</param>
            /// <param name="variant">变体名称。</param>
            /// <param name="extension">扩展名称。</param>
            public ResourceName(string name, string variant, string extension)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new FrameworkException("Resource name is invalid.");
                }

                if (string.IsNullOrEmpty(extension))
                {
                    throw new FrameworkException("Resource extension is invalid.");
                }

                Name = name;
                Variant = variant;
                Extension = extension;
            }

            /// <summary>
            /// 获取资源名称。
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 获取变体名称。
            /// </summary>
            public string Variant { get; }

            /// <summary>
            /// 获取扩展名称。
            /// </summary>
            public string Extension { get; }

            /// <summary>
            /// 获取包含变体和扩展名的完整资源名称。
            /// </summary>
            public string FullName
            {
                get
                {
                    string fullName = null;
                    if (s_ResourceFullNames.TryGetValue(this, out fullName))
                    {
                        return fullName;
                    }

                    fullName = Variant != null ? Utility.Text.Format("{0}.{1}.{2}", Name, Variant, Extension) : Utility.Text.Format("{0}.{1}", Name, Extension);
                    s_ResourceFullNames.Add(this, fullName);
                    return fullName;
                }
            }

            /// <summary>
            /// 获取完整资源名称的字符串。
            /// </summary>
            /// <returns>完整资源名称。</returns>
            public override string ToString()
            {
                return FullName;
            }

            /// <summary>
            /// 计算资源名称的哈希值。
            /// </summary>
            /// <returns>由名称、变体和扩展名组成的哈希值。</returns>
            public override int GetHashCode()
            {
                return (Name?.GetHashCode() ?? 0) ^ (Variant?.GetHashCode() ?? 0) ^ (Extension?.GetHashCode() ?? 0);
            }

            /// <summary>
            /// 比较资源名称是否相等。
            /// </summary>
            /// <param name="obj">要比较的对象。</param>
            /// <returns>名称、变体和扩展名是否均相等。</returns>
            public override bool Equals(object obj)
            {
                return obj is ResourceName other && Equals(other);
            }

            /// <summary>
            /// 比较资源名称是否相等。
            /// </summary>
            /// <param name="value">要比较的资源名称。</param>
            /// <returns>名称、变体和扩展名是否均相等。</returns>
            public bool Equals(ResourceName value)
            {
                return string.Equals(Name, value.Name, StringComparison.Ordinal) && string.Equals(Variant, value.Variant, StringComparison.Ordinal) && string.Equals(Extension, value.Extension, StringComparison.Ordinal);
            }

            /// <summary>
            /// 比较两个资源名称是否相等。
            /// </summary>
            /// <param name="a">第一个资源名称。</param>
            /// <param name="b">第二个资源名称。</param>
            /// <returns>两个资源名称是否相等。</returns>
            public static bool operator ==(ResourceName a, ResourceName b)
            {
                return a.Equals(b);
            }

            /// <summary>
            /// 比较两个资源名称是否不相等。
            /// </summary>
            /// <param name="a">第一个资源名称。</param>
            /// <param name="b">第二个资源名称。</param>
            /// <returns>两个资源名称是否不相等。</returns>
            public static bool operator !=(ResourceName a, ResourceName b)
            {
                return !(a == b);
            }

            /// <summary>
            /// 按名称、变体和扩展名的顺序比较资源名称。
            /// </summary>
            /// <param name="value">要比较的对象。</param>
            /// <returns>表示资源名称排序关系的有符号整数。</returns>
            public int CompareTo(object value)
            {
                if (value == null)
                {
                    return 1;
                }

                if (!(value is ResourceName resourceName))
                {
                    throw new FrameworkException("Type of value is invalid.");
                }

                return CompareTo(resourceName);
            }

            /// <summary>
            /// 按名称、变体和扩展名的顺序比较资源名称。
            /// </summary>
            /// <param name="resourceName">要比较的资源名称。</param>
            /// <returns>表示资源名称排序关系的有符号整数。</returns>
            public int CompareTo(ResourceName resourceName)
            {
                int result = string.CompareOrdinal(Name, resourceName.Name);
                if (result != 0)
                {
                    return result;
                }

                result = string.CompareOrdinal(Variant, resourceName.Variant);
                if (result != 0)
                {
                    return result;
                }

                return string.CompareOrdinal(Extension, resourceName.Extension);
            }
        }
    }
}
