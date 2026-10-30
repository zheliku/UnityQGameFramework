using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 根据当前语言刷新 TMP 文本和可选字体。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Unity Q Game Framework/Localization/Localize Text")]
    public sealed class LocalizeText : LocalizeComponent
    {
        [SerializeField, Tooltip("Optional TMP font asset key. Use the Font load type in LocalizationComponent.")]
        private string m_FontKey;
        [SerializeField, Tooltip("Initial named values. Runtime code can replace or update them.")]
        private LocalizationFormatValue[] m_InitialFormatValues = Array.Empty<LocalizationFormatValue>();
        [SerializeField, Tooltip("Display policy when the key is absent after all configured fallbacks.")]
        private MissingTranslationBehavior m_MissingTranslation;
        private TMP_Text m_Text;
        private string m_OriginalText;
        private TMP_FontAsset m_OriginalFont;
        private Material m_OriginalMaterial;
        private Dictionary<string, object> m_FormatValues;

        /// <summary>
        /// 获取或设置可选的语言字体键；留空时使用预制体字体。
        /// </summary>
        public string FontKey
        {
            get
            {
                return m_FontKey;
            }
            set
            {
                m_FontKey = value;
                Refresh();
            }
        }

        /// <summary>
        /// 保存预制体字体。
        /// </summary>
        private void Awake()
        {
            m_Text = GetComponent<TMP_Text>();
            m_OriginalFont = m_Text.font;
            m_OriginalMaterial = m_Text.fontSharedMaterial;
            m_OriginalText = m_Text.text;
            if (m_FormatValues == null && m_InitialFormatValues.Length > 0)
            {
                m_FormatValues = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (LocalizationFormatValue value in m_InitialFormatValues)
                {
                    if (value == null || string.IsNullOrEmpty(value.Name) || m_FormatValues.ContainsKey(value.Name))
                    {
                        throw new ArgumentException("Initial format values require unique, non-empty names.");
                    }
                    m_FormatValues.Add(value.Name, value.GetValue());
                }
            }
        }

        /// <summary>
        /// 设置命名格式值并刷新文本；字典会复制。
        /// </summary>
        /// <param name="values">与翻译中占位符名称对应的值；为空时清除所有值。</param>
        public void SetFormatValues(IReadOnlyDictionary<string, object> values)
        {
            var copy = new Dictionary<string, object>(StringComparer.Ordinal);
            if (values != null)
            {
                foreach (KeyValuePair<string, object> entry in values)
                {
                    if (string.IsNullOrEmpty(entry.Key))
                    {
                        throw new ArgumentException("Format value names cannot be empty.", nameof(values));
                    }
                    copy.Add(entry.Key, entry.Value);
                }
            }
            m_FormatValues = copy;
            Refresh();
        }

        /// <summary>
        /// 更新一个命名格式值并刷新文本。
        /// </summary>
        /// <param name="name">翻译中的占位符名称，不含大括号。</param>
        /// <param name="value">用于格式化的值。</param>
        public void SetFormatValue(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Format value name cannot be empty.", nameof(name));
            }
            if (m_FormatValues == null)
            {
                m_FormatValues = new Dictionary<string, object>(StringComparer.Ordinal);
            }
            m_FormatValues[name] = value;
            Refresh();
        }

        /// <summary>
        /// 刷新当前语言文本与字体。
        /// </summary>
        public override void Refresh()
        {
            if (m_Text == null || Localization == null || string.IsNullOrEmpty(Key))
            {
                Restore();
                return;
            }
            if (!Localization.HasRawString(Key) && m_MissingTranslation != MissingTranslationBehavior.ShowKey)
            {
                m_Text.text = m_MissingTranslation == MissingTranslationBehavior.KeepOriginal ? m_OriginalText : string.Empty;
                Restore();
                return;
            }
            m_Text.text = m_FormatValues == null ? Localization.GetString(Key) : Localization.GetString(Key, m_FormatValues);
            m_Text.font = string.IsNullOrEmpty(m_FontKey) ? m_OriginalFont : Localization.GetAsset<TMP_FontAsset>(m_FontKey) ?? m_OriginalFont;
        }

        /// <summary>
        /// 还原预制体字体。
        /// </summary>
        protected override void Restore()
        {
            if (m_Text != null)
            {
                m_Text.font = m_OriginalFont;
                m_Text.fontSharedMaterial = m_OriginalMaterial;
            }
        }
    }
}
