//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace UnityQGameFramework.Core
{
    /// <summary>
    /// 游戏框架入口。
    /// </summary>
    public static class FrameworkEntry
    {
        private static readonly FrameworkLinkedList<FrameworkModule> s_FrameworkModules = new FrameworkLinkedList<FrameworkModule>();
        private static readonly Dictionary<Type, FrameworkModule> s_ModuleCache = new Dictionary<Type, FrameworkModule>();

        /// <summary>
        /// 所有游戏框架模块轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public static void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (FrameworkModule module in s_FrameworkModules)
            {
                module.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理所有游戏框架模块。
        /// </summary>
        public static void Shutdown()
        {
            for (LinkedListNode<FrameworkModule> current = s_FrameworkModules.Last; current != null; current = current.Previous)
            {
                current.Value.Shutdown();
            }

            s_FrameworkModules.Clear();
            s_ModuleCache.Clear();
            ReferencePool.ClearAll();
            Utility.Marshal.FreeCachedHGlobal();
            FrameworkLog.SetLogHelper(null);
        }

        /// <summary>
        /// 获取游戏框架模块。
        /// </summary>
        /// <typeparam name="T">要获取的游戏框架模块类型。</typeparam>
        /// <returns>要获取的游戏框架模块。</returns>
        /// <remarks>如果要获取的游戏框架模块不存在，则自动创建该游戏框架模块。</remarks>
        public static T GetModule<T>() where T : class
        {
            Type interfaceType = typeof(T);
            if (!interfaceType.IsInterface)
            {
                throw new FrameworkException(Utility.Text.Format("You must get module by interface, but '{0}' is not.", interfaceType.FullName));
            }

            if (s_ModuleCache.TryGetValue(interfaceType, out FrameworkModule cachedModule))
            {
                return (T)(object)cachedModule;
            }

            if (!interfaceType.FullName.StartsWith("UnityQGameFramework.Core.", StringComparison.Ordinal))
            {
                throw new FrameworkException(Utility.Text.Format("You must get a Unity Q Game Framework module, but '{0}' is not.", interfaceType.FullName));
            }

            string moduleName = Utility.Text.Format("{0}.{1}", interfaceType.Namespace, interfaceType.Name.Substring(1));
            Type moduleType = Type.GetType(moduleName);
            if (moduleType == null)
            {
                throw new FrameworkException(Utility.Text.Format("Can not find Unity Q Game Framework module type '{0}'.", moduleName));
            }

            T module = GetModule(moduleType) as T;
            s_ModuleCache.Add(interfaceType, (FrameworkModule)(object)module);
            return module;
        }

        /// <summary>
        /// 获取游戏框架模块。
        /// </summary>
        /// <param name="moduleType">要获取的游戏框架模块类型。</param>
        /// <returns>要获取的游戏框架模块。</returns>
        /// <remarks>如果要获取的游戏框架模块不存在，则自动创建该游戏框架模块。</remarks>
        private static FrameworkModule GetModule(Type moduleType)
        {
            return s_ModuleCache.TryGetValue(moduleType, out FrameworkModule module) ? module : CreateModule(moduleType);
        }

        /// <summary>
        /// 创建游戏框架模块。
        /// </summary>
        /// <param name="moduleType">要创建的游戏框架模块类型。</param>
        /// <returns>要创建的游戏框架模块。</returns>
        private static FrameworkModule CreateModule(Type moduleType)
        {
            FrameworkModule module = (FrameworkModule)Activator.CreateInstance(moduleType);
            if (module == null)
            {
                throw new FrameworkException(Utility.Text.Format("Can not create module '{0}'.", moduleType.FullName));
            }

            LinkedListNode<FrameworkModule> current = s_FrameworkModules.First;
            while (current != null)
            {
                if (module.Priority > current.Value.Priority)
                {
                    break;
                }

                current = current.Next;
            }

            if (current != null)
            {
                s_FrameworkModules.AddBefore(current, module);
            }
            else
            {
                s_FrameworkModules.AddLast(module);
            }

            s_ModuleCache.Add(moduleType, module);
            return module;
        }
    }
}
