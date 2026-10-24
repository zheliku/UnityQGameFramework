//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.IO;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Resource;

namespace StarForce
{
    /// <summary>
    /// 游戏入口。
    /// </summary>
    public partial class GameEntry : MonoBehaviour
    {
        public const string ResourceFolder = "QgfExamples/StarForce";

        private void Awake()
        {
            if (Application.isEditor && GetComponentInChildren<UnityQGameFramework.Runtime.BaseComponent>(true).EditorResourceMode) return;
            // 在 ResourceComponent.Start 创建加载代理前，指定示例自己的资源目录。
            var resources = FrameworkEntry.GetModule<IResourceManager>();
            resources.SetReadOnlyPath(Path.Combine(Application.streamingAssetsPath, ResourceFolder));
            resources.SetReadWritePath(Path.Combine(Application.persistentDataPath, ResourceFolder));
        }

        private void Start()
        {
            InitBuiltinComponents();
            InitCustomComponents();
        }
    }
}
