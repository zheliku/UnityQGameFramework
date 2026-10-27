using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Core.DataTable;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 绘制数据表的生成配置、运行时数据与已加载记录。
    /// </summary>
    [CustomEditor(typeof(DataTableComponent))]
    internal sealed class DataTableComponentInspector : FrameworkInspector
    {
        private string m_Filter = string.Empty;
        private int m_Selected;
        private int m_Page;
        private LubanProfile m_Profile;
        private LubanProfile m_LastGlobalProfile;
        private bool m_ShowCatalog;
        private bool m_ShowProfile;
        private UnityEditor.Editor m_ProfileEditor;
        private DataTableBase m_LastBrowsedTable;
        private const int PageSize = 20;

        /// <summary>
        /// 初始化序列化属性和编辑器缓存。
        /// </summary>
        private void OnEnable()
        {
            m_Profile = m_LastGlobalProfile = LubanSettings.instance.Profile;
        }

        /// <summary>
        /// 销毁嵌入的 Luban profile 编辑器。
        /// </summary>
        private void OnDisable()
        {
            if (m_ProfileEditor != null)
            {
                DestroyImmediate(m_ProfileEditor);
            }
        }

        /// <summary>
        /// 绘制数据表组件。
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            DrawLubanTools();
            var catalogProperty = serializedObject.FindProperty("m_LubanCatalog");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("运行时数据", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.PropertyField(catalogProperty, new GUIContent("数据清单 Catalog", "生成的 bytes 引用和结构版本；运行时从这里加载数据，不是缓存。"));
            }

            EditorGUILayout.HelpBox("Profile 用于编辑器生成；Catalog 指定游戏加载的数据。切换全局生成 Profile 不会自动更换此 Catalog。", MessageType.Info);
            DrawCatalog(catalogProperty);
            serializedObject.ApplyModifiedProperties();
            var component = (DataTableComponent)target;
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            EditorGUILayout.LabelField("加载状态", component.IsLoading ? "加载中" : component.IsLoaded ? "已加载" : "未加载");
            EditorGUILayout.LabelField("已注册数据表", component.Count.ToString());
            var tables = component.GetAllDataTables();
            if (tables.Length == 0)
            {
                return;
            }

            var names = Array.ConvertAll(tables, t => t.Type.Name + " · " + t.Count);
            m_Selected = Mathf.Clamp(m_Selected, 0, tables.Length - 1);
            int selection = EditorGUILayout.Popup("数据表", m_Selected, names);
            if (selection != m_Selected)
            {
                m_Selected = selection;
                m_Page = 0;
            }

            if (tables[m_Selected] != m_LastBrowsedTable)
            {
                m_LastBrowsedTable = tables[m_Selected];
                m_Page = 0;
            }

            string filter = EditorGUILayout.TextField("筛选", m_Filter);
            if (filter != m_Filter)
            {
                m_Filter = filter;
                m_Page = 0;
            }

            var rows = tables[m_Selected] as IEnumerable;
            if (rows == null)
            {
                EditorGUILayout.HelpBox("此数据表未提供记录枚举。", MessageType.Info);
                return;
            }

            int index = 0;
            foreach (object row in rows)
            {
                if (!string.IsNullOrEmpty(m_Filter) && row.ToString().IndexOf(m_Filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (index++ < m_Page * PageSize || index > (m_Page + 1) * PageSize)
                {
                    continue;
                }

                EditorGUILayout.LabelField(row.ToString(), EditorStyles.wordWrappedLabel);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(m_Page == 0))
                {
                    if (GUILayout.Button("上一页"))
                    {
                        --m_Page;
                    }
                }

                EditorGUILayout.LabelField((m_Page + 1).ToString(), GUILayout.Width(30));
                using (new EditorGUI.DisabledScope(index <= (m_Page + 1) * PageSize))
                {
                    if (GUILayout.Button("下一页"))
                    {
                        ++m_Page;
                    }
                }
            }
        }

        /// <summary>
        /// 绘制 Luban 配置生成入口。
        /// </summary>
        private void DrawLubanTools()
        {
            var settings = LubanSettings.instance;
            if (settings.Profile != m_LastGlobalProfile)
            {
                if (m_Profile == m_LastGlobalProfile)
                {
                    m_Profile = settings.Profile;
                }

                m_LastGlobalProfile = settings.Profile;
            }

            EditorGUILayout.LabelField("Luban 生成（编辑器）", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!LubanMenu.CanGenerate))
            {
                m_Profile = (LubanProfile)EditorGUILayout.ObjectField(new GUIContent("生成 Profile", "源表与输出目录配置；选择后点击设为全局，生成菜单才会使用它。"), m_Profile, typeof(LubanProfile), false);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(m_Profile == null || m_Profile == settings.Profile))
                    {
                        if (GUILayout.Button("设为全局"))
                        {
                            settings.Profile = m_Profile;
                        }
                    }

                    if (m_Profile == null && GUILayout.Button("创建默认配置"))
                    {
                        LubanMenu.CreateWorkspace();
                        m_Profile = settings.Profile;
                    }

                    using (new EditorGUI.DisabledScope(settings.Profile == null))
                    {
                        if (GUILayout.Button(new GUIContent("生成", "全局 Profile：" + AssetDatabase.GetAssetPath(settings.Profile))))
                        {
                            LubanMenu.Generate();
                        }
                    }
                }

                if (m_Profile != null)
                {
                    m_ShowProfile = EditorGUILayout.Foldout(m_ShowProfile, "生成配置", true);
                    if (m_ShowProfile)
                    {
                        CreateCachedEditor(m_Profile, null, ref m_ProfileEditor);
                        m_ProfileEditor.OnInspectorGUI();
                    }
                }
            }
        }

        /// <summary>
        /// 绘制已生成数据表目录信息。
        /// </summary>
        /// <param name="catalogProperty">数据表目录的序列化属性。</param>
        private void DrawCatalog(SerializedProperty catalogProperty)
        {
            var profile = LubanSettings.instance.Profile;
            string dataPath = profile == null ? string.Empty : AssetDatabase.GetAssetPath(profile.DataFolder);
            var generated = string.IsNullOrEmpty(dataPath) ? null : AssetDatabase.LoadAssetAtPath<LubanDataTableCatalog>(dataPath + "/Catalog.asset");
            var catalog = catalogProperty.objectReferenceValue as LubanDataTableCatalog;
            if (generated != null && generated != catalog)
            {
                using (new EditorGUI.DisabledScope(!LubanMenu.CanGenerate))
                {
                    if (GUILayout.Button(new GUIContent("使用全局 Catalog", "将组件的运行时数据切换为全局 Profile 的生成结果。")))
                    {
                        catalogProperty.objectReferenceValue = catalog = generated;
                    }
                }
            }

            if (catalog == null)
            {
                return;
            }

            m_ShowCatalog = EditorGUILayout.Foldout(m_ShowCatalog, "数据表 · " + catalog.Entries.Count, true);
            if (!m_ShowCatalog)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                foreach (var entry in catalog.Entries)
                {
                    if (entry != null)
                    {
                        EditorGUILayout.ObjectField(entry.Name, entry.Data, typeof(TextAsset), false);
                    }
                }
            }
        }
    }
}
