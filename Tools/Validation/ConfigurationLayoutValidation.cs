using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;

/// <summary>
/// 检查根目录配置、首次生成与旧 Profile 升级。
/// </summary>
public static class ConfigurationLayoutValidation
{
    /// <summary>
    /// 在隔离工程检查配置生成和目录移动。
    /// </summary>
    /// <returns>配置布局与生成检查结果。</returns>
    public static async Task<object> Run()
    {
        const string root = "Assets/QgfConfigLayoutValidation";
        const string moved = "Assets/QgfConfigLayoutMoved";
        if (!Application.dataPath.Contains("Artifacts~") || Directory.Exists(root) || Directory.Exists(moved))
        {
            throw new InvalidOperationException("Use an isolated project with unoccupied validation paths.");
        }
        EditorApplication.LockReloadAssemblies();
        try
        {
            foreach (string path in AssetDatabase.FindAssets("t:LubanProfile", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var profile = AssetDatabase.LoadAssetAtPath<LubanProfile>(path);
                string configRoot = Path.GetDirectoryName(path).Replace('\\', '/');
                Require(LubanGenerator.ConfigurationPath(profile) == configRoot + "/luban.conf", "Configuration is not beside Profile.");
                Require(LubanGenerator.SourcePath(profile) == configRoot + "/Source", "Source path differs from dataDir.");
                string before = Snapshot(configRoot + "/Generated") + Snapshot(configRoot + "/Data");
                await LubanGenerator.GenerateAsync(profile);
                Require(before == Snapshot(configRoot + "/Generated") + Snapshot(configRoot + "/Data"), "Relocation changed generated code, data or asset identities.");
            }

            LubanProfile created = LubanWorkspace.Create(root);
            Require(File.Exists(root + "/luban.conf") && !File.Exists(root + "/Source/luban.conf"), "Fresh layout nested the configuration file.");
            Require(Directory.GetDirectories(root).Select(Path.GetFileName).OrderBy(name => name)
                .SequenceEqual(new[] { "Data", "Generated", "Source" }), "Fresh layout contains unnecessary directories.");
            Require(Directory.GetFiles(root + "/Source", "*.xlsx").Length == 4, "Starter workbooks are missing.");
            string conf = File.ReadAllText(root + "/luban.conf").Replace("UnityQGameFramework.Config", "QgfConfigLayoutFixture");
            File.WriteAllText(root + "/luban.conf", conf);
            await LubanGenerator.GenerateAsync(created);
            string beforeMove = Snapshot(root + "/Generated") + Snapshot(root + "/Data");
            Require(string.IsNullOrEmpty(AssetDatabase.MoveAsset(root, moved)), "Cannot move validation workspace.");
            Require(LubanGenerator.ConfigurationPath(created) == moved + "/luban.conf", "Configuration reference did not follow the move.");
            await LubanGenerator.GenerateAsync(created);
            Require(beforeMove == Snapshot(moved + "/Generated") + Snapshot(moved + "/Data"), "Workspace move changed generation output.");

            byte[] item = File.ReadAllBytes(moved + "/Source/Item.xlsx");
            Require(AssetDatabase.DeleteAsset(moved + "/Source/__beans__.xlsx"), "Cannot remove starter schema for repair test.");
            File.WriteAllText(moved + "/luban.conf", conf.Replace("QgfConfigLayoutFixture", "UnityQGameFramework.Config"));
            LubanWorkspace.RepairStarterSchemas(created);
            Require(File.Exists(moved + "/Source/__beans__.xlsx") && item.SequenceEqual(File.ReadAllBytes(moved + "/Source/Item.xlsx")), "Schema repair changed business data or did not restore the missing schema.");

            // 模拟旧版仅保存 SourceFolder 的资产，使用真实序列化字段完成升级。
            string oldConf = conf.Replace("Source/", string.Empty).Replace("\"dataDir\": \"Source\"", "\"dataDir\": \".\"");
            File.WriteAllText(moved + "/Source/luban.conf", oldConf);
            AssetDatabase.ImportAsset(moved + "/Source/luban.conf");
            var legacy = ScriptableObject.CreateInstance<LubanProfile>();
            AssetDatabase.CreateAsset(legacy, moved + "/LegacyProfile.asset");
            string legacyPath = moved + "/LegacyProfile.asset";
            string sourceGuid = AssetDatabase.AssetPathToGUID(moved + "/Source");
            string yaml = File.ReadAllText(legacyPath);
            yaml = System.Text.RegularExpressions.Regex.Replace(yaml, @"  m_LegacySourceFolder: .*", "  SourceFolder: {fileID: 102900000, guid: " + sourceGuid + ", type: 3}");
            File.WriteAllText(legacyPath, yaml);
            AssetDatabase.ImportAsset(legacyPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            legacy = AssetDatabase.LoadAssetAtPath<LubanProfile>(legacyPath);
            Require(LubanGenerator.ConfigurationPath(legacy) == moved + "/Source/luban.conf", "Legacy SourceFolder did not migrate to a configuration file.");
            Require(legacy.ConfigurationFile != null && File.ReadAllText(legacyPath).Contains("ConfigurationFile: {fileID: 102900000"), "Legacy upgrade was not saved.");
            return new
            {
                passed = true,
                existingProfiles = 3,
                identicalOutputs = true,
                rootConfiguration = true,
                freshWorkspace = true,
                workspaceMove = true,
                schemaRepair = true,
                legacyUpgrade = true
            };
        }
        finally
        {
            AssetDatabase.DeleteAsset(root);
            AssetDatabase.DeleteAsset(moved);
            EditorApplication.UnlockReloadAssemblies();
        }
    }

    private static string Snapshot(string root)
    {
        using (var hash = SHA256.Create())
        {
            return string.Join("|", Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(path => path)
                .Select(path => path.Substring(root.Length).Replace('\\', '/') + ":" + BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path)))));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
