using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    [CustomEditor(typeof(LubanProfile))]
    internal sealed class LubanProfileInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(!LubanMenu.CanGenerate)) DrawDefaultInspector();
            var profile = (LubanProfile)target;
            string source = AssetDatabase.GetAssetPath(profile.SourceFolder);
            string path = source + "/luban.conf";
            if (string.IsNullOrEmpty(source) || !File.Exists(path))
            {
                EditorGUILayout.HelpBox("Source Folder 中需要有 luban.conf。", MessageType.Info);
                return;
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("所选 target 的生成参数", EditorStyles.boldLabel);
            try
            {
                var conf = JsonUtility.FromJson<Configuration>(File.ReadAllText(path));
                var selected = Array.Find(conf.targets ?? Array.Empty<ExportTarget>(), t => t.name == profile.Target);
                if (selected == null) EditorGUILayout.HelpBox("luban.conf 中不存在此 target：" + profile.Target, MessageType.Warning);
                else
                {
                    EditorGUILayout.LabelField("命名空间 (topModule)", selected.topModule ?? string.Empty);
                    EditorGUILayout.LabelField("入口类名 (manager)", selected.manager ?? string.Empty);
                    EditorGUILayout.LabelField("导出分组 (groups)", string.Join(", ", selected.groups ?? Array.Empty<string>()));
                }
            }
            catch (Exception exception) { EditorGUILayout.HelpBox(exception.Message, MessageType.Warning); }
            EditorGUILayout.HelpBox("以上参数以 luban.conf 为准，修改后重新生成。Profile 负责选择 target 和输出目录，不保存第二份命名空间设置。", MessageType.None);
            if (GUILayout.Button("打开 luban.conf")) EditorUtility.OpenWithDefaultApp(Path.GetFullPath(path));
            if (GUILayout.Button("定位 Excel 表定义")) Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(source + "/Datas/__tables__.xlsx");
        }

        [Serializable]
        private sealed class Configuration { public ExportTarget[] targets = null; }
        [Serializable]
        private sealed class ExportTarget { public string name = null; public string topModule = null; public string manager = null; public string[] groups = null; }
    }
}
