using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Editor;

/// <summary>Fault injection against a disposable workspace; never edits game source tables.</summary>
public static class LubanFailureValidation
{
    private const string Root = "Assets/QgfProductionFailureValidation";

    public static async Task<object> Run()
    {
        if (EditorApplication.isPlaying || Directory.Exists(Root)) throw new Exception("Validation workspace is occupied or Play Mode is active.");
        EditorApplication.LockReloadAssemblies();
        try
        {
            var profile = LubanWorkspace.Create(Root);
            string conf = Root + "/Source/luban.conf";
            File.WriteAllText(conf, File.ReadAllText(conf).Replace("UnityQGameFramework.Config", "QgfProductionValidation"));
            string code = Root + "/Generated/DRItem.cs";
            string data = Root + "/Data/tbitem.bytes";
            string catalog = Root + "/Data/Catalog.asset";
            await LubanGenerator.GenerateAsync(profile);

            // Ensure C# is changed first, then fail while replacing the data file.
            File.AppendAllText(code, "\n// Before failed generation.\n");
            using (var append = new FileStream(data, FileMode.Append)) append.WriteByte(0);
            string before = Snapshot(Root);
            bool rejected = false;
            using (var locked = new FileStream(data, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try { await LubanGenerator.GenerateAsync(profile); }
                catch (IOException) { rejected = true; }
            }
            Require(rejected && !LubanGenerator.IsBusy && Snapshot(Root) == before, "An I/O failure did not restore outputs, GUIDs and Profile state.");
            await LubanGenerator.GenerateAsync(profile);

            // A wrong asset at the owned catalog path must fail before any output is changed.
            Require(AssetDatabase.DeleteAsset(catalog), "Cannot replace the catalog fixture.");
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<LubanProfile>(), catalog);
            before = Snapshot(Root);
            rejected = false;
            try { await LubanGenerator.GenerateAsync(profile); }
            catch (IOException) { rejected = true; }
            Require(rejected && Snapshot(Root) == before, "Invalid catalog changed outputs.");

            // Use an actual long-running child process to prove timeout termination.
            var runProcess = typeof(LubanGenerator).GetMethod("RunProcess", BindingFlags.NonPublic | BindingFlags.Static);
            var pending = (Task<string>)runProcess.Invoke(null, new object[]
            { "powershell.exe", new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", "Start-Sleep -Seconds 10" }, Directory.GetCurrentDirectory(), 0, 200 });
            bool timedOut = false;
            try { await pending; }
            catch (TimeoutException) { timedOut = true; }
            Require(timedOut, "Long-running generation process was not terminated.");

            var result = new Result { passed = true, ioRollback = true, catalogPreflight = true, timeout = true };
            Directory.CreateDirectory("Artifacts~/ProductionAudit/20261018");
            File.WriteAllText("Artifacts~/ProductionAudit/20261018/failure-validation.json", JsonUtility.ToJson(result, true));
            return result;
        }
        finally
        {
            AssetDatabase.DeleteAsset(Root);
            EditorApplication.UnlockReloadAssemblies();
        }
    }

    private static string Snapshot(string root)
    {
        using (var sha = SHA256.Create())
            return string.Join("|", Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => path + ":" + BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))));
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    [Serializable] private sealed class Result { public bool passed; public bool ioRollback; public bool catalogPreflight; public bool timeout; }
}
