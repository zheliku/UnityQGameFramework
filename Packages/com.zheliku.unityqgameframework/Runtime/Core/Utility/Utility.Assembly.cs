//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityQGameFramework.Core
{
    public static partial class Utility
    {
        /// <summary>
        /// 程序集相关的实用函数。
        /// </summary>
        public static class Assembly
        {
            private static readonly Dictionary<string, Type> s_CachedTypes = new Dictionary<string, Type>(StringComparer.Ordinal);

            /// <summary>
            /// 获取已加载的程序集。
            /// </summary>
            /// <returns>已加载的程序集。</returns>
            public static System.Reflection.Assembly[] GetAssemblies()
            {
                return AppDomain.CurrentDomain.GetAssemblies();
            }

            /// <summary>
            /// 获取已加载的程序集中的所有类型。
            /// </summary>
            /// <returns>已加载的程序集中的所有类型。</returns>
            public static Type[] GetTypes()
            {
                List<Type> results = new List<Type>();
                GetTypes(results);
                return results.ToArray();
            }

            /// <summary>
            /// 获取已加载的程序集中的所有类型。
            /// </summary>
            /// <param name="results">接收已加载程序集中的所有类型；填充前清空，反射枚举仍会创建临时数组。</param>
            public static void GetTypes(List<Type> results)
            {
                if (results == null)
                {
                    throw new FrameworkException("Results is invalid.");
                }

                results.Clear();
                foreach (System.Reflection.Assembly assembly in GetAssemblies())
                {
                    try
                    {
                        results.AddRange(assembly.GetTypes());
                    }
                    catch (ReflectionTypeLoadException exception)
                    {
                        foreach (Type type in exception.Types)
                        {
                            if (type != null)
                            {
                                results.Add(type);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// 获取已加载的程序集中的指定类型。
            /// </summary>
            /// <param name="typeName">要获取的类型名。</param>
            /// <returns>已加载的程序集中的指定类型。</returns>
            public static Type GetType(string typeName)
            {
                if (string.IsNullOrEmpty(typeName))
                {
                    throw new FrameworkException("Type name is invalid.");
                }

                Type type = null;
                if (s_CachedTypes.TryGetValue(typeName, out type))
                {
                    return type;
                }

                type = Type.GetType(typeName);
                if (type != null)
                {
                    s_CachedTypes.Add(typeName, type);
                    return type;
                }

                foreach (System.Reflection.Assembly assembly in GetAssemblies())
                {
                    type = assembly.GetType(typeName, false);
                    if (type != null)
                    {
                        s_CachedTypes.Add(typeName, type);
                        return type;
                    }
                }

                return null;
            }
        }
    }
}
