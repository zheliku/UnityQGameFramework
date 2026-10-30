using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SmartFormat;
using SmartFormat.Core.Parsing;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;
using YooAsset.Editor;
using TMPro;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 在构建前检查语言字典、回退链和收集资源。
    /// </summary>
    internal sealed class LocalizationBuildValidator : IProcessSceneWithReport
    {
        /// <summary>
        /// 获取场景处理顺序。
        /// </summary>
        public int callbackOrder
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// 校验构建场景中配置了语言的本地化组件。
        /// </summary>
        /// <param name="scene">正在构建的场景。</param>
        /// <param name="report">构建报告；编辑器进入运行模式时为空。</param>
        public void OnProcessScene(Scene scene, UnityEditor.Build.Reporting.BuildReport report)
        {
            if (report == null)
            {
                return;
            }
            foreach (LocalizationComponent component in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LocalizationComponent>(true)))
            {
                if (component.Languages.Count > 0)
                {
                    Validate(component);
                }
            }
        }

        /// <summary>
        /// 使用运行时相同的解析辅助器和回退顺序校验配置。
        /// </summary>
        /// <param name="component">待校验的场景组件。</param>
        internal static void Validate(LocalizationComponent component)
        {
            component.ValidateLanguages();
            if (component.Languages.Count == 0)
            {
                return;
            }
            ResourceComponent resources = component.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ResourceComponent>(true)).SingleOrDefault();
            if (resources == null || resources.Settings == null)
            {
                throw new BuildFailedException("Localization validation requires ResourceSettings in the same scene.");
            }
            BundleCollectorSetting collector = UnityEngine.Object.Instantiate(ResourceBuildService.GetCollector(resources.Settings));
            var helperObject = new GameObject("Localization Validation");
            helperObject.SetActive(false);
            helperObject.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                BundleCollectorPackage package = collector.GetPackage(resources.Settings.DefaultPackageName);
                ResourceBuildService.ResolveCollectorPaths(package);
                CollectResult collected = collector.BeginCollect(package.PackageName, true, false, true);
                Dictionary<string, string> locations = GetLocations(package, collected);
                LocalizationHelperBase helper = CreateParser(component, helperObject);
                var dictionaries = new Dictionary<Language, Dictionary<string, string>>();
                var assets = new Dictionary<Language, Dictionary<string, LocalizationAssetMapping>>();
                var requiredKeys = new HashSet<string>(StringComparer.Ordinal);
                foreach (LocalizationLanguage language in component.Languages)
                {
                    var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
                    var mappings = new Dictionary<string, LocalizationAssetMapping>(StringComparer.Ordinal);
                    List<LocalizationLanguage> chain = component.GetLanguageChain(language.Language);
                    for (int index = chain.Count - 1; index >= 0; index--)
                    {
                        LocalizationLanguage configuration = chain[index];
                        string path = Resolve(locations, configuration.DictionaryAssetName);
                        string text = new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path));
                        foreach (KeyValuePair<string, string> entry in helper.ParseDictionary(text, configuration.Language))
                        {
                            dictionary[entry.Key] = entry.Value;
                            requiredKeys.Add(entry.Key);
                        }
                        foreach (LocalizationAssetMapping mapping in configuration.Assets)
                        {
                            ValidateAsset(locations, mapping);
                            mappings[mapping.Key] = mapping;
                        }
                    }
                    dictionaries.Add(language.Language, dictionary);
                    assets.Add(language.Language, mappings);
                }
                ValidateTranslations(dictionaries, requiredKeys);
                IEnumerable<LocalizeComponent> bindings = component.gameObject.scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<LocalizeComponent>(true));
                // 收集到的 UI 预制体也会在 Player 中打开，不能只检查启动场景。
                foreach (string path in collected.CollectAssets.Select(asset => asset.AssetInfo.AssetPath).Distinct())
                {
                    if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        bindings = bindings.Concat(prefab.GetComponentsInChildren<LocalizeComponent>(true));
                    }
                }
                ValidateBindings(bindings, dictionaries, assets, locations);
            }
            catch (Exception exception)
            {
                throw new BuildFailedException("Localization validation failed in " + component.gameObject.scene.path + ": " + exception.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(helperObject);
                UnityEngine.Object.DestroyImmediate(collector);
            }
        }

        /// <summary>
        /// 在禁用物体上创建解析器，复制自定义辅助器的序列化配置。
        /// </summary>
        /// <param name="component">提供辅助器配置的组件。</param>
        /// <param name="owner">不会触发运行生命周期的临时物体。</param>
        /// <returns>仅用于纯文本解析的辅助器。</returns>
        private static LocalizationHelperBase CreateParser(LocalizationComponent component, GameObject owner)
        {
            var serialized = new SerializedObject(component);
            string name = serialized.FindProperty("m_LocalizationHelperTypeName").stringValue;
            var custom = serialized.FindProperty("m_CustomLocalizationHelper").objectReferenceValue as LocalizationHelperBase;
            System.Type type = string.IsNullOrEmpty(name) ? custom?.GetType()
                : TypeCache.GetTypesDerivedFrom<LocalizationHelperBase>().FirstOrDefault(candidate => candidate.FullName == name);
            if (type == null || type.IsAbstract)
            {
                throw new InvalidOperationException("A concrete localization parser is required.");
            }
            var helper = (LocalizationHelperBase)owner.AddComponent(type);
            if (string.IsNullOrEmpty(name) && custom != null)
            {
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(custom), helper);
            }
            return helper;
        }

        /// <summary>
        /// 按 YooAsset 清单规则建立可用的定位地址，不将 GUID 当作普通地址。
        /// </summary>
        /// <param name="package">资源包寻址配置。</param>
        /// <param name="collected">官方收集结果。</param>
        /// <returns>定位地址到资产路径的映射。</returns>
        private static Dictionary<string, string> GetLocations(BundleCollectorPackage package, CollectResult collected)
        {
            var locations = new Dictionary<string, string>(!package.EnableAddressable && package.LocationToLower
                ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            foreach (CollectAssetInfo asset in collected.CollectAssets.Where(asset => asset.CollectorType == ECollectorType.MainAssetCollector))
            {
                string path = asset.AssetInfo.AssetPath;
                locations.Add(path, path);
                if (package.SupportExtensionless)
                {
                    string extensionless = Path.ChangeExtension(path, null);
                    if (!locations.ContainsKey(extensionless))
                    {
                        locations.Add(extensionless, path);
                    }
                }
                if (package.EnableAddressable && !string.IsNullOrEmpty(asset.Address))
                {
                    locations.Add(asset.Address, path);
                }
            }
            return locations;
        }

        /// <summary>
        /// 解析实际运行时可访问的资源地址。
        /// </summary>
        /// <param name="locations">默认包的地址映射。</param>
        /// <param name="address">配置的资源地址。</param>
        /// <returns>已收集的资产路径。</returns>
        private static string Resolve(Dictionary<string, string> locations, string address)
        {
            if (!locations.TryGetValue(address, out string path))
            {
                throw new InvalidOperationException("Localized asset is not collected at address: " + address);
            }
            return path;
        }

        /// <summary>
        /// 检查资源存在并能以配置的类型加载。
        /// </summary>
        /// <param name="locations">默认包的地址映射。</param>
        /// <param name="mapping">语言资源映射。</param>
        private static void ValidateAsset(Dictionary<string, string> locations, LocalizationAssetMapping mapping)
        {
            string path = Resolve(locations, mapping.AssetName);
            System.Type type = mapping.AssetType == LocalizationAssetType.Sprite ? typeof(Sprite)
                : mapping.AssetType == LocalizationAssetType.Font ? typeof(TMP_FontAsset) : typeof(UnityEngine.Object);
            if (!AssetDatabase.LoadAllAssetsAtPath(path).Any(asset => asset != null && type.IsInstanceOfType(asset)))
            {
                throw new InvalidOperationException("Localized asset has an unexpected type: " + mapping.Key + " at " + path);
            }
        }

        /// <summary>
        /// 检查合并回退后的键和顶层格式参数，允许复数分支数量随语言变化。
        /// </summary>
        /// <param name="dictionaries">每种语言的完整字典。</param>
        /// <param name="keys">所有语言声明的键。</param>
        private static void ValidateTranslations(Dictionary<Language, Dictionary<string, string>> dictionaries, HashSet<string> keys)
        {
            var expected = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var formatter = new SmartFormatter();
            foreach (KeyValuePair<Language, Dictionary<string, string>> language in dictionaries)
            {
                if (language.Value.Count == 0)
                {
                    throw new InvalidOperationException("The language dictionary is empty: " + language.Key);
                }
                foreach (string key in keys)
                {
                    if (!language.Value.TryGetValue(key, out string text) || text == null)
                    {
                        throw new InvalidOperationException("Missing translation: " + language.Key + " / " + key);
                    }
                    var arguments = new HashSet<string>(StringComparer.Ordinal);
                    using (Format format = formatter.Parser.ParseFormat(text))
                    {
                        CollectArguments(format, arguments);
                    }
                    if (expected.TryGetValue(key, out HashSet<string> previous) && !previous.SetEquals(arguments))
                    {
                        throw new InvalidOperationException("Translation argument mismatch: " + language.Key + " / " + key);
                    }
                    expected[key] = arguments;
                }
            }
        }

        /// <summary>
        /// 从官方语法树收集根参数，忽略在当前参数作用域内的嵌套格式。
        /// </summary>
        /// <param name="format">已解析的文本格式。</param>
        /// <param name="results">接收参数名的集合，不会清空。</param>
        private static void CollectArguments(Format format, HashSet<string> results)
        {
            foreach (SmartFormat.Core.Parsing.Placeholder placeholder in format.Items.OfType<SmartFormat.Core.Parsing.Placeholder>())
            {
                var selectors = placeholder.GetSelectors();
                if (selectors.Count > 0)
                {
                    results.Add(selectors[0].RawText);
                }
            }
        }

        /// <summary>
        /// 检查场景和资源预制体中的声明式文本、字体和图片绑定。
        /// </summary>
        /// <param name="bindings">待校验的本地化绑定。</param>
        /// <param name="dictionaries">合并回退后的字典。</param>
        /// <param name="assets">合并回退后的资源映射。</param>
        /// <param name="locations">默认包的地址映射。</param>
        private static void ValidateBindings(IEnumerable<LocalizeComponent> bindings, Dictionary<Language, Dictionary<string, string>> dictionaries,
            Dictionary<Language, Dictionary<string, LocalizationAssetMapping>> assets, Dictionary<string, string> locations)
        {
            foreach (LocalizeComponent binding in bindings)
            {
                var serialized = new SerializedObject(binding);
                SerializedProperty fontKey = serialized.FindProperty("m_FontKey");
                SerializedProperty values = serialized.FindProperty("m_InitialFormatValues");
                if (values != null)
                {
                    var names = new HashSet<string>(StringComparer.Ordinal);
                    for (int index = 0; index < values.arraySize; index++)
                    {
                        string name = values.GetArrayElementAtIndex(index).FindPropertyRelative("m_Name").stringValue;
                        if (string.IsNullOrEmpty(name) || !names.Add(name))
                        {
                            throw new InvalidOperationException("Format values require unique, non-empty names: " + binding.name);
                        }
                    }
                }
                foreach (Language language in dictionaries.Keys)
                {
                    if (!string.IsNullOrEmpty(binding.Key) && (binding is LocalizeImage
                        ? !assets[language].TryGetValue(binding.Key, out LocalizationAssetMapping image) || image.AssetType != LocalizationAssetType.Sprite
                        : !dictionaries[language].ContainsKey(binding.Key)))
                    {
                        throw new InvalidOperationException("Missing localization binding: " + language + " / " + binding.name + " / " + binding.Key);
                    }
                    if (fontKey != null && !string.IsNullOrEmpty(fontKey.stringValue)
                        && assets[language].TryGetValue(fontKey.stringValue, out LocalizationAssetMapping font)
                        && !(AssetDatabase.LoadMainAssetAtPath(Resolve(locations, font.AssetName)) is TMP_FontAsset))
                    {
                        throw new InvalidOperationException("Missing localized font: " + language + " / " + fontKey.stringValue);
                    }
                }
            }
        }
    }
}
