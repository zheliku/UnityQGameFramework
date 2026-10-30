using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 从当前语言已准备的资源中刷新 uGUI 图片。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("Unity Q Game Framework/Localization/Localize Image")]
    public sealed class LocalizeImage : LocalizeComponent
    {
        private Image m_Image;
        private Sprite m_OriginalSprite;

        /// <summary>
        /// 保存预制体图片。
        /// </summary>
        private void Awake()
        {
            m_Image = GetComponent<Image>();
            m_OriginalSprite = m_Image.sprite;
        }

        /// <summary>
        /// 应用当前语言的图片；未配置时还原预制体图片。
        /// </summary>
        public override void Refresh()
        {
            if (m_Image != null && Localization != null && !string.IsNullOrEmpty(Key))
            {
                m_Image.sprite = Localization.GetAsset<Sprite>(Key) ?? m_OriginalSprite;
            }
            else
            {
                Restore();
            }
        }

        /// <summary>
        /// 还原预制体图片。
        /// </summary>
        protected override void Restore()
        {
            if (m_Image != null)
            {
                m_Image.sprite = m_OriginalSprite;
            }
        }
    }
}
