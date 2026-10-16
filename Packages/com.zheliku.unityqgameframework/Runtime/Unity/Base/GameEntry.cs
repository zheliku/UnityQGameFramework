//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 游戏入口。
    /// </summary>
    public static class GameEntry
    {
        private static readonly FrameworkLinkedList<FrameworkComponent> s_FrameworkComponents = new FrameworkLinkedList<FrameworkComponent>();
        private static readonly Dictionary<Type, FrameworkComponent> s_ComponentCache = new Dictionary<Type, FrameworkComponent>();

        /// <summary>
        /// 游戏框架所在的场景编号。
        /// </summary>
        internal const int FrameworkSceneId = 0;

        /// <summary>
        /// 获取游戏框架组件。
        /// </summary>
        /// <typeparam name="T">要获取的游戏框架组件类型。</typeparam>
        /// <returns>要获取的游戏框架组件。</returns>
        public static T GetComponent<T>() where T : FrameworkComponent
        {
            return (T)GetComponent(typeof(T));
        }

        /// <summary>
        /// 获取游戏框架组件。
        /// </summary>
        /// <param name="type">要获取的游戏框架组件类型。</param>
        /// <returns>要获取的游戏框架组件。</returns>
        public static FrameworkComponent GetComponent(Type type)
        {
            return type != null && s_ComponentCache.TryGetValue(type, out FrameworkComponent component) ? component : null;
        }

        /// <summary>
        /// 获取游戏框架组件。
        /// </summary>
        /// <param name="typeName">要获取的游戏框架组件类型名称。</param>
        /// <returns>要获取的游戏框架组件。</returns>
        public static FrameworkComponent GetComponent(string typeName)
        {
            LinkedListNode<FrameworkComponent> current = s_FrameworkComponents.First;
            while (current != null)
            {
                Type type = current.Value.GetType();
                if (type.FullName == typeName || type.Name == typeName)
                {
                    return current.Value;
                }

                current = current.Next;
            }

            return null;
        }

        /// <summary>
        /// 关闭游戏框架。
        /// </summary>
        /// <param name="shutdownType">关闭游戏框架类型。</param>
        public static void Shutdown(ShutdownType shutdownType)
        {
            Log.Info("Shutdown Unity Q Game Framework ({0})...", shutdownType);
            BaseComponent baseComponent = GetComponent<BaseComponent>();
            if (baseComponent != null)
            {
                baseComponent.Shutdown();
                baseComponent = null;
            }

            ClearComponents();

            if (shutdownType == ShutdownType.None)
            {
                return;
            }

            if (shutdownType == ShutdownType.Restart)
            {
                SceneManager.LoadScene(FrameworkSceneId);
                return;
            }

            if (shutdownType == ShutdownType.Quit)
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                return;
            }
        }

        /// <summary>
        /// 注册游戏框架组件。
        /// </summary>
        /// <param name="frameworkComponent">要注册的游戏框架组件。</param>
        internal static void RegisterComponent(FrameworkComponent frameworkComponent)
        {
            if (frameworkComponent == null)
            {
                Log.Error("Unity Q Game Framework component is invalid.");
                return;
            }

            Type type = frameworkComponent.GetType();

            if (s_ComponentCache.ContainsKey(type))
            {
                Log.Error("Unity Q Game Framework component type '{0}' is already exist.", type.FullName);
                return;
            }

            s_FrameworkComponents.AddLast(frameworkComponent);
            s_ComponentCache.Add(type, frameworkComponent);
        }

        internal static void ClearComponents()
        {
            s_FrameworkComponents.Clear();
            s_ComponentCache.Clear();
        }
    }
}
