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
    public static class GameFrameworkEntry
    {
        private static readonly GameFrameworkLinkedList<GameFrameworkModule> s_GameFrameworkModules = new GameFrameworkLinkedList<GameFrameworkModule>();
        private static readonly Dictionary<Type, GameFrameworkModule> s_ModuleCache = new Dictionary<Type, GameFrameworkModule>();
        private static readonly Dictionary<Type, Func<GameFrameworkModule>> s_ModuleFactories = new Dictionary<Type, Func<GameFrameworkModule>>
        {
            { typeof(IConfigManager), () => new ConfigManager() },
            { typeof(IDataNodeManager), () => new DataNodeManager() },
            { typeof(IDataTableManager), () => new DataTableManager() },
            { typeof(IDebuggerManager), () => new DebuggerManager() },
            { typeof(IDownloadManager), () => new DownloadManager() },
            { typeof(IEntityManager), () => new EntityManager() },
            { typeof(IEventManager), () => new EventManager() },
            { typeof(IFileSystemManager), () => new FileSystemManager() },
            { typeof(IFsmManager), () => new FsmManager() },
            { typeof(ILocalizationManager), () => new LocalizationManager() },
            { typeof(INetworkManager), () => new NetworkManager() },
            { typeof(IObjectPoolManager), () => new ObjectPoolManager() },
            { typeof(IProcedureManager), () => new ProcedureManager() },
            { typeof(IResourceManager), () => new ResourceManager() },
            { typeof(ISceneManager), () => new SceneManager() },
            { typeof(ISettingManager), () => new SettingManager() },
            { typeof(ISoundManager), () => new SoundManager() },
            { typeof(IUIManager), () => new UIManager() },
            { typeof(IWebRequestManager), () => new WebRequestManager() }
        };

        /// <summary>
        /// 所有游戏框架模块轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public static void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (GameFrameworkModule module in s_GameFrameworkModules)
            {
                module.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理所有游戏框架模块。
        /// </summary>
        public static void Shutdown()
        {
            for (LinkedListNode<GameFrameworkModule> current = s_GameFrameworkModules.Last; current != null; current = current.Previous)
            {
                current.Value.Shutdown();
            }

            s_GameFrameworkModules.Clear();
            s_ModuleCache.Clear();
            ReferencePool.ClearAll();
            Utility.Marshal.FreeCachedHGlobal();
            GameFrameworkLog.SetLogHelper(null);
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
                throw new GameFrameworkException(Utility.Text.Format("You must get module by interface, but '{0}' is not.", interfaceType.FullName));
            }

            if (s_ModuleCache.TryGetValue(interfaceType, out GameFrameworkModule cachedModule))
            {
                return (T)(object)cachedModule;
            }

            return (T)(object)CreateModule(interfaceType);
        }

        /// <summary>
        /// 创建游戏框架模块。
        /// </summary>
        /// <param name="interfaceType">要创建的游戏框架模块接口。</param>
        /// <returns>要创建的游戏框架模块。</returns>
        private static GameFrameworkModule CreateModule(Type interfaceType)
        {
            if (!s_ModuleFactories.TryGetValue(interfaceType, out Func<GameFrameworkModule> factory))
            {
                throw new GameFrameworkException(Utility.Text.Format("Unknown Unity Q Game Framework module interface '{0}'.", interfaceType.FullName));
            }
            // 显式工厂使构造函数对 AOT 可见；模块仍按首次访问创建，并沿用 UGF 的轮询顺序。
            GameFrameworkModule module = factory();

            LinkedListNode<GameFrameworkModule> current = s_GameFrameworkModules.First;
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
                s_GameFrameworkModules.AddBefore(current, module);
            }
            else
            {
                s_GameFrameworkModules.AddLast(module);
            }

            s_ModuleCache.Add(interfaceType, module);
            return module;
        }
    }
}
