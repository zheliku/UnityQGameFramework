using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;

public static class ExcelSchemaMigrationValidation
{
    public static async Task<object> Run()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode first.");
        EditorApplication.LockReloadAssemblies();
        try
        {
            foreach (string root in new[] { "Assets/UnityQGameFramework/Config", "Assets/Examples/02_StarForce/UnityQGameFramework/Config" })
            {
                var profile = AssetDatabase.LoadAssetAtPath<LubanProfile>(root + "/LubanProfile.asset");
                await LubanGenerator.GenerateAsync(profile);
                var catalog = AssetDatabase.LoadAssetAtPath<LubanDataTableCatalog>(root + "/Data/Catalog.asset");
                int expected = root.Contains("StarForce") ? 11 : 1;
                if (catalog.Entries.Count != expected) throw new Exception("Unexpected table count at " + root);
            }
            return new { passed = true, defaultTables = 1, starForceTables = 11 };
        }
        finally { EditorApplication.UnlockReloadAssemblies(); }
    }
}
