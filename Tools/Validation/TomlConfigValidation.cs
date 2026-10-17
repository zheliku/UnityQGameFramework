using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Core.Config;

public static class TomlConfigValidation
{
    public static object Run()
    {
        Type type = typeof(IConfigManager).Assembly.GetType("UnityQGameFramework.Core.Config.ConfigManager");
        var manager = (IConfigManager)Activator.CreateInstance(type, true);
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            TomlConfigParser.ParseAndAdd(manager, "\uFEFF[Game]\nId = \"Star Force # 配置\"\nEnabled = true\nRate = 1.25\nHex = 0x10\nCount = 1_000\nText = \"123\"\n[Scene]\nMenu = 1\nMain = 2\n");
            Require(manager.GetString("Game.Id") == "Star Force # 配置" && manager.GetBool("Game.Enabled"), "Strings/booleans did not parse.");
            Require(manager.GetInt("Game.Hex") == 16 && manager.GetInt("Game.Count") == 1000, "TOML numeric syntax failed.");
            Require(Math.Abs(manager.GetFloat("Game.Rate") - 1.25f) < 0.00001f && manager.GetInt("Game.Text") == 123, "Culture-invariant numeric conversion failed.");
            int count = manager.Count;
            foreach (string invalid in new[]
            {
                "New = 10\n[Scene]\nMenu = 100\n", // Existing key: no partial addition.
                "[Game\nId = 1",                    // Syntax error.
                "New = 10\nUnsupported = [1, 2]",     // Unsupported scalar shape.
                "New = 10\nOverflow = 2147483648",     // Int32 bound.
                "New = 10\nHuge = 1e99",              // Single bound.
                "New = 10\nInvalid = nan",            // Non-finite value.
                "\"Scene.Menu\" = 1\n[Scene]\nMenu = 2" // Flattened key collision.
            })
            {
                bool rejected = false;
                try { TomlConfigParser.ParseAndAdd(manager, invalid); }
                catch (Exception) { rejected = true; }
                Require(rejected && manager.Count == count && !manager.HasConfig("New"), "An invalid document changed existing settings.");
            }
            const string path = "Assets/Examples/StarForce/GameMain/Configs/DefaultConfig.toml";
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            Require(asset != null && AssetImporter.GetAtPath(path).GetType().Name == "TomlImporter", "TOML did not import as a TextAsset.");
            Require(AssetDatabase.AssetPathToGUID(path) == "44c8db52241385c45bbb14a1718f17bf", "Config GUID changed during migration.");
            manager.RemoveAllConfigs();
            TomlConfigParser.ParseAndAdd(manager, asset.text);
            Require(manager.Count == 3 && manager.GetString("Game.Id") == "Star Force" && manager.GetInt("Scene.Main") == 2, "StarForce settings changed.");
            var result = new { passed = true, settings = manager.Count, rejectedDocuments = 7, cultureIndependent = true, importer = true, guidPreserved = true };
            Directory.CreateDirectory("Artifacts~/Toml");
            File.WriteAllText("Artifacts~/Toml/editor-validation.json", JsonUtility.ToJson(new Summary { passed = true, settings = manager.Count }, true));
            return result;
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    [Serializable] private sealed class Summary { public bool passed; public int settings; }
}
