using System;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 用标准语言列表与已配置的回退语言绘制语言条目。
    /// </summary>
    [CustomPropertyDrawer(typeof(LocalizationLanguage))]
    internal sealed class LocalizationLanguageDrawer : PropertyDrawer
    {
        /// <summary>
        /// 绘制语言、字典地址和资源映射。
        /// </summary>
        /// <param name="position">绘制区域。</param>
        /// <param name="property">语言配置。</param>
        /// <param name="label">条目标签。</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            SerializedProperty code = property.FindPropertyRelative("m_Code");
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, GetLabel(code.stringValue), true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                DrawCode(row, code, new GUIContent("Language"));
                row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(row, property.FindPropertyRelative("m_DictionaryAssetName"),
                    new GUIContent("Dictionary Address", "Use an address collected by the default YooAsset package."));
                row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                DrawConfiguredCode(row, property.FindPropertyRelative("m_FallbackCode"), new GUIContent("Fallback Language"), "Automatic (parent, then final fallback)");
                row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                SerializedProperty assets = property.FindPropertyRelative("m_Assets");
                row.height = EditorGUI.GetPropertyHeight(assets, true);
                EditorGUI.PropertyField(row, assets, new GUIContent("Localized Assets"), true);
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// 获取当前折叠状态所需的高度。
        /// </summary>
        /// <param name="property">语言配置。</param>
        /// <param name="label">条目标签。</param>
        /// <returns>条目高度。</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 4
                + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_Assets"), true);
        }

        /// <summary>
        /// 绘制可搜索的标准语言选择按钮。
        /// </summary>
        /// <param name="position">绘制区域。</param>
        /// <param name="property">保存语言代码的字符串属性。</param>
        /// <param name="label">属性标签。</param>
        internal static void DrawCode(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect button = EditorGUI.PrefixLabel(position, label);
            if (EditorGUI.DropdownButton(button, new GUIContent(GetLabel(property.stringValue)), FocusType.Keyboard))
            {
                new LanguageDropdown(property).Show(button);
            }
        }

        /// <summary>
        /// 从当前组件已配置的语言中选择回退语言。
        /// </summary>
        /// <param name="position">绘制区域。</param>
        /// <param name="property">回退语言属性。</param>
        /// <param name="label">属性标签。</param>
        /// <param name="emptyLabel">未指定语言时的提示。</param>
        internal static void DrawConfiguredCode(Rect position, SerializedProperty property, GUIContent label, string emptyLabel)
        {
            SerializedProperty languages = property.serializedObject.FindProperty("m_Languages");
            string[] codes = Enumerable.Range(0, languages.arraySize)
                .Select(index => languages.GetArrayElementAtIndex(index).FindPropertyRelative("m_Code").stringValue)
                .Where(code => !string.IsNullOrWhiteSpace(code)).Distinct(StringComparer.OrdinalIgnoreCase).Prepend(string.Empty).ToArray();
            int selected = Array.FindIndex(codes, code => code.Equals(property.stringValue, StringComparison.OrdinalIgnoreCase));
            if (selected < 0)
            {
                codes = codes.Append(property.stringValue).ToArray();
                selected = codes.Length - 1;
            }
            string[] labels = codes.Select(code => string.IsNullOrEmpty(code) ? emptyLabel : GetLabel(code)).ToArray();
            EditorGUI.BeginChangeCheck();
            int next = EditorGUI.Popup(position, label, selected, labels.Select(text => new GUIContent(text)).ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = codes[next];
            }
        }

        /// <summary>
        /// 显示语言名称与标准代码，保留无效输入以便修正。
        /// </summary>
        /// <param name="code">语言代码。</param>
        /// <returns>可读的语言标签。</returns>
        internal static string GetLabel(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return "Select Language";
            }
            try
            {
                return CultureInfo.GetCultureInfo(code).EnglishName + " (" + code + ")";
            }
            catch (ArgumentException)
            {
                return code;
            }
        }

        /// <summary>
        /// 提供语言名称和区域代码搜索，不把运行时语言限制为 UGF 枚举。
        /// </summary>
        private sealed class LanguageDropdown : AdvancedDropdown
        {
            private static readonly CultureInfo[] s_Cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures | CultureTypes.SpecificCultures)
                .Where(culture => culture.Name.Length > 0 && culture.Name.IndexOf('_') < 0)
                .OrderBy(culture => culture.EnglishName, StringComparer.Ordinal).ToArray();
            private readonly UnityEngine.Object[] m_Targets;
            private readonly string m_PropertyPath;

            /// <summary>
            /// 保存选择回调所需的序列化目标，避免捕获已失效的属性。
            /// </summary>
            /// <param name="property">要更新的语言代码属性。</param>
            internal LanguageDropdown(SerializedProperty property) : base(new AdvancedDropdownState())
            {
                m_Targets = property.serializedObject.targetObjects;
                m_PropertyPath = property.propertyPath;
                minimumSize = new Vector2(360, 320);
            }

            /// <summary>
            /// 构建可搜索的平面语言列表。
            /// </summary>
            /// <returns>语言选择根节点。</returns>
            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("Languages");
                root.AddChild(new AdvancedDropdownItem("Unspecified") { id = -1 });
                for (int index = 0; index < s_Cultures.Length; index++)
                {
                    root.AddChild(new AdvancedDropdownItem(GetLabel(s_Cultures[index].Name)) { id = index });
                }
                return root;
            }

            /// <summary>
            /// 保存所选语言，支持 Unity 撤销与预制体覆盖。
            /// </summary>
            /// <param name="item">选中的语言项。</param>
            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                using (var serialized = new SerializedObject(m_Targets))
                {
                    serialized.Update();
                    serialized.FindProperty(m_PropertyPath).stringValue = item.id < 0 ? string.Empty : s_Cultures[item.id].Name;
                    serialized.ApplyModifiedProperties();
                }
            }
        }
    }
}
