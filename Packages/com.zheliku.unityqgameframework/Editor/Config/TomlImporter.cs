using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>Lets ResourceComponent load .toml files as ordinary Unity TextAssets.</summary>
    [ScriptedImporter(1, "toml")]
    public sealed class TomlImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            var asset = new TextAsset(File.ReadAllText(context.assetPath, new UTF8Encoding(false, true)))
            { name = Path.GetFileNameWithoutExtension(context.assetPath) };
            context.AddObjectToAsset("text", asset);
            context.SetMainObject(asset);
        }
    }
}
