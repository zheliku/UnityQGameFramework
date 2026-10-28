using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 将 TOML 文件导入为可由资源模块加载的 TextAsset。
    /// </summary>
    [ScriptedImporter(1, "toml")]
    public sealed class TomlImporter : ScriptedImporter
    {
        /// <summary>
        /// 将 TOML 文件导入为 TextAsset。
        /// </summary>
        /// <param name="context">当前资产的导入上下文。</param>
        public override void OnImportAsset(AssetImportContext context)
        {
            var asset = new TextAsset(File.ReadAllText(context.assetPath, new UTF8Encoding(false, true)))
            {
                name = Path.GetFileNameWithoutExtension(context.assetPath)
            };
            context.AddObjectToAsset("text", asset);
            context.SetMainObject(asset);
        }
    }
}
