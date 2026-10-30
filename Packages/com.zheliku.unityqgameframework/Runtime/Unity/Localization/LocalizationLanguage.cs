using System;
using System.Collections.Generic;
using UnityEngine;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 描述一种语言的字典、回退关系和随语言切换的资源。
    /// </summary>
    [Serializable]
    public sealed class LocalizationLanguage
    {
        [SerializeField] private string m_Code = Language.English.Code;
        [SerializeField, Tooltip("Dictionary address collected by the default YooAsset package.")]
        private string m_DictionaryAssetName;
        [SerializeField] private string m_FallbackCode;
        [SerializeField] private LocalizationAssetMapping[] m_Assets = Array.Empty<LocalizationAssetMapping>();

        /// <summary>
        /// 初始化语言配置。
        /// </summary>
        /// <param name="language">标准语言标识。</param>
        /// <param name="dictionaryAssetName">YooAsset 字典地址。</param>
        /// <param name="fallbackLanguage">回退语言；为 Unspecified 时按区域父语言查找。</param>
        /// <param name="assets">需要随字典一起准备的资源；数组会复制。</param>
        public LocalizationLanguage(Language language, string dictionaryAssetName, Language fallbackLanguage = default, params LocalizationAssetMapping[] assets)
        {
            m_Code = language.Code;
            m_DictionaryAssetName = dictionaryAssetName;
            m_FallbackCode = fallbackLanguage.Code;
            m_Assets = assets == null ? Array.Empty<LocalizationAssetMapping>() : (LocalizationAssetMapping[])assets.Clone();
        }

        /// <summary>
        /// 获取语言值。
        /// </summary>
        public Language Language
        {
            get
            {
                return string.IsNullOrEmpty(m_Code) ? Language.Unspecified : Language.FromCode(m_Code);
            }
        }

        /// <summary>
        /// 获取字典资源地址。
        /// </summary>
        public string DictionaryAssetName
        {
            get
            {
                return m_DictionaryAssetName;
            }
        }

        /// <summary>
        /// 获取显式回退语言；为 Unspecified 时使用区域父语言。
        /// </summary>
        public Language FallbackLanguage
        {
            get
            {
                return string.IsNullOrEmpty(m_FallbackCode) ? Language.Unspecified : Language.FromCode(m_FallbackCode);
            }
        }

        /// <summary>
        /// 获取语言资源；调用方不修改集合内容。
        /// </summary>
        public IReadOnlyList<LocalizationAssetMapping> Assets
        {
            get
            {
                return m_Assets;
            }
        }
    }

    /// <summary>
    /// 将字体、图片等语言资源映射到稳定的业务键。
    /// </summary>
    [Serializable]
    public sealed class LocalizationAssetMapping
    {
        [SerializeField, Tooltip("Stable key used by LocalizeText, LocalizeImage or GetAsset<T>.")]
        private string m_Key;
        [SerializeField, Tooltip("Asset address collected by the default YooAsset package.")]
        private string m_AssetName;
        [SerializeField, Tooltip("Use Sprite for images, Font for TMP font assets, or Object for other main assets.")]
        private LocalizationAssetType m_AssetType;

        /// <summary>
        /// 初始化语言资源映射。
        /// </summary>
        /// <param name="key">业务键，如 UI.Font 或 UI.Logo。</param>
        /// <param name="assetName">YooAsset 资源地址。</param>
        /// <param name="assetType">加载类型；图片使用 Sprite，TMP 字体使用 Font。</param>
        public LocalizationAssetMapping(string key, string assetName, LocalizationAssetType assetType = LocalizationAssetType.Object)
        {
            m_Key = key;
            m_AssetName = assetName;
            m_AssetType = assetType;
        }

        /// <summary>
        /// 获取业务键。
        /// </summary>
        public string Key
        {
            get
            {
                return m_Key;
            }
        }

        /// <summary>
        /// 获取资源地址。
        /// </summary>
        public string AssetName
        {
            get
            {
                return m_AssetName;
            }
        }

        /// <summary>
        /// 获取资源加载类型。
        /// </summary>
        public LocalizationAssetType AssetType
        {
            get
            {
                return m_AssetType;
            }
        }
    }

    /// <summary>
    /// 指定语言资源的加载类型，避免图片被加载为纹理。
    /// </summary>
    public enum LocalizationAssetType
    {
        /// <summary>
        /// 加载资源文件的主资产，适用于 TMP 字体等独立资产。
        /// </summary>
        Object,

        /// <summary>
        /// 加载 uGUI 使用的图片。
        /// </summary>
        Sprite,

        /// <summary>
        /// 加载 TMP 字体资产。
        /// </summary>
        Font
    }
}
