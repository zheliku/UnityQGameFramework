using System;
using System.Collections.Generic;
using System.Globalization;
using Tomlyn;
using Tomlyn.Model;

namespace UnityQGameFramework.Core.Config
{
    /// <summary>
    /// 将 TOML 标量映射为配置模块的键值数据。
    /// </summary>
    public static class TomlConfigParser
    {
        /// <summary>
        /// 解析 TOML 并登记游戏配置。
        /// </summary>
        /// <param name="manager">接收解析结果的配置管理器。</param>
        /// <param name="text">TOML 文本。</param>
        public static void ParseAndAdd(IConfigManager manager, string text)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (text.StartsWith("\uFEFF", StringComparison.Ordinal))
            {
                text = text.Substring(1);
            }

            var entries = new Dictionary<string, Value>(StringComparer.Ordinal);
            Flatten(Toml.ToModel(text), string.Empty, entries);
            foreach (string key in entries.Keys)
            {
                if (manager.HasConfig(key))
                {
                    throw new InvalidOperationException("Config key already exists: " + key);
                }
            }

            var added = new List<string>();
            try
            {
                foreach (var entry in entries)
                {
                    Value value = entry.Value;
                    if (!manager.AddConfig(entry.Key, value.Boolean, value.Integer, value.Float, value.Text))
                    {
                        throw new InvalidOperationException("Cannot add config key: " + entry.Key);
                    }

                    added.Add(entry.Key);
                }
            }
            catch
            {
                foreach (string key in added)
                {
                    manager.RemoveConfig(key);
                }

                throw;
            }
        }

        /// <summary>
        /// 按点分路径展开 TOML 表的配置项。
        /// </summary>
        /// <param name="table">当前 TOML 表。</param>
        /// <param name="prefix">当前表的键前缀。</param>
        /// <param name="entries">用于保存配置项的字典。</param>
        private static void Flatten(TomlTable table, string prefix, Dictionary<string, Value> entries)
        {
            foreach (var entry in table)
            {
                string key = string.IsNullOrEmpty(prefix) ? entry.Key : prefix + "." + entry.Key;
                if (entry.Value is TomlTable nested)
                {
                    Flatten(nested, key, entries);
                    continue;
                }

                if (string.IsNullOrEmpty(key))
                {
                    throw new FormatException("Config keys must not be empty.");
                }

                if (entries.ContainsKey(key))
                {
                    throw new FormatException("TOML keys map to the same Config key: " + key);
                }

                entries.Add(key, ConvertValue(key, entry.Value));
            }
        }

        /// <summary>
        /// 将 TOML 标量转换为配置值。
        /// </summary>
        /// <param name="key">配置项完整键名。</param>
        /// <param name="source">TOML 标量值。</param>
        /// <returns>转换后的配置值。</returns>
        private static Value ConvertValue(string key, object source)
        {
            switch (source)
            {
                case bool boolean:
                    return new Value(boolean, 0, 0f, boolean ? "true" : "false");
                case long integer:
                    if (integer < int.MinValue || integer > int.MaxValue)
                    {
                        throw new FormatException("Config integer exceeds Int32 range: " + key);
                    }

                    return new Value(false, (int)integer, integer, integer.ToString(CultureInfo.InvariantCulture));
                case double number:
                    if (double.IsNaN(number) || double.IsInfinity(number) || Math.Abs(number) > float.MaxValue)
                    {
                        throw new FormatException("Config float must be finite and fit Single: " + key);
                    }

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

        /// <summary>
        /// 解析后的配置标量。
        /// </summary>
        private readonly struct Value
        {
            /// <summary>
            /// 初始化 TOML 配置标量。
            /// </summary>
            /// <param name="boolean">布尔值。</param>
            /// <param name="integer">整数值。</param>
            /// <param name="number">浮点数值。</param>
            /// <param name="text">字符串值。</param>
            public Value(bool boolean, int integer, float number, string text)
            {
                Boolean = boolean;
                Integer = integer;
                Float = number;
                Text = text;
            }

            /// <summary>
            /// 获取布尔值。
            /// </summary>
            public bool Boolean { get; }

            /// <summary>
            /// 获取整数值。
            /// </summary>
            public int Integer { get; }

            /// <summary>
            /// 获取浮点数值。
            /// </summary>
            public float Float { get; }

            /// <summary>
            /// 获取字符串值。
            /// </summary>
            public string Text { get; }
        }
    }
}
