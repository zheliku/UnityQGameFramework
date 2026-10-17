using System;
using System.Collections.Generic;
using Tomlyn;
using Tomlyn.Model;

namespace UnityQGameFramework.Core.Localization
{
    /// <summary>Flatten nested language sections into the module's dotted translation keys.</summary>
    public static class TomlLocalizationParser
    {
        public static void ParseAndAdd(ILocalizationManager manager, string text)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.StartsWith("\uFEFF", StringComparison.Ordinal)) text = text.Substring(1);
            var document = Toml.ToModel(text);
            string language = manager.Language.ToString();
            if (!document.TryGetValue(language, out object value) || !(value is TomlTable translations))
                throw new FormatException("Dictionary does not contain language: " + language);
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            Collect(translations, string.Empty, entries);
            foreach (var entry in entries)
            {
                if (manager.HasRawString(entry.Key)) throw new InvalidOperationException("Translation key already exists: " + entry.Key);
            }
            var added = new List<string>();
            try
            {
                foreach (var entry in entries)
                {
                    if (!manager.AddRawString(entry.Key, entry.Value)) throw new InvalidOperationException("Cannot add translation: " + entry.Key);
                    added.Add(entry.Key);
                }
            }
            catch
            {
                foreach (string key in added) manager.RemoveRawString(key);
                throw;
            }
        }

        private static void Collect(TomlTable table, string prefix, Dictionary<string, string> entries)
        {
            foreach (var entry in table)
            {
                if (string.IsNullOrEmpty(entry.Key)) throw new FormatException("Translation keys must not be empty.");
                string key = string.IsNullOrEmpty(prefix) ? entry.Key : prefix + "." + entry.Key;
                if (entry.Value is TomlTable nested) { Collect(nested, key, entries); continue; }
                if (!(entry.Value is string text)) throw new FormatException("Translation must be a string: " + key);
                if (entries.ContainsKey(key)) throw new FormatException("Translation keys map to the same key: " + key);
                entries.Add(key, text);
            }
        }
    }
}
