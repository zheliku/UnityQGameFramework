using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Editor;

public static class GenerateExampleTables
{
    public static async Task<object> Run()
    {
        const string root = "Assets/Examples/01_Modules/11_DataTable/Common/Config";
        EditorApplication.LockReloadAssemblies();
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var profile = AssetDatabase.LoadAssetAtPath<LubanProfile>(root + "/Editor/LubanProfile.asset");
            if (profile == null) throw new IOException("Shared DataTable profile is missing.");
            await LubanGenerator.GenerateAsync(profile);
            if (!File.Exists(root + "/Data/Catalog.asset")) throw new IOException("Catalog missing: " + root);
            AssetDatabase.SaveAssets();
            return new { generated = 1 };
        }
        finally { EditorApplication.UnlockReloadAssemblies(); }
    }
}
