using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 绘制 Luban 生成配置。
    /// </summary>
    [CustomEditor(typeof(LubanProfile))]
    internal sealed class LubanProfileInspector : UnityEditor.Editor
    {
        /// <summary>
        /// 绘制生成参数与配置入口。
        /// </summary>
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(!LubanMenu.CanGenerate))
            {
                DrawDefaultInspector();
            }

            var profile = (LubanProfile)target;
            profile.UpgradeConfiguration();
            string path = AssetDatabase.GetAssetPath(profile.ConfigurationFile);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                EditorGUILayout.HelpBox("Assign the luban.conf file in the Config folder.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generation Options", EditorStyles.boldLabel);
            Configuration conf = null;
            try
            {
                conf = JsonUtility.FromJson<Configuration>(File.ReadAllText(path));
                var selected = Array.Find(conf.targets ?? Array.Empty<ExportTarget>(), t => t.name == profile.Target);
                if (selected == null)
                {
                    EditorGUILayout.HelpBox("Target not found in luban.conf: " + profile.Target, MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField("Namespace", selected.topModule ?? string.Empty);
                    EditorGUILayout.LabelField("Entry Class", selected.manager ?? string.Empty);
                    EditorGUILayout.LabelField("Groups", string.Join(", ", selected.groups ?? Array.Empty<string>()));
                }
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Edit Configuration"))
                {
                    EditorUtility.OpenWithDefaultApp(Path.GetFullPath(path));
                }

                SchemaFile tableSchema = Array.Find(conf?.schemaFiles ?? Array.Empty<SchemaFile>(), schema => schema.type == "table");
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(tableSchema?.fileName)))
                {
                    if (GUILayout.Button("Table Schema"))
                    {
                        string schemaPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path), tableSchema.fileName));
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(FileUtil.GetProjectRelativePath(schemaPath));
                    }
                }
            }
        }

        /// <summary>
        /// Luban 配置文件中的导出目标与表定义。
        /// </summary>
        [Serializable]
        private sealed class Configuration
        {
            /// <summary>
            /// 导出目标配置。
            /// </summary>
            public ExportTarget[] targets = null;

            /// <summary>
            /// Excel schema 文件配置。
            /// </summary>
            public SchemaFile[] schemaFiles = null;
        }

        /// <summary>
        /// Luban 表定义文件的位置和类型。
        /// </summary>
        [Serializable]
        private sealed class SchemaFile
        {
            /// <summary>
            /// 导出文件名称。
            /// </summary>
            public string fileName = null;

            /// <summary>
            /// 导出类型。
            /// </summary>
            public string type = null;
        }

        /// <summary>
        /// Luban 导出目标的命名空间、入口和分组。
        /// </summary>
        [Serializable]
        private sealed class ExportTarget
        {
            /// <summary>
            /// 导出目标名称。
            /// </summary>
            public string name = null;

            /// <summary>
            /// 顶层模块名称。
            /// </summary>
            public string topModule = null;

            /// <summary>
            /// 数据表管理器名称。
            /// </summary>
            public string manager = null;

            /// <summary>
            /// 导出分组名称。
            /// </summary>
            public string[] groups = null;
        }
    }
}
