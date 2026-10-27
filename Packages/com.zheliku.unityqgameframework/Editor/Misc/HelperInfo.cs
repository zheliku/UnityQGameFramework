//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 记录辅助器类型及其显示名称。
    /// </summary>
    /// <typeparam name="T">辅助器组件的基类型。</typeparam>
    internal sealed class HelperInfo<T> where T : MonoBehaviour
    {
        private const string CustomOptionName = "<Custom>";

        private readonly string m_Name;

        private SerializedProperty m_HelperTypeName;
        private SerializedProperty m_CustomHelper;
        private string[] m_HelperTypeNames;
        private int m_HelperTypeNameIndex;

        /// <summary>
        /// 初始化编辑器辅助器信息。
        /// </summary>
        /// <param name="name">辅助器名称。</param>
        public HelperInfo(string name)
        {
            m_Name = name;

            m_HelperTypeName = null;
            m_CustomHelper = null;
            m_HelperTypeNames = null;
            m_HelperTypeNameIndex = 0;
        }

        /// <summary>
        /// 绑定辅助器类型和自定义对象属性。
        /// </summary>
        /// <param name="serializedObject">目标组件的序列化对象。</param>
        public void Init(SerializedObject serializedObject)
        {
            m_HelperTypeName = serializedObject.FindProperty(Utility.Text.Format("m_{0}HelperTypeName", m_Name));
            m_CustomHelper = serializedObject.FindProperty(Utility.Text.Format("m_Custom{0}Helper", m_Name));
        }

        /// <summary>
        /// 绘制辅助器类型选择和自定义对象字段。
        /// </summary>
        public void Draw()
        {
            string displayName = FieldNameForDisplay(m_Name);
            int selectedIndex = EditorGUILayout.Popup(Utility.Text.Format("{0} Helper", displayName), m_HelperTypeNameIndex, m_HelperTypeNames);
            if (selectedIndex != m_HelperTypeNameIndex)
            {
                m_HelperTypeNameIndex = selectedIndex;
                m_HelperTypeName.stringValue = selectedIndex <= 0 ? null : m_HelperTypeNames[selectedIndex];
            }

            if (m_HelperTypeNameIndex <= 0)
            {
                EditorGUILayout.PropertyField(m_CustomHelper);
                if (m_CustomHelper.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox(Utility.Text.Format("You must set Custom {0} Helper.", displayName), MessageType.Error);
                }
            }
        }

        /// <summary>
        /// 刷新可用辅助器类型。
        /// </summary>
        public void Refresh()
        {
            List<string> helperTypeNameList = new List<string>
            {
                CustomOptionName
            };

            helperTypeNameList.AddRange(Type.GetRuntimeTypeNames(typeof(T)));
            m_HelperTypeNames = helperTypeNameList.ToArray();

            m_HelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_HelperTypeName.stringValue))
            {
                m_HelperTypeNameIndex = helperTypeNameList.IndexOf(m_HelperTypeName.stringValue);
                if (m_HelperTypeNameIndex <= 0)
                {
                    m_HelperTypeNameIndex = 0;
                    m_HelperTypeName.stringValue = null;
                }
            }
        }

        /// <summary>
        /// 将序列化字段名转换为显示名称。
        /// </summary>
        /// <param name="fieldName">字段名称。</param>
        /// <returns>用于显示的字段名称。</returns>
        private string FieldNameForDisplay(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return string.Empty;
            }

            string str = Regex.Replace(fieldName, @"^m_", string.Empty);
            str = Regex.Replace(str, @"((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", @" $1").TrimStart();
            return str;
        }
    }
}
