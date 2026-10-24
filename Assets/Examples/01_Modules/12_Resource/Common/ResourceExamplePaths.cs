#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    // 只负责示例数据的位置，资源初始化与加载由各示例直接调用。
    public sealed class ResourceExamplePaths : MonoBehaviour
    {
        [SerializeField] private bool emptyReadOnly;
        [SerializeField] private bool seedCache;
        [SerializeField] private bool variantPackage;
        public const string CubeGuid = "51d003874f9f553f84bccd96d341285c";
        public const string AssemblyGuid = "26a4a9304f1b571a97885965a89aaed1";
        public static string BuildRoot => Path.GetFullPath("Library/QgfExamples/Resources/" + Application.unityVersion);
        public static ResourceFixtureManifest Fixture { get; private set; }
        public static string Notice { get; private set; }

        public static string GetAssetStatusText(HasAssetResult result)
        {
            switch (result)
            {
                case HasAssetResult.NotReady: return "已登记，尚未就绪";
                case HasAssetResult.AssetOnDisk: return "AssetBundle（磁盘）";
                case HasAssetResult.AssetOnFileSystem: return "AssetBundle（文件系统）";
                case HasAssetResult.BinaryOnDisk: return "二进制（磁盘）";
                case HasAssetResult.BinaryOnFileSystem: return "二进制（文件系统）";
                default: return "未登记";
            }
        }

        public static string GetReadinessText(bool ready) => ready ? "已就绪" : "准备中";

        private void Awake()
        {
            string manifest = Path.Combine(BuildRoot, "fixture.json");
            Fixture = File.Exists(manifest) ? JsonUtility.FromJson<ResourceFixtureManifest>(File.ReadAllText(manifest)) : null;
            if (Fixture != null && (Fixture.UnityVersion != Application.unityVersion
                || Fixture.PrefabAssetName != AssetDatabase.GUIDToAssetPath(CubeGuid))) Fixture = null;
            if (Fixture != null && variantPackage && string.IsNullOrEmpty(Fixture.VariantPackageFolder)) Fixture = null;
            Notice = Fixture == null ? "先停止运行，执行 QGF > Examples > Build Resource Fixtures。" : string.Empty;
            string write = Path.Combine(BuildRoot, "Sessions", SceneManager.GetActiveScene().name, Guid.NewGuid().ToString("N"));
            string readOnly = emptyReadOnly || Fixture == null ? Path.Combine(write, "ReadOnly")
                : Path.Combine(BuildRoot, variantPackage ? Fixture.VariantPackageFolder : Fixture.PackageFolder);
            Directory.CreateDirectory(readOnly);
            Directory.CreateDirectory(write);
            if (seedCache && Fixture != null)
            {
                // Packed 产物含本地清单和未压缩资源，复制后模拟上次运行留下的缓存。
                string packed = Path.Combine(BuildRoot, Fixture.PackedFolder);
                foreach (string source in Directory.GetFiles(packed, "*", SearchOption.AllDirectories))
                {
                    string target = Path.Combine(write, Path.GetRelativePath(packed, source));
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(source, target);
                }
                // Packed 的清单属于只读区（GFR），缓存要用读写区格式（GFW）重新序列化。
                var reader = new ReadOnlyVersionListSerializer();
                reader.RegisterDeserializeCallback(2, BuiltinVersionListSerializer.LocalVersionListDeserializeCallback_V2);
                LocalVersionList versionList;
                using (var stream = File.OpenRead(Path.Combine(packed, "GameFrameworkList.dat"))) versionList = reader.Deserialize(stream);
                var writer = new ReadWriteVersionListSerializer();
                writer.RegisterSerializeCallback(2, BuiltinVersionListSerializer.LocalVersionListSerializeCallback_V2);
                using (var stream = File.Create(Path.Combine(write, "GameFrameworkList.dat")))
                    if (!writer.Serialize(stream, versionList)) throw new IOException("Cannot prepare example writable cache.");
            }
            // 在 ResourceComponent.Start 创建加载代理前设置，避免读写项目的正常资源区。
            var resources = FrameworkEntry.GetModule<IResourceManager>();
            resources.SetReadOnlyPath(readOnly);
            resources.SetReadWritePath(write);
        }
    }

    [Serializable]
    public sealed class ResourceFixtureManifest
    {
        public string UnityVersion;
        public string PrefabAssetName;
        public string FileSystemAssetName;
        public string DiskAssetName;
        public string PackageFolder;
        public string RemoteFolder;
        public string PackedFolder;
        public string ResourcePackFile;
        public string VariantPackageFolder, NormalVariantAsset, HardVariantAsset;
        public int ResourceVersion;
        public int VersionLength, VersionHash, VersionCompressedLength, VersionCompressedHash;
    }
}
#endif
