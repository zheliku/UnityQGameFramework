using System;
using System.IO;
using System.Linq;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

namespace UnityQGameFramework.Editor
{
    internal static class EditorToml
    {
        internal static TomlTable Read(string path) => Toml.ToModel(File.ReadAllText(path));

        internal static void Write(string path, TomlTable model)
        {
            PreserveEmptyArrays(model);
            string full = Path.GetFullPath(path);
            const string managedRoot = "Assets/UnityQGameFramework/Configs";
            bool managed = string.Equals(Path.GetDirectoryName(full), Path.GetFullPath(managedRoot), StringComparison.OrdinalIgnoreCase);
            if (managed) LubanWorkspace.EnsureFolder(managedRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            string temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, Toml.FromModel(model), new UTF8Encoding(false));
                if (File.Exists(full)) File.Replace(temporary, full, null);
                else File.Move(temporary, full);
                if (managed) QgfProjectAssets.TrackWorkspace(managedRoot);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        // An empty array of tables has no [[header]] to emit; serialize it as [].
        private static void PreserveEmptyArrays(TomlTable table)
        {
            foreach (var entry in table.ToArray())
            {
                if (entry.Value is TomlTable child) PreserveEmptyArrays(child);
                else if (entry.Value is TomlTableArray tables)
                {
                    if (tables.Count == 0) table[entry.Key] = new TomlArray();
                    else foreach (var item in tables) PreserveEmptyArrays(item);
                }
            }
        }

        internal static TomlTable Table(this TomlTable table, string key) =>
            table.TryGetValue(key, out object value) && value is TomlTable result ? result : throw Invalid(key, "table");

        internal static TomlTableArray Tables(this TomlTable table, string key)
        {
            if (!table.TryGetValue(key, out object value)) throw Invalid(key, "array of tables");
            if (value is TomlTableArray result) return result;
            if (value is TomlArray empty && empty.Count == 0) return new TomlTableArray();
            throw Invalid(key, "array of tables");
        }

        internal static string String(this TomlTable table, string key, string fallback = null) =>
            !table.TryGetValue(key, out object value) ? fallback : value is string text ? text : throw Invalid(key, "string");

        internal static int Integer(this TomlTable table, string key, int fallback = 0) =>
            !table.TryGetValue(key, out object value) ? fallback : value is long integer ? checked((int)integer) : throw Invalid(key, "integer");

        internal static bool Boolean(this TomlTable table, string key, bool fallback = false) =>
            !table.TryGetValue(key, out object value) ? fallback : value is bool boolean ? boolean : throw Invalid(key, "boolean");

        internal static string[] Strings(this TomlTable table, string key)
        {
            if (!table.TryGetValue(key, out object value)) return Array.Empty<string>();
            if (!(value is TomlArray array) || array.Any(item => !(item is string))) throw Invalid(key, "string array");
            return array.Cast<string>().ToArray();
        }

        internal static T EnumValue<T>(this TomlTable table, string key, T fallback = default) where T : struct, Enum
        {
            if (!table.TryGetValue(key, out object value)) return fallback;
            if (!(value is string text) || !Enum.TryParse(text, out T result)) throw Invalid(key, typeof(T).Name);
            if (Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            {
                long mask = Enum.GetValues(typeof(T)).Cast<T>().Aggregate(0L, (bits, item) => bits | Convert.ToInt64(item));
                if ((Convert.ToInt64(result) & ~mask) != 0) throw Invalid(key, typeof(T).Name);
            }
            else if (!Enum.IsDefined(typeof(T), result)) throw Invalid(key, typeof(T).Name);
            return result;
        }

        internal static TomlArray ArrayOf(string[] values)
        {
            var array = new TomlArray();
            foreach (string value in values) array.Add(value);
            return array;
        }

        private static InvalidDataException Invalid(string key, string type) => new InvalidDataException("TOML '" + key + "' must be " + type + ".");
    }
}
