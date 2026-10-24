using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Editor.ResourceTools;

namespace UnityQGameFramework.Examples.Modules.Editor
{
    // 构建放在编辑器侧。示例场景读取 Library 产物，不改 StreamingAssets 或项目设置。
    public static class BuildResourceExamples
    {
        [MenuItem("QGF/Examples/Build Resource Fixtures", false, 400)]
        public static void BuildMenu()
        {
            try { Build(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        public static object Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before building resource examples.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Resource fixtures currently target Windows64. Select this target before building.");
            string root = ResourceExamplePaths.BuildRoot;
            Directory.CreateDirectory(root);
            // 后续构建会覆盖资源；失败时不能继续把旧清单当成可用产物。
            string manifestPath = Path.Combine(root, "fixture.json");
            if (File.Exists(manifestPath)) File.Delete(manifestPath);
            string prefab = AssetDatabase.GUIDToAssetPath(ResourceExamplePaths.CubeGuid);
            if (string.IsNullOrEmpty(prefab)) throw new IOException("Resource example Cube is missing.");
            string art = Path.GetDirectoryName(prefab).Replace('\\', '/');
            string fileSystemAsset = art + "/Archive.bytes", diskAsset = art + "/Message.bytes";
            if (!File.Exists(fileSystemAsset) || !File.Exists(diskAsset)) throw new IOException("Resource example binary source files are missing.");
            string collectionPath = Path.Combine(root, "ResourceCollection.toml");
            var collection = new ResourceCollection(collectionPath);
            Add(collection, "example/cube", prefab, LoadType.LoadFromFile, null, "Base", otherGroup: "Shared");
            Add(collection, "example/archive", fileSystemAsset, LoadType.LoadFromBinary, "example-files", "Data", otherGroup: "Shared");
            Add(collection, "example/message", diskAsset, LoadType.LoadFromBinary, null, "Data");
            if (!collection.Save()) throw new IOException("Cannot save example resource collection.");
            var settings = new[] { "ProjectSettings", "GraphicsSettings", "QualitySettings", "TagManager", "EditorBuildSettings" }
                .ToDictionary(name => name, name => Hash("ProjectSettings/" + name + ".asset"));
            var builder = new ResourceBuilderController(Path.Combine(root, "ResourceBuilder.toml"), collectionPath)
            {
                InternalResourceVersion = 1,
                Platforms = Platform.Windows64,
                AssetBundleCompression = AssetBundleCompressionType.LZ4,
                CompressionHelperTypeName = "UnityQGameFramework.Runtime.DefaultCompressionHelper",
                AdditionalCompressionSelected = true,
                OutputDirectory = Path.Combine(root, "Output"),
                OutputPackageSelected = true,
                OutputFullSelected = true,
                OutputPackedSelected = true
            };
            Directory.CreateDirectory(builder.OutputDirectory);
            // 构建器会覆盖自己的输出。先核对绝对路径，防止清理其他目录。
            string prefix = Path.GetFullPath(builder.OutputDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (string output in new[]
                     {
                         builder.WorkingPath, builder.OutputPackagePath, builder.OutputFullPath,
                         builder.OutputPackedPath, builder.BuildReportPath
                     })
            {
                if (!Path.GetFullPath(output).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Example build output escaped its root.");
            }
            if (!builder.RefreshCompressionHelper() || !builder.BuildResources())
                throw new IOException("Example resource build failed. See " + builder.BuildReportPath);
            var packBuilder = new ResourcePackBuilderController(Path.Combine(root, "ResourceBuilder.toml"))
            {
                WorkingDirectory = builder.OutputDirectory,
                Platform = Platform.Windows64,
                CompressionHelperTypeName = "UnityQGameFramework.Runtime.DefaultCompressionHelper",
                BackupDiff = false
            };
            // null 表示从空版本打包，因此包内包含当前版本的全部资源。
            string versionName = new DirectoryInfo(builder.OutputFullPath.TrimEnd('/', '\\')).Name;
            if (!Path.GetFullPath(packBuilder.OutputPath).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Example resource pack output escaped its root.");
            if (Directory.Exists(packBuilder.OutputPath))
                foreach (string oldPack in Directory.GetFiles(packBuilder.OutputPath, "FrameworkResourcePack-*.dat")) File.Delete(oldPack);
            if (!packBuilder.RefreshCompressionHelper() || !packBuilder.BuildResourcePack(null, versionName))
                throw new IOException("Example resource pack build failed.");
            string pack = Directory.GetFiles(packBuilder.OutputPath, "FrameworkResourcePack-*.dat").Single();
            string remote = builder.OutputFullPath + "Windows64";
            string version = Directory.GetFiles(remote, "GameFrameworkVersion.*.dat").Single();
            byte[] compressed = File.ReadAllBytes(version), bytes = Utility.Compression.Decompress(compressed);
            var fixture = new ResourceFixtureManifest
            {
                UnityVersion = Application.unityVersion, PrefabAssetName = prefab,
                FileSystemAssetName = fileSystemAsset, DiskAssetName = diskAsset, ResourceVersion = builder.InternalResourceVersion,
                PackageFolder = Path.GetRelativePath(root, builder.OutputPackagePath + "Windows64"),
                RemoteFolder = Path.GetRelativePath(root, remote), PackedFolder = Path.GetRelativePath(root, builder.OutputPackedPath + "Windows64"),
                ResourcePackFile = Path.GetRelativePath(root, pack),
                VersionLength = bytes.Length, VersionHash = Utility.Verifier.GetCrc32(bytes),
                VersionCompressedLength = compressed.Length, VersionCompressedHash = Utility.Verifier.GetCrc32(compressed)
            };
            string variantCollectionPath = Path.Combine(root, "VariantCollection.toml");
            var variants = new ResourceCollection(variantCollectionPath);
            Add(variants, "example/cube", prefab, LoadType.LoadFromFile, null, "Base");
            string normal = art + "/Variants/Normal.bytes", hard = art + "/Variants/Hard.bytes";
            Add(variants, "example/mode", normal, LoadType.LoadFromBinaryAndQuickDecrypt, "variant-files", "Data", "normal");
            Add(variants, "example/mode", hard, LoadType.LoadFromBinaryAndDecrypt, "variant-files", "Data", "hard");
            if (!variants.Save()) throw new IOException("Cannot save example variant collection.");
            var variantBuilder = new ResourceBuilderController(Path.Combine(root, "VariantBuilder.toml"), variantCollectionPath)
            {
                InternalResourceVersion = 2,
                Platforms = Platform.Windows64,
                AssetBundleCompression = AssetBundleCompressionType.LZ4,
                CompressionHelperTypeName = "UnityQGameFramework.Runtime.DefaultCompressionHelper",
                OutputDirectory = Path.Combine(builder.OutputDirectory, "Variants"),
                OutputPackageSelected = true
            };
            Directory.CreateDirectory(variantBuilder.OutputDirectory);
            foreach (string output in new[]
                     {
                         variantBuilder.WorkingPath, variantBuilder.OutputPackagePath,
                         variantBuilder.BuildReportPath
                     })
            {
                if (!Path.GetFullPath(output).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Example variant output escaped its root.");
            }
            if (!variantBuilder.RefreshCompressionHelper() || !variantBuilder.BuildResources()) throw new IOException("Example variant build failed.");
            fixture.VariantPackageFolder = Path.GetRelativePath(root, variantBuilder.OutputPackagePath + "Windows64");
            fixture.NormalVariantAsset = normal;
            fixture.HardVariantAsset = hard;
            if (settings.Any(pair => Hash("ProjectSettings/" + pair.Key + ".asset") != pair.Value))
                throw new IOException("Example build changed project settings.");
            File.WriteAllText(manifestPath, JsonUtility.ToJson(fixture, true));
            Debug.Log("Resource examples built: " + root);
            return new { built = true, unity = Application.unityVersion, settingsUnchanged = true, root, fixture };
        }

        private static void Add(
            ResourceCollection collection,
            string name,
            string asset,
            LoadType loadType,
            string fileSystem,
            string group,
            string variant = null,
            string otherGroup = null)
        {
            string[] groups = otherGroup == null ? new[] { group } : new[] { group, otherGroup };
            if (!collection.AddResource(name, variant, fileSystem, loadType, true, groups)
                || !collection.AssignAsset(AssetDatabase.AssetPathToGUID(asset), name, variant)) throw new IOException("Cannot collect " + asset);
        }

        private static string Hash(string path)
        {
            using (var hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(path)));
        }
    }
}
