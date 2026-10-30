using System;
using System.Collections.Generic;
using Tomlyn;
using Tomlyn.Model;

namespace UnityQGameFramework.Core
{
    /// <summary>
    /// 将 TOML 语言分区展开为本地化字典键值。
    /// </summary>
    public static class TomlLocalizationParser
    {
        /// <summary>
        /// 解析指定语言的字典，不修改当前语言状态。
        /// </summary>
        /// <param name="text">TOML 文本。</param>
        /// <param name="language">目标语言；精确区域覆盖父语言的同名键。</param>
        /// <returns>本次解析独占的字典，调用方可用于合并回退内容。</returns>
        public static Dictionary<string, string> Parse(string text, Language language)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }
            if (language == Language.Unspecified)
            {
                throw new ArgumentException("Language is unspecified.", nameof(language));
            }
            string code = language.Code;
            if (text.StartsWith("\uFEFF", StringComparison.Ordinal))
            {
                text = text.Substring(1);
            }
            TomlTable document = Toml.ToModel(text);
            var sections = new Dictionary<string, TomlTable>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, object> entry in document)
            {
                if (!(entry.Value is TomlTable table))
                {
                    throw new FormatException("Language section must be a table: " + entry.Key);
                }
                if (sections.ContainsKey(entry.Key))
                {
                    throw new FormatException("Duplicate language section: " + entry.Key);
                }
                sections.Add(entry.Key, table);
            }

            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            bool found = false;
            var candidates = new List<string>();
            for (Language candidate = language; candidate != Language.Unspecified; candidate = candidate.Parent)
            {
                candidates.Add(candidate.Code);
            }
            // 目标区域覆盖父语言；不同分区可以补齐缺失键，同一分区仍禁止重复映射。
            for (int index = candidates.Count - 1; index >= 0; index--)
            {
                if (!sections.TryGetValue(candidates[index], out TomlTable translations))
                {
                    continue;
                }
                found = true;
                var translated = new Dictionary<string, string>(StringComparer.Ordinal);
                Collect(translations, string.Empty, translated);
                foreach (KeyValuePair<string, string> entry in translated)
                {
                    entries[entry.Key] = entry.Value;
                }
            }
            if (!found)
            {
                throw new FormatException("Dictionary does not contain language: " + code);
            }
            return entries;
        }

        /// <summary>
        /// 解析 TOML 并登记本地化字典。
        /// </summary>
        /// <param name="manager">接收解析结果的本地化管理器。</param>
        /// <param name="text">TOML 文本。</param>
        public static void ParseAndAdd(ILocalizationManager manager, string text)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            Dictionary<string, string> entries = Parse(text, manager.Language);
            foreach (var entry in entries)
            {
                if (manager.HasRawString(entry.Key))
                {
                    throw new InvalidOperationException("Translation key already exists: " + entry.Key);
                }
            }

            var added = new List<string>();
            try
            {
                foreach (var entry in entries)
                {
                    if (!manager.AddRawString(entry.Key, entry.Value))
                    {
                        throw new InvalidOperationException("Cannot add translation: " + entry.Key);
                    }

                    added.Add(entry.Key);
                }
            }
            catch
            {
                foreach (string key in added)
                {
                    manager.RemoveRawString(key);
                }

                throw;
            }
        }

        /// <summary>
        /// 按点分路径收集本地化文本。
        /// </summary>
        /// <param name="table">当前语言的 TOML 表。</param>
        /// <param name="prefix">当前表的键前缀。</param>
        /// <param name="entries">用于保存本地化文本的字典。</param>
        private static void Collect(TomlTable table, string prefix, Dictionary<string, string> entries)
        {
            foreach (var entry in table)
            {
                if (string.IsNullOrEmpty(entry.Key))
                {
                    throw new FormatException("Translation keys must not be empty.");
                }

                string key = string.IsNullOrEmpty(prefix) ? entry.Key : prefix + "." + entry.Key;
                if (entry.Value is TomlTable nested)
                {
                    Collect(nested, key, entries);
                    continue;
                }

                if (!(entry.Value is string text))
                {
                    throw new FormatException("Translation must be a string: " + key);
                }

                if (entries.ContainsKey(key))
                {
                    throw new FormatException("Translation keys map to the same key: " + key);
                }

                entries.Add(key, text);
            }
        }
    }
}
