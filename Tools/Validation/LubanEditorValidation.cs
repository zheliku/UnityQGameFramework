using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEditor;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;

/// <summary>Run with Unity CLI run_script outside Play Mode. Restores all temporary moves.</summary>
public static class LubanEditorValidation
{
    private const string Root = "Assets/Examples/02_StarForce/UnityQGameFramework/Config";
    public static async Task<object> Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode first.");
        var profile = AssetDatabase.LoadAssetAtPath<LubanProfile>(Root + "/LubanProfile.asset");
        string before = Snapshot(Root + "/Generated") + Snapshot(Root + "/Data");
        string catalogGuid = AssetDatabase.AssetPathToGUID(Root + "/Data/Catalog.asset");
        string definition = Root + "/luban.conf";
        string schema = File.ReadAllText(definition);
        string userFile = Root + "/Generated/UserOwned.cs";
        string staleFile = Root + "/Generated/QgfObsolete.cs";
        if (File.Exists(userFile) || File.Exists(staleFile)) throw new Exception("Validation sentinel path is occupied.");
        EditorApplication.LockReloadAssemblies();
        try
        {
            await ValidateReassignedFolders(profile);
            await LubanGenerator.GenerateAsync(profile, true);
            File.WriteAllText(definition, "{invalid");
            bool rejected = false;
            try { await LubanGenerator.GenerateAsync(profile); }
            catch (Exception) { rejected = true; }
            finally { File.WriteAllText(definition, schema); }
            Require(rejected, "Invalid schema must fail.");
            Require(before == Snapshot(Root + "/Generated") + Snapshot(Root + "/Data"), "Failed generation changed existing outputs.");
            File.WriteAllText(userFile, "// User-owned sentinel.\n");
            File.WriteAllText(staleFile, "// Generator-owned obsolete sentinel.\n");
            AssetDatabase.ImportAsset(userFile);
            AssetDatabase.ImportAsset(staleFile);
            profile.GeneratedCodeFiles.Add("QgfObsolete.cs");
            foreach (string folder in new[] { "Source", "Generated", "Data" })
                Require(AssetDatabase.MoveAsset(Root + "/" + folder, Root + "/" + folder + " Moved") == string.Empty, "Folder move failed: " + folder);
            File.WriteAllText(definition, schema.Replace("Source/", "Source Moved/").Replace("\"dataDir\": \"Source\"", "\"dataDir\": \"Source Moved\""));
            Require(LubanGenerator.SourcePath(profile).EndsWith("Source Moved"), "Luban did not read the updated source directory.");
            await LubanGenerator.GenerateAsync(profile);
            Require(File.Exists(Root + "/Generated Moved/UserOwned.cs"), "Unowned file was deleted.");
            Require(!File.Exists(Root + "/Generated Moved/QgfObsolete.cs") && !File.Exists(Root + "/Generated Moved/QgfObsolete.cs.meta"), "Obsolete output was not removed.");
            Require(AssetDatabase.AssetPathToGUID(Root + "/Data Moved/Catalog.asset") == catalogGuid, "Catalog GUID changed.");
            var catalog = AssetDatabase.LoadAssetAtPath<LubanDataTableCatalog>(Root + "/Data Moved/Catalog.asset");
            Require(catalog.GetAssets().Count == 11, "Catalog references did not follow the move.");
            IDataTableLoader loader = new StarForce.Tables();
            var tables = loader.CreateTables(name => new UnityQGameFramework.Luban.ByteBuf(catalog.GetAssets()[name].bytes));
            var entities = (StarForce.TbEntity)tables.Single(table => table is StarForce.TbEntity);
            Require(loader.SchemaHash == catalog.SchemaHash && entities.GetDataRow(10000).AssetName == "PlayerShip", "Moved catalog could not be parsed.");
        }
        finally
        {
            foreach (string folder in new[] { "Source", "Generated", "Data" })
                if (AssetDatabase.IsValidFolder(Root + "/" + folder + " Moved"))
                    Require(AssetDatabase.MoveAsset(Root + "/" + folder + " Moved", Root + "/" + folder) == string.Empty, "Could not restore " + folder);
            File.WriteAllText(definition, schema);
            AssetDatabase.DeleteAsset(userFile);
            AssetDatabase.DeleteAsset(staleFile);
            profile.GeneratedCodeFiles.Remove("QgfObsolete.cs");
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorApplication.UnlockReloadAssemblies();
        }
        Require(before == Snapshot(Root + "/Generated") + Snapshot(Root + "/Data"), "Regeneration or moves changed output bytes/GUIDs.");
        var result = new { passed = true, validation = true, invalidSchemaPreservesOutputs = true, sourceCodeDataMoves = true,
            preservesMetas = true, preservesUserFiles = true, removesOwnedObsoleteFiles = true, rejectsUnownedDestinations = true };
        Directory.CreateDirectory("Artifacts~/Luban");
        File.WriteAllText("Artifacts~/Luban/editor-validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    private static async Task ValidateReassignedFolders(LubanProfile profile)
    {
        const string code = Root + "/Reassigned Code";
        const string data = Root + "/Reassigned Data";
        Require(!Directory.Exists(code) && !Directory.Exists(data), "Retarget validation paths are occupied.");
        var originalCode = profile.CodeFolder;
        var originalData = profile.DataFolder;
        string codeGuid = profile.GeneratedCodeFolderGuid;
        string dataGuid = profile.GeneratedDataFolderGuid;
        try
        {
            LubanWorkspace.EnsureFolder(code);
            LubanWorkspace.EnsureFolder(data);
            const string userContent = "// User-owned destination file.\n";
            File.WriteAllText(code + "/Tables.cs", userContent);
            AssetDatabase.ImportAsset(code + "/Tables.cs");
            profile.CodeFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(code);
            bool rejected = false;
            try { await LubanGenerator.GenerateAsync(profile); }
            catch (IOException) { rejected = true; }
            Require(rejected && File.ReadAllText(code + "/Tables.cs") == userContent, "Reassigning CodeFolder overwrote user data.");
            profile.CodeFolder = originalCode;
            Require(AssetDatabase.CopyAsset(Root + "/Data/Catalog.asset", data + "/Catalog.asset"), "Cannot create catalog sentinel.");
            string before = Snapshot(data);
            profile.DataFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(data);
            rejected = false;
            try { await LubanGenerator.GenerateAsync(profile); }
            catch (IOException) { rejected = true; }
            Require(rejected && Snapshot(data) == before, "Reassigning DataFolder overwrote an unowned catalog.");
            Require(profile.GeneratedCodeFolderGuid == codeGuid && profile.GeneratedDataFolderGuid == dataGuid, "Rejected generation changed ownership.");
        }
        finally
        {
            profile.CodeFolder = originalCode;
            profile.DataFolder = originalData;
            AssetDatabase.DeleteAsset(code);
            AssetDatabase.DeleteAsset(data);
        }
    }
    private static string Snapshot(string directory)
    {
        using (var sha = SHA256.Create())
            return string.Join("|", Directory.GetFiles(directory, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => p.Substring(directory.Length).Replace('\\', '/') + ":" + BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p)))));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
