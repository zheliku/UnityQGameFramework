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

            public override string ToString()
            {
                return FullName;
            }

            public override int GetHashCode()
            {
                return (Name?.GetHashCode() ?? 0) ^ (Variant?.GetHashCode() ?? 0) ^ (Extension?.GetHashCode() ?? 0);
            }

            public override bool Equals(object obj)
            {
                return obj is ResourceName other && Equals(other);
            }

            public bool Equals(ResourceName value)
            {
                return string.Equals(Name, value.Name, StringComparison.Ordinal) && string.Equals(Variant, value.Variant, StringComparison.Ordinal) && string.Equals(Extension, value.Extension, StringComparison.Ordinal);
            }

            public static bool operator ==(ResourceName a, ResourceName b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(ResourceName a, ResourceName b)
            {
                return !(a == b);
            }

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
