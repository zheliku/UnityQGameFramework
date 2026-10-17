using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    internal static class LubanSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider() => new SettingsProvider(LubanSettings.SettingsPath, SettingsScope.Project)
        {
            label = "Luban",
            guiHandler = _ => DrawSettings(),
            keywords = new HashSet<string> { "QGF", "Luban", "Profile", "配置表", "工作区" }
        };

        private static void DrawSettings()
        {
            var settings = LubanSettings.instance;
            EditorGUILayout.HelpBox("一键生成始终使用此全局 Profile。设置随项目保存，切换 Project 窗口中的选择不会改变生成目标。", MessageType.Info);
            using (new EditorGUI.DisabledScope(!LubanMenu.CanGenerate))
            {
                var profile = (LubanProfile)EditorGUILayout.ObjectField("全局 Profile", settings.Profile, typeof(LubanProfile), false);
                if (profile != settings.Profile) settings.Profile = profile;
                string root = EditorGUILayout.DelayedTextField("新工作区目录", settings.WorkspaceRoot);
                if (root != settings.WorkspaceRoot)
                {
                    try { settings.WorkspaceRoot = root; }
                    catch (Exception exception) { Debug.LogWarning(exception.Message); }
                }
                EditorGUILayout.HelpBox("默认目录用于创建新工作区，不会移动已有文件。移动现有工作区请使用 Project 窗口并保留 .meta；也可在 Profile 的 Inspector 中重新指定各输出目录。", MessageType.None);
                if (GUILayout.Button("创建或使用该目录的工作区")) LubanMenu.CreateWorkspace();
            }
            if (settings.HasProfile && settings.Profile == null)
                EditorGUILayout.HelpBox("已配置的 Profile 丢失。请重新指定，工具不会自动切换到其他工作区。", MessageType.Warning);
            using (new EditorGUI.DisabledScope(settings.Profile == null))
                if (GUILayout.Button("定位并编辑当前 Profile")) Selection.activeObject = settings.Profile;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("当前目录", EditorStyles.boldLabel);
            if (settings.Profile != null)
            {
                EditorGUILayout.LabelField("表源", AssetDatabase.GetAssetPath(settings.Profile.SourceFolder));
                EditorGUILayout.LabelField("生成代码", AssetDatabase.GetAssetPath(settings.Profile.CodeFolder));
                EditorGUILayout.LabelField("生成数据", AssetDatabase.GetAssetPath(settings.Profile.DataFolder));
            }
            EditorGUILayout.LabelField("项目设置文件", "ProjectSettings/UnityQGameFrameworkSettings.asset");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("本机生成工具", EditorStyles.boldLabel);
            string compiler = string.IsNullOrWhiteSpace(LubanGenerator.LocalGenerator) ? LubanGenerator.DefaultGenerator : LubanGenerator.LocalGenerator;
            EditorGUILayout.LabelField("编译器版本", LubanGenerator.Version);
            EditorGUILayout.SelectableLabel(compiler, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (!File.Exists(compiler)) EditorGUILayout.HelpBox("尚未安装编译器。首次生成将下载并校验固定版本；离线环境可选择本地 Luban.dll。生成需要 .NET 8 运行时或兼容环境。", MessageType.Info);
            using (new EditorGUI.DisabledScope(!LubanMenu.CanGenerate))
            {
                int timeout = EditorGUILayout.DelayedIntField("进程超时（秒）", LubanGenerator.ProcessTimeoutSeconds);
                if (timeout != LubanGenerator.ProcessTimeoutSeconds)
                {
                    try { LubanGenerator.ProcessTimeoutSeconds = timeout; }
                    catch (ArgumentOutOfRangeException exception) { Debug.LogWarning(exception.Message); }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("选择本地编译器")) LubanMenu.SelectLocalCompiler();
                    if (GUILayout.Button("使用下载缓存")) LubanMenu.UseDownloadedCompiler();
                }
            }
            EditorGUILayout.HelpBox("编译器路径与超时仅保存在本机，不随项目提交。生成结果见 Console。", MessageType.None);
        }
    }
}
