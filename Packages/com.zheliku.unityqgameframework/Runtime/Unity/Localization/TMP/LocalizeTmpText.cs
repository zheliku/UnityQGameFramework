using System;
using TMPro;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 根据当前语言刷新 TMP 文本和可选字体。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    [AddComponentMenu("Unity Q Game Framework/Localization/Localize TMP Text")]
    public sealed class LocalizeTmpText : LocalizeComponent
    {
        [SerializeField, Tooltip("Optional TMP font asset key. Use the Object load type in LocalizationComponent.")]
        private string m_FontKey;
        private TMP_Text m_Text;
        private TMP_FontAsset m_OriginalFont;
        private object[] m_Arguments;

        /// <summary>
        /// 保存预制体字体。
        /// </summary>
        private void Awake()
        {
            m_Text = GetComponent<TMP_Text>();
            m_OriginalFont = m_Text.font;
        }

        /// <summary>
        /// 设置动态格式参数并立即刷新。
        /// </summary>
        /// <param name="arguments">位置参数或命名字典；数组会复制。</param>
        public void SetArguments(params object[] arguments)
        {
            m_Arguments = arguments == null ? Array.Empty<object>() : (object[])arguments.Clone();
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
            m_Text.text = m_Arguments == null ? Localization.GetString(Key) : Localization.GetString(Key, m_Arguments);
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
            }
        }
    }
}
