using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Editor.ResourceTools;

namespace UnityQGameFramework.Examples.PointPointPoint.Editor
{
    public static class BuildPointGame
    {
        public const string SceneGuid = "f99fe5fffa1724e4ab21a8936d44ab7e";

        [MenuItem("QGF/Examples/PointPointPoint/Build Windows Player", false, 410)]
        public static void BuildMenu()
        {
            string output = EditorUtility.SaveFilePanel("Build PointPointPoint", "", "PointPointPoint", "exe");
            if (string.IsNullOrEmpty(output)) return;
            try { Debug.Log("PointPointPoint 已构建：" + BuildPlayer(output)); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private static string BuildResources()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before building.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Select Windows64 before building PointPointPoint.");
            string scene = AssetDatabase.GUIDToAssetPath(SceneGuid);
            if (string.IsNullOrEmpty(scene)) throw new FileNotFoundException("Import the complete PointPointPoint sample.");
            string gameMain = Path.GetDirectoryName(Path.GetDirectoryName(scene)).Replace('\\', '/');
            string root = Path.GetFullPath("Library/QgfExamples/PointPointPoint/" + Application.unityVersion);
            Directory.CreateDirectory(root);
            string collectionPath = Path.Combine(root, "ResourceCollection.toml");
            // 只登记两份动态资产。界面的贴图和字体随预制体依赖一起构建。
            var collection = new ResourceCollection(collectionPath);
            if (!collection.AddResource("point/game", null, null, LoadType.LoadFromFile, true, new[] { "Base" })
                || !collection.AssignAsset(AssetDatabase.AssetPathToGUID(gameMain + "/Configs/Game.toml"), "point/game", null)
                || !collection.AddResource("point/view", null, null, LoadType.LoadFromFile, true, new[] { "Base" })
                || !collection.AssignAsset(AssetDatabase.AssetPathToGUID(gameMain + "/UI/UIForms/PointGameView.prefab"), "point/view", null)
                || !collection.Save()) throw new IOException("Cannot collect PointPointPoint resources.");
            // 显式传入两份配置，不使用宿主或 StarForce 的全局资源工具配置。
            var builder = new ResourceBuilderController(Path.Combine(root, "ResourceBuilder.toml"), collectionPath)
            {
                InternalResourceVersion = 1,
                Platforms = Platform.Windows64,
                AssetBundleCompression = AssetBundleCompressionType.LZ4,
                CompressionHelperTypeName = "UnityQGameFramework.Runtime.DefaultCompressionHelper",
                OutputDirectory = Path.Combine(root, "Output"),
                OutputPackageSelected = true
            };
            Directory.CreateDirectory(builder.OutputDirectory);
            // 构建器会重建自己的输出；先核对所有目标仍在本例的 Library 目录内。
            string prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (string path in new[] { builder.WorkingPath, builder.OutputPackagePath, builder.BuildReportPath })
                if (!Path.GetFullPath(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("PointPointPoint build output escaped its root.");
            if (!builder.RefreshCompressionHelper() || !builder.BuildResources())
                throw new IOException("PointPointPoint resource build failed. See " + builder.BuildReportPath);
            return Path.GetFullPath(builder.OutputPackagePath + "Windows64");
        }

        public static string BuildPlayer(string output)
        {
            if (!string.Equals(Path.GetExtension(output), ".exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Choose a Windows .exe output path.", nameof(output));
            output = Path.GetFullPath(output);
            // Unity 首次构建会补写默认项，结束时恢复宿主原来的设置。
            using (var settings = new ProjectSettingsSnapshot()) return BuildPlayerAndDeploy(output);
        }

        private static string BuildPlayerAndDeploy(string output)
        {
            string resources = BuildResources();
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            // 直接传场景列表，不改 Build Settings、图形管线或 Player Settings。
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { AssetDatabase.GUIDToAssetPath(SceneGuid) },
                target = BuildTarget.StandaloneWindows64,
                locationPathName = output,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new IOException("PointPointPoint Player build failed.");
            // 构建完成后部署到 Player 输出，项目的 Assets/StreamingAssets 不参与。
            string data = Path.Combine(Path.GetDirectoryName(output), Path.GetFileNameWithoutExtension(output) + "_Data");
            string destination = Path.GetFullPath(Path.Combine(data, "StreamingAssets", PointGameResources.Folder));
            string expected = Path.GetFullPath(data).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!destination.StartsWith(expected, StringComparison.OrdinalIgnoreCase)) throw new IOException("Invalid Player resource destination.");
            if (Directory.Exists(destination)) Directory.Delete(destination, true);
            foreach (string source in Directory.GetFiles(resources, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(destination, Path.GetRelativePath(resources, source));
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(source, target);
            }
            return output;
        }

        private sealed class ProjectSettingsSnapshot : IDisposable
        {
            private readonly (UnityEngine.Object Asset, string Path, string Json, byte[] Bytes)[] entries;

            public ProjectSettingsSnapshot()
            {
                entries = new[] { "PlayerSettings", "GraphicsSettings", "QualitySettings" }.Select(name =>
                {
                    var asset = Unsupported.GetSerializedAssetInterfaceSingleton(name);
                    if (asset == null) throw new InvalidOperationException("Cannot capture " + name + ".");
                    string path = "ProjectSettings/" + (name == "PlayerSettings" ? "ProjectSettings" : name) + ".asset";
                    return (asset, path, EditorJsonUtility.ToJson(asset), File.ReadAllBytes(path));
                }).ToArray();
            }

            public void Dispose()
            {
                foreach (var entry in entries)
                {
                    // 两处都恢复，避免退出编辑器时又把构建后的内存状态写回项目。
                    if (EditorJsonUtility.ToJson(entry.Asset) != entry.Json)
                    {
                        EditorJsonUtility.FromJsonOverwrite(entry.Json, entry.Asset);
                        EditorUtility.ClearDirty(entry.Asset);
                    }
                    if (!File.ReadAllBytes(entry.Path).SequenceEqual(entry.Bytes)) File.WriteAllBytes(entry.Path, entry.Bytes);
                }
            }
        }
    }
}
