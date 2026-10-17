using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>Project-owned generation settings. Folder references survive moves within Assets.</summary>
    [CreateAssetMenu(menuName = "QGF/Luban Profile", fileName = "LubanProfile")]
    public sealed class LubanProfile : ScriptableObject
    {
        public DefaultAsset SourceFolder;
        public DefaultAsset CodeFolder;
        public DefaultAsset DataFolder;
        public string Target = "client";
        [HideInInspector] public List<string> GeneratedCodeFiles = new List<string>();
        [HideInInspector] public List<string> GeneratedDataFiles = new List<string>();
        [HideInInspector] public string GeneratedCodeFolderGuid;
        [HideInInspector] public string GeneratedDataFolderGuid;
    }
}
