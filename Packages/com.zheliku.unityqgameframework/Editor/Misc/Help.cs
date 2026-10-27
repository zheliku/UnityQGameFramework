//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEditor;
using System.IO;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 帮助相关的实用函数。
    /// </summary>
    public static class Help
    {
        /// <summary>
        /// 打开框架文档。
        /// </summary>
        [MenuItem("QGF/Documentation", false, 90)]
        public static void ShowDocumentation()
        {
            EditorUtility.OpenWithDefaultApp(Path.Combine(LubanGenerator.PackageRoot, "Documentation~/README.md"));
        }
    }
}
