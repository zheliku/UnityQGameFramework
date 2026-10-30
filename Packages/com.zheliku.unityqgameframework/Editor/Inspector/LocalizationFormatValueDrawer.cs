using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 按类型显示命名格式值，避免同时展开无关字段。
    /// </summary>
    [CustomPropertyDrawer(typeof(LocalizationFormatValue))]
    internal sealed class LocalizationFormatValueDrawer : PropertyDrawer
    {
        /// <summary>
        /// 绘制参数名称、类型和值。
        /// </summary>
        /// <param name="position">字段区域。</param>
        /// <param name="property">格式值配置。</param>
        /// <param name="label">条目标签。</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float width = position.width;
            position.width = width * .38f;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("m_Name"), GUIContent.none);
            position.x += position.width + 4f;
            position.width = width * .23f;
            SerializedProperty type = property.FindPropertyRelative("m_Type");
            EditorGUI.PropertyField(position, type, GUIContent.none);
            position.x += position.width + 4f;
            position.width = width * .39f - 8f;
            string[] fields = { "m_Text", "m_Integer", "m_Number", "m_Boolean" };
            EditorGUI.PropertyField(position, property.FindPropertyRelative(fields[Mathf.Clamp(type.enumValueIndex, 0, fields.Length - 1)]), GUIContent.none);
            EditorGUI.EndProperty();
        }
    }
}
