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
            EditorGUILayout.LabelField("生成参数", EditorStyles.boldLabel);
            try
            {
                var conf = JsonUtility.FromJson<Configuration>(File.ReadAllText(path));
                var selected = Array.Find(conf.targets ?? Array.Empty<ExportTarget>(), t => t.name == profile.Target);
                if (selected == null) EditorGUILayout.HelpBox("luban.conf 中不存在此 target：" + profile.Target, MessageType.Warning);
                else
                {
                    EditorGUILayout.LabelField("命名空间", selected.topModule ?? string.Empty);
                    EditorGUILayout.LabelField("入口类", selected.manager ?? string.Empty);
                    EditorGUILayout.LabelField("分组", string.Join(", ", selected.groups ?? Array.Empty<string>()));
                }
            }
            catch (Exception exception) { EditorGUILayout.HelpBox(exception.Message, MessageType.Warning); }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("编辑配置")) EditorUtility.OpenWithDefaultApp(Path.GetFullPath(path));
                if (GUILayout.Button("表定义")) Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(source + "/Datas/__tables__.xlsx");
            }
        }

        [Serializable]
        private sealed class Configuration { public ExportTarget[] targets = null; }
        [Serializable]
        private sealed class ExportTarget { public string name = null; public string topModule = null; public string manager = null; public string[] groups = null; }
    }
}
