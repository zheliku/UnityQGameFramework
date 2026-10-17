using System;
using System.Collections.Generic;
using System.Globalization;
using Tomlyn;
using Tomlyn.Model;

namespace UnityQGameFramework.Core.Config
{
    /// <summary>Maps TOML scalar settings to the existing Config key/value API.</summary>
    public static class TomlConfigParser
    {
        public static void ParseAndAdd(IConfigManager manager, string text)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.StartsWith("\uFEFF", StringComparison.Ordinal)) text = text.Substring(1);
            var entries = new Dictionary<string, Value>(StringComparer.Ordinal);
            Flatten(Toml.ToModel(text), string.Empty, entries);
            foreach (string key in entries.Keys)
                if (manager.HasConfig(key)) throw new InvalidOperationException("Config key already exists: " + key);

            var added = new List<string>();
            try
            {
                foreach (var entry in entries)
                {
                    Value value = entry.Value;
                    if (!manager.AddConfig(entry.Key, value.Boolean, value.Integer, value.Float, value.Text))
                        throw new InvalidOperationException("Cannot add config key: " + entry.Key);
                    added.Add(entry.Key);
                }
            }
            catch
            {
                foreach (string key in added) manager.RemoveConfig(key);
                throw;
            }
        }

        private static void Flatten(TomlTable table, string prefix, Dictionary<string, Value> entries)
        {
            foreach (var entry in table)
            {
                string key = string.IsNullOrEmpty(prefix) ? entry.Key : prefix + "." + entry.Key;
                if (entry.Value is TomlTable nested) { Flatten(nested, key, entries); continue; }
                if (string.IsNullOrEmpty(key)) throw new FormatException("Config keys must not be empty.");
                if (entries.ContainsKey(key)) throw new FormatException("TOML keys map to the same Config key: " + key);
                entries.Add(key, ConvertValue(key, entry.Value));
            }
        }

        private static Value ConvertValue(string key, object source)
        {
            switch (source)
            {
                case bool boolean:
                    return new Value(boolean, 0, 0f, boolean ? "true" : "false");
                case long integer:
                    if (integer < int.MinValue || integer > int.MaxValue) throw new FormatException("Config integer exceeds Int32 range: " + key);
                    return new Value(false, (int)integer, integer, integer.ToString(CultureInfo.InvariantCulture));
                case double number:
                    if (double.IsNaN(number) || double.IsInfinity(number) || Math.Abs(number) > float.MaxValue)
                        throw new FormatException("Config float must be finite and fit Single: " + key);
                    return new Value(false, 0, (float)number, number.ToString("R", CultureInfo.InvariantCulture));
                case string text:
                    bool.TryParse(text, out bool boolValue);
                    int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue);
                    float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue);
                    return new Value(boolValue, intValue, floatValue, text);
                default:
                    throw new FormatException("Config supports strings, booleans, Int32 and Single values; use Luban for arrays and structured records. Key: " + key);
            }
        }

        private readonly struct Value
        {
            public Value(bool boolean, int integer, float number, string text)
            { Boolean = boolean; Integer = integer; Float = number; Text = text; }

            public bool Boolean { get; }
            public int Integer { get; }
            public float Float { get; }
            public string Text { get; }
        }
    }
}
