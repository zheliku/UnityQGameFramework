using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityQGameFramework.Core;
using YooAsset;
using TMPro;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class LocalizationComponent
    {
        private static readonly UTF8Encoding s_Utf8 = new UTF8Encoding(false, true);

        [SerializeField] private LocalizationLanguage[] m_Languages = Array.Empty<LocalizationLanguage>();
        [SerializeField] private string m_FallbackLanguageCode = Language.English.Code;
        private LocalizationHelperBase m_LocalizationHelper;
        private CancellationTokenSource m_LanguageCancellation;
        private Dictionary<string, AssetHandle> m_LanguageAssets = new Dictionary<string, AssetHandle>(StringComparer.Ordinal);

        /// <summary>
        /// 获取是否正在准备新的语言。
        /// </summary>
        public bool IsChangingLanguage
        {
            get; private set;
        }

        /// <summary>
        /// 获取语言配置；调用方不修改集合。
        /// </summary>
        public IReadOnlyList<LocalizationLanguage> Languages
        {
            get
            {
                return m_Languages;
            }
        }

        /// <summary>
        /// 设置语言配置。
        /// </summary>
        /// <param name="languages">语言配置；数组会复制，已有语言保持可用。</param>
        public void SetLanguages(params LocalizationLanguage[] languages)
        {
            if (IsChangingLanguage)
            {
                throw new GameFrameworkException("Wait for the current language change before replacing its configuration.");
            }
            LocalizationLanguage[] previous = m_Languages;
            m_Languages = languages == null ? Array.Empty<LocalizationLanguage>() : (LocalizationLanguage[])languages.Clone();
            try
            {
                ValidateLanguages();
            }
            catch
            {
                m_Languages = previous;
                throw;
            }
        }

        /// <summary>
        /// 验证语言标识、资源映射与回退链。
        /// </summary>
        public void ValidateLanguages()
        {
            Dictionary<Language, LocalizationLanguage> languages = GetLanguageMap();
            foreach (Language language in languages.Keys)
            {
                GetLanguageChain(language, languages);
            }
        }

        /// <summary>
        /// 准备完整字典和语言资源，成功后一次性切换并通知界面。
        /// </summary>
        /// <param name="language">已配置的标准语言标识。</param>
        /// <param name="cancellationToken">取消本次切换；失败或取消时保留旧语言。</param>
        /// <returns>语言切换任务；较新的请求会取消尚未提交的旧请求。</returns>
        public async UniTask SetLanguageAsync(Language language, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (m_LocalizationHelper == null)
            {
                throw new GameFrameworkException("Access Localization after component initialization.");
            }
            List<LocalizationLanguage> chain = GetLanguageChain(language);
            m_LanguageCancellation?.Cancel();
            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.GetCancellationTokenOnDestroy());
            m_LanguageCancellation = cancellation;
            IsChangingLanguage = true;
            var assets = new Dictionary<string, AssetHandle>(StringComparer.Ordinal);
            bool committed = false;
            try
            {
                ResourceComponent resources = GameEntry.GetComponent<ResourceComponent>();
                var texts = new Dictionary<string, string>(StringComparer.Ordinal);
                var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
                var resourceNames = new Dictionary<string, LocalizationAssetMapping>(StringComparer.Ordinal);
                // 先加入最低优先级的回退语言，再由目标语言覆盖同名键。
                for (int index = chain.Count - 1; index >= 0; index--)
                {
                    LocalizationLanguage configuration = chain[index];
                    string address = configuration.DictionaryAssetName;
                    if (!texts.TryGetValue(address, out string text))
                    {
                        byte[] bytes = await resources.LoadBinaryAsync(address, cancellationToken: cancellation.Token);
                        text = s_Utf8.GetString(bytes);
                        texts.Add(address, text);
                    }
                    cancellation.Token.ThrowIfCancellationRequested();
                    foreach (KeyValuePair<string, string> entry in m_LocalizationHelper.ParseDictionary(text, configuration.Language))
                    {
                        dictionary[entry.Key] = entry.Value;
                    }
                    foreach (LocalizationAssetMapping asset in configuration.Assets)
                    {
                        resourceNames[asset.Key] = asset;
                    }
                }
                foreach (KeyValuePair<string, LocalizationAssetMapping> entry in resourceNames)
                {
                    Type assetType = typeof(UnityEngine.Object);
                    if (entry.Value.AssetType == LocalizationAssetType.Sprite)
                    {
                        assetType = typeof(Sprite);
                    }
                    else if (entry.Value.AssetType == LocalizationAssetType.Font)
                    {
                        assetType = typeof(TMP_FontAsset);
                    }
                    assets.Add(entry.Key, await resources.LoadAssetHandleAsync(entry.Value.AssetName, assetType, cancellationToken: cancellation.Token));
                }
                cancellation.Token.ThrowIfCancellationRequested();
                Dictionary<string, AssetHandle> previous = m_LanguageAssets;
                m_LanguageAssets = assets;
                try
                {
                    m_LocalizationManager.SetLanguage(language, dictionary);
                    committed = true;
                }
                finally
                {
                    if (committed)
                    {
                        ReleaseLanguageAssets(previous);
                    }
                    else
                    {
                        m_LanguageAssets = previous;
                    }
                }
            }
            finally
            {
                if (!committed)
                {
                    ReleaseLanguageAssets(assets);
                }
                if (ReferenceEquals(m_LanguageCancellation, cancellation))
                {
                    m_LanguageCancellation = null;
                    IsChangingLanguage = false;
                }
                cancellation.Dispose();
            }
        }

        /// <summary>
        /// 获取当前语言资源；借用的资源不可由调用方释放。
        /// </summary>
        /// <typeparam name="T">字体、图片等 Unity 资源类型。</typeparam>
        /// <param name="key">语言资源业务键。</param>
        /// <returns>当前资源；没有配置时返回空，类型不匹配时抛出异常。</returns>
        public T GetAsset<T>(string key) where T : UnityEngine.Object
        {
            if (!m_LanguageAssets.TryGetValue(key, out AssetHandle handle))
            {
                return null;
            }
            if (!(handle.AssetObject is T result))
            {
                throw new GameFrameworkException("Localized asset has an unexpected type: " + key);
            }
            return result;
        }

        /// <summary>
        /// 使用位置参数或命名字典格式化文本。
        /// </summary>
        /// <param name="key">字典主键。</param>
        /// <param name="arguments">格式参数；命名参数传入一个字典。</param>
        /// <returns>格式化结果或带标记的诊断文本。</returns>
        public string GetString(string key, params object[] arguments)
        {
            return m_LocalizationManager.GetString(key, arguments);
        }

        /// <summary>
        /// 获取已验证的语言配置索引。
        /// </summary>
        /// <returns>以强类型语言值为键的配置。</returns>
        private Dictionary<Language, LocalizationLanguage> GetLanguageMap()
        {
            var result = new Dictionary<Language, LocalizationLanguage>();
            foreach (LocalizationLanguage configuration in m_Languages)
            {
                if (configuration == null || configuration.Language == Language.Unspecified || string.IsNullOrWhiteSpace(configuration.DictionaryAssetName))
                {
                    throw new GameFrameworkException("Each language requires a language value and dictionary address.");
                }
                Language language = configuration.Language;
                if (result.ContainsKey(language))
                {
                    throw new GameFrameworkException("Duplicate language configuration: " + language);
                }
                var keys = new HashSet<string>(StringComparer.Ordinal);
                foreach (LocalizationAssetMapping asset in configuration.Assets)
                {
                    if (asset == null || string.IsNullOrWhiteSpace(asset.Key) || string.IsNullOrWhiteSpace(asset.AssetName)
                        || !Enum.IsDefined(typeof(LocalizationAssetType), asset.AssetType) || !keys.Add(asset.Key))
                    {
                        throw new GameFrameworkException("Localized asset mappings require unique keys and valid addresses: " + language);
                    }
                }
                result.Add(language, configuration);
            }
            if (result.Count > 0 && !string.IsNullOrEmpty(m_FallbackLanguageCode)
                && !result.ContainsKey(Language.FromCode(m_FallbackLanguageCode)))
            {
                throw new GameFrameworkException("The final fallback language is not configured: " + m_FallbackLanguageCode);
            }
            return result;
        }

        /// <summary>
        /// 获取目标语言到最终回退语言的链，并检查循环。
        /// </summary>
        /// <param name="language">目标语言。</param>
        /// <param name="languages">已验证的语言配置索引；为空时从组件读取。</param>
        /// <returns>按优先级排列的语言配置。</returns>
        internal List<LocalizationLanguage> GetLanguageChain(Language language, Dictionary<Language, LocalizationLanguage> languages = null)
        {
            languages = languages ?? GetLanguageMap();
            var result = new List<LocalizationLanguage>();
            var visited = new HashSet<Language>();
            while (language != Language.Unspecified)
            {
                if (!visited.Add(language))
                {
                    throw new GameFrameworkException("Language fallback cycle: " + language);
                }
                if (!languages.TryGetValue(language, out LocalizationLanguage configuration))
                {
                    throw new GameFrameworkException("Language is not configured: " + language);
                }
                result.Add(configuration);
                if (configuration.FallbackLanguage != Language.Unspecified)
                {
                    language = configuration.FallbackLanguage;
                    continue;
                }
                Language parent = language.Parent;
                language = Language.Unspecified;
                while (parent != Language.Unspecified)
                {
                    if (languages.ContainsKey(parent))
                    {
                        language = parent;
                        break;
                    }
                    parent = parent.Parent;
                }
                if (language == Language.Unspecified && !string.IsNullOrEmpty(m_FallbackLanguageCode))
                {
                    Language fallback = Language.FromCode(m_FallbackLanguageCode);
                    if (languages.ContainsKey(fallback) && !visited.Contains(fallback))
                    {
                        language = fallback;
                    }
                }
            }
            if (result.Count == 0)
            {
                throw new GameFrameworkException("Language is unspecified.");
            }
            return result;
        }

        /// <summary>
        /// 在旧语言资源归还前通知界面刷新。
        /// </summary>
        /// <param name="sender">Core 管理器。</param>
        /// <param name="args">已提交的语言标识。</param>
        private void OnLanguageChanged(object sender, Core.Events.LanguageChangedEventArgs args)
        {
            m_EventComponent.FireNow(this, LanguageChangedEventArgs.Create(args));
        }

        /// <summary>
        /// 结束切换请求并释放组件持有的语言资源。
        /// </summary>
        private void OnDestroy()
        {
            m_LanguageCancellation?.Cancel();
            if (m_LocalizationManager != null)
            {
                m_LocalizationManager.LanguageChanged -= OnLanguageChanged;
                m_LocalizationManager.ReadDataSuccess -= OnReadDataSuccess;
                m_LocalizationManager.ReadDataFailure -= OnReadDataFailure;
                m_LocalizationManager.ReadDataUpdate -= OnReadDataUpdate;
                m_LocalizationManager.ReadDataDependencyAsset -= OnReadDataDependencyAsset;
            }
            ReleaseLanguageAssets(m_LanguageAssets);
        }

        /// <summary>
        /// 归还一个语言快照持有的资源句柄。
        /// </summary>
        /// <param name="assets">将被清空的资源句柄集合。</param>
        private static void ReleaseLanguageAssets(Dictionary<string, AssetHandle> assets)
        {
            foreach (AssetHandle handle in assets.Values)
            {
                if (handle.IsValid)
                {
                    handle.Release();
                }
            }
            assets.Clear();
        }
    }
}
