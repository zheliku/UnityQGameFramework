//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 帮助相关的实用函数。
    /// </summary>
    public static class Help
    {
        [MenuItem("Unity Q Game Framework/Documentation", false, 90)]
        public static void ShowDocumentation()
        {
            ShowHelp("https://github.com/zheliku/UnityQGameFramework/blob/main/README.md");
        }

        [MenuItem("Unity Q Game Framework/API Reference", false, 91)]
        public static void ShowApiReference()
        {
            ShowHelp("https://github.com/zheliku/UnityQGameFramework/blob/main/Packages/com.zheliku.unityqgameframework/Documentation~/api.md");
        }

        private static void ShowHelp(string uri)
        {
            Application.OpenURL(uri);
        }
    }
}
