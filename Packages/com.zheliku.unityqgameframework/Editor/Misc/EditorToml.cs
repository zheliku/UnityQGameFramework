using System;
using System.IO;
using System.Linq;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 读写编辑器使用的 TOML 配置。
    /// </summary>
    internal static class EditorToml
    {
        /// <summary>
        /// 读取 TOML 配置文件。
        /// </summary>
        /// <param name="path">TOML 文件路径。</param>
        /// <returns>解析后的 TOML 表。</returns>
        internal static TomlTable Read(string path)
        {
            return Toml.ToModel(File.ReadAllText(path));
        }

        /// <summary>
        /// 将 TOML 配置写入文件并保持空数组。
        /// </summary>
        /// <param name="path">TOML 文件路径。</param>
        /// <param name="model">要写入的 TOML 表。</param>
        internal static void Write(string path, TomlTable model)
        {
            PreserveEmptyArrays(model);
            string full = Path.GetFullPath(path);
            const string managedRoot = "Assets/UnityQGameFramework/Configs";
            bool managed = string.Equals(Path.GetDirectoryName(full), Path.GetFullPath(managedRoot), StringComparison.OrdinalIgnoreCase);
            if (managed)
            {
                LubanWorkspace.EnsureFolder(managedRoot);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(full));
            string temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, Toml.FromModel(model), new UTF8Encoding(false));
                if (File.Exists(full))
                {
                    File.Replace(temporary, full, null);
                }
                else
                {
                    File.Move(temporary, full);
                }

                if (managed)
                {
                    QgfProjectAssets.TrackWorkspace(managedRoot);
                }
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        // An empty array of tables has no [[header]] to emit; serialize it as [].
        /// <summary>
        /// 将空的 TOML 表数组转换为可序列化的空数组。
        /// </summary>
        /// <param name="table">要处理的 TOML 表。</param>
        private static void PreserveEmptyArrays(TomlTable table)
        {
            foreach (var entry in table.ToArray())
            {
                if (entry.Value is TomlTable child)
                {
                    PreserveEmptyArrays(child);
                }
                else if (entry.Value is TomlTableArray tables)
                {
                    if (tables.Count == 0)
                    {
                        table[entry.Key] = new TomlArray();
                    }
                    else
                    {
                        foreach (var item in tables)
                        {
                            PreserveEmptyArrays(item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 读取指定的 TOML 子表。
        /// </summary>
        /// <param name="table">父 TOML 表。</param>
        /// <param name="key">子表键名。</param>
        /// <returns>TOML 子表。</returns>
        internal static TomlTable Table(this TomlTable table, string key)
        {
            return table.TryGetValue(key, out object value) && value is TomlTable result ? result : throw Invalid(key, "table");
        }

        /// <summary>
        /// 读取指定的 TOML 表数组。
        /// </summary>
        /// <param name="table">父 TOML 表。</param>
        /// <param name="key">表数组键名。</param>
        /// <returns>TOML 表数组。</returns>
        internal static TomlTableArray Tables(this TomlTable table, string key)
        {
            if (!table.TryGetValue(key, out object value))
            {
                throw Invalid(key, "array of tables");
            }

            if (value is TomlTableArray result)
            {
                return result;
            }

            if (value is TomlArray empty && empty.Count == 0)
            {
                return new TomlTableArray();
            }

            throw Invalid(key, "array of tables");
        }

        /// <summary>
        /// 读取 TOML 字符串，缺失时返回默认值。
        /// </summary>
        /// <param name="table">TOML 表。</param>
        /// <param name="key">键名。</param>
        /// <param name="fallback">键缺失时的默认值。</param>
        /// <returns>字符串值。</returns>
        internal static string String(this TomlTable table, string key, string fallback = null)
        {
            return !table.TryGetValue(key, out object value) ? fallback : value is string text ? text : throw Invalid(key, "string");
        }

        /// <summary>
        /// 读取 TOML 整数，缺失时返回默认值。
        /// </summary>
        /// <param name="table">TOML 表。</param>
        /// <param name="key">键名。</param>
        /// <param name="fallback">键缺失时的默认值。</param>
        /// <returns>整数值。</returns>
        internal static int Integer(this TomlTable table, string key, int fallback = 0)
        {
            return !table.TryGetValue(key, out object value) ? fallback : value is long integer ? checked((int)integer) : throw Invalid(key, "integer");
        }

        /// <summary>
        /// 读取 TOML 布尔值，缺失时返回默认值。
        /// </summary>
        /// <param name="table">TOML 表。</param>
        /// <param name="key">键名。</param>
        /// <param name="fallback">键缺失时的默认值。</param>
        /// <returns>布尔值。</returns>
        internal static bool Boolean(this TomlTable table, string key, bool fallback = false)
        {
            return !table.TryGetValue(key, out object value) ? fallback : value is bool boolean ? boolean : throw Invalid(key, "boolean");
        }

        /// <summary>
        /// 读取 TOML 字符串数组。
        /// </summary>
        /// <param name="table">TOML 表。</param>
        /// <param name="key">键名。</param>
        /// <returns>字符串数组；键缺失时返回空数组。</returns>
        internal static string[] Strings(this TomlTable table, string key)
        {
            if (!table.TryGetValue(key, out object value))
            {
                return Array.Empty<string>();
            }

            if (!(value is TomlArray array) || array.Any(item => !(item is string)))
            {
                throw Invalid(key, "string array");
            }

            return array.Cast<string>().ToArray();
        }

        /// <summary>
        /// 读取并校验 TOML 枚举值。
        /// </summary>
        /// <typeparam name="T">枚举类型。</typeparam>
        /// <param name="table">TOML 表。</param>
        /// <param name="key">键名。</param>
        /// <param name="fallback">键缺失时的默认枚举值。</param>
        /// <returns>枚举值。</returns>
        internal static T EnumValue<T>(this TomlTable table, string key, T fallback = default)
            where T : struct, Enum
        {
            if (!table.TryGetValue(key, out object value))
            {
                return fallback;
            }

            if (!(value is string text) || !Enum.TryParse(text, out T result))
            {
                throw Invalid(key, typeof(T).Name);
            }

            if (Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            {
                long mask = Enum.GetValues(typeof(T)).Cast<T>().Aggregate(0L, (bits, item) => bits | Convert.ToInt64(item));
                if ((Convert.ToInt64(result) & ~mask) != 0)
                {
                    throw Invalid(key, typeof(T).Name);
                }
            }
            else if (!Enum.IsDefined(typeof(T), result))
            {
                throw Invalid(key, typeof(T).Name);
            }

            return result;
        }

        /// <summary>
        /// 创建 TOML 字符串数组。
        /// </summary>
        /// <param name="values">要写入的字符串。</param>
        /// <returns>TOML 数组。</returns>
        internal static TomlArray ArrayOf(string[] values)
        {
            var array = new TomlArray();
            foreach (string value in values)
            {
                array.Add(value);
            }

            return array;
        }

        /// <summary>
        /// 创建配置类型错误异常。
        /// </summary>
        /// <param name="key">配置键名。</param>
        /// <param name="type">期望的 TOML 类型。</param>
        /// <returns>配置格式异常。</returns>
        private static InvalidDataException Invalid(string key, string type)
        {
            return new InvalidDataException("TOML '" + key + "' must be " + type + ".");
        }
    }
}
