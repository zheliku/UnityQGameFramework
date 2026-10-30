using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 根据当前语言刷新 uGUI 文本和可选字体。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    [AddComponentMenu("Unity Q Game Framework/Localization/Localize Text")]
    public sealed class LocalizeText : LocalizeComponent
    {
        [SerializeField, Tooltip("Optional font asset key from LocalizationComponent > Languages > Localized Assets.")]
        private string m_FontKey;
        private Text m_Text;
        private Font m_OriginalFont;
        private object[] m_Arguments;

        /// <summary>
        /// 保存预制体字体，在物体隐藏时还原。
        /// </summary>
        private void Awake()
        {
            m_Text = GetComponent<Text>();
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
        /// 使用当前语言刷新文本和已准备的字体。
        /// </summary>
        public override void Refresh()
        {
            if (m_Text == null || Localization == null || string.IsNullOrEmpty(Key))
            {
                Restore();
                return;
            }
            m_Text.text = m_Arguments == null ? Localization.GetString(Key) : Localization.GetString(Key, m_Arguments);
            m_Text.font = string.IsNullOrEmpty(m_FontKey) ? m_OriginalFont : Localization.GetAsset<Font>(m_FontKey) ?? m_OriginalFont;
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
