using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Core.DataTable;

namespace UnityQGameFramework.Editor
{
    [CustomEditor(typeof(DataTableComponent))]
    internal sealed class DataTableComponentInspector : FrameworkInspector
    {
        private string m_Filter = string.Empty;
        private int m_Selected;
        private int m_Page;
        private LubanProfile m_Profile;
        private LubanProfile m_LastGlobalProfile;
        private bool m_ShowCatalog;
        private DataTableBase m_LastBrowsedTable;
        private const int PageSize = 20;

        private void OnEnable() => m_Profile = m_LastGlobalProfile = LubanSettings.instance.Profile;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            DrawLubanTools();
            var catalogProperty = serializedObject.FindProperty("m_LubanCatalog");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("运行时数据", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                EditorGUILayout.PropertyField(catalogProperty, new GUIContent("Luban Catalog"));
            DrawCatalog(catalogProperty);
            serializedObject.ApplyModifiedProperties();
            var component = (DataTableComponent)target;
            EditorGUILayout.LabelField("加载状态", component.IsLoading ? "加载中" : "就绪");
            EditorGUILayout.LabelField("已注册数据表", component.Count.ToString());
            if (!EditorApplication.isPlaying) return;
            var tables = component.GetAllDataTables();
            if (tables.Length == 0) return;
            var names = Array.ConvertAll(tables, t => t.Type.Name + " · " + t.Count);
            m_Selected = Mathf.Clamp(m_Selected, 0, tables.Length - 1);
            int selection = EditorGUILayout.Popup("数据表", m_Selected, names);
            if (selection != m_Selected) { m_Selected = selection; m_Page = 0; }
            if (tables[m_Selected] != m_LastBrowsedTable) { m_LastBrowsedTable = tables[m_Selected]; m_Page = 0; }
            string filter = EditorGUILayout.TextField("筛选", m_Filter);
            if (filter != m_Filter) { m_Filter = filter; m_Page = 0; }
            var rows = tables[m_Selected] as IEnumerable;
            if (rows == null)
            {
                EditorGUILayout.HelpBox("此数据表未提供记录枚举。", MessageType.Info);
                return;
            }
            int index = 0;
            foreach (object row in rows)
            {
                if (!string.IsNullOrEmpty(m_Filter) && row.ToString().IndexOf(m_Filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (index++ < m_Page * PageSize || index > (m_Page + 1) * PageSize) continue;
                EditorGUILayout.LabelField(row.ToString(), EditorStyles.wordWrappedLabel);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(m_Page == 0)) if (GUILayout.Button("上一页")) --m_Page;
                EditorGUILayout.LabelField((m_Page + 1).ToString(), GUILayout.Width(30));
                using (new EditorGUI.DisabledScope(index <= (m_Page + 1) * PageSize)) if (GUILayout.Button("下一页")) ++m_Page;
            }
        }

        private void DrawLubanTools()
        {
            var settings = LubanSettings.instance;
            if (settings.Profile != m_LastGlobalProfile)
            {
                if (m_Profile == m_LastGlobalProfile) m_Profile = settings.Profile;
                m_LastGlobalProfile = settings.Profile;
            }
            EditorGUILayout.LabelField("Luban 配表", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!LubanMenu.CanGenerate))
            {
                m_Profile = (LubanProfile)EditorGUILayout.ObjectField("生成 Profile", m_Profile, typeof(LubanProfile), false);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(m_Profile == null || m_Profile == settings.Profile))
                        if (GUILayout.Button("设为全局")) settings.Profile = m_Profile;
                    if (GUILayout.Button("项目设置")) LubanMenu.OpenSettings();
                    using (new EditorGUI.DisabledScope(settings.Profile == null))
                        if (GUILayout.Button("生成全局配置")) LubanMenu.Generate();
                }
            }
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("当前全局 Profile", settings.Profile, typeof(LubanProfile), false);
            if (m_Profile != settings.Profile)
                EditorGUILayout.HelpBox("此处选择的方案尚未设为全局。点击“设为全局”后，菜单和生成按钮才会使用它。", MessageType.Info);
            using (new EditorGUI.DisabledScope(settings.Profile == null || settings.Profile.SourceFolder == null))
                if (GUILayout.Button("在 Project 中定位源表")) Selection.activeObject = settings.Profile.SourceFolder;
        }

        private void DrawCatalog(SerializedProperty catalogProperty)
        {
            var profile = LubanSettings.instance.Profile;
            string dataPath = profile == null ? string.Empty : AssetDatabase.GetAssetPath(profile.DataFolder);
            var generated = string.IsNullOrEmpty(dataPath) ? null : AssetDatabase.LoadAssetAtPath<LubanDataTableCatalog>(dataPath + "/Catalog.asset");
            var catalog = catalogProperty.objectReferenceValue as LubanDataTableCatalog;
            if (generated != null && generated != catalog)
            {
                EditorGUILayout.HelpBox("当前组件的 Catalog 与全局 Profile 的生成结果不同。切换生成方案不会自动修改运行时引用。", MessageType.Info);
                using (new EditorGUI.DisabledScope(!LubanMenu.CanGenerate))
                    if (GUILayout.Button("绑定全局 Profile 的 Catalog")) catalogProperty.objectReferenceValue = catalog = generated;
            }
            if (catalog == null)
            {
                EditorGUILayout.HelpBox("请先生成配置，再指定 Catalog；也可由业务通过资源路径加载 Catalog。", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("Luban 版本", catalog.GeneratorVersion);
            m_ShowCatalog = EditorGUILayout.Foldout(m_ShowCatalog, "生成数据（" + catalog.Entries.Count + " 张表）", true);
            if (!m_ShowCatalog) return;
            using (new EditorGUI.DisabledScope(true))
                foreach (var entry in catalog.Entries)
                    if (entry != null) EditorGUILayout.ObjectField(entry.Name, entry.Data, typeof(TextAsset), false);
        }
    }
}
