using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 绘制 QGF 项目设置中的 Luban 选项。
    /// </summary>
    internal static class LubanSettingsProvider
    {
        private static bool s_Advanced;

        /// <summary>
        /// 创建 QGF Luban 设置页面。
        /// </summary>
        /// <returns>Luban 设置页面。</returns>
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(LubanSettings.SettingsPath, SettingsScope.Project)
            {
                label = "Luban",
                guiHandler = _ => DrawSettings(),
                keywords = new HashSet<string>
                {
                    "QGF",
                    "Luban",
                    "Profile",
                    "Data Tables"
                }
            };
        }

        /// <summary>
        /// 绘制 Luban 全局设置。
        /// </summary>
        private static void DrawSettings()
        {
            var settings = LubanSettings.instance;
            using (new EditorGUI.DisabledScope(!LubanMenu.CanGenerate))
            {
                var profile = (LubanProfile)EditorGUILayout.ObjectField("Profile", settings.Profile, typeof(LubanProfile), false);
                if (profile != settings.Profile)
                {
                    settings.Profile = profile;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    string root = EditorGUILayout.DelayedTextField("New Workspace", settings.WorkspaceRoot);
                    if (root != settings.WorkspaceRoot)
                    {
                        try
                        {
                            settings.WorkspaceRoot = root;
                        }
                        catch (ArgumentException exception)
                        {
                            Debug.LogWarning(exception.Message);
                        }
                    }

                    if (GUILayout.Button("Create", GUILayout.Width(60)))
                    {
                        LubanMenu.CreateWorkspace();
                    }
                }
            }

            if (settings.HasProfile && settings.Profile == null)
            {
                EditorGUILayout.HelpBox("The selected Profile is missing. Assign an existing Profile.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            s_Advanced = EditorGUILayout.Foldout(s_Advanced, "Advanced", true);
            if (!s_Advanced)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(!LubanMenu.CanGenerate))
            {
                int timeout = EditorGUILayout.DelayedIntField("Process Timeout (Seconds)", LubanGenerator.ProcessTimeoutSeconds);
                if (timeout != LubanGenerator.ProcessTimeoutSeconds)
                {
                    try
                    {
                        LubanGenerator.ProcessTimeoutSeconds = timeout;
                    }
                    catch (ArgumentOutOfRangeException exception)
                    {
                        Debug.LogWarning(exception.Message);
                    }
                }
            }
        }
    }
}
