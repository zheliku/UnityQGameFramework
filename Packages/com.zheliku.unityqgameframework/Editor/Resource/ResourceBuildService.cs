using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Runtime;
using YooAsset;
using YooAsset.Editor;
using UnityBuildReport = UnityEditor.Build.Reporting.BuildReport;
using UnityBuildResult = UnityEditor.Build.Reporting.BuildResult;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 资源包构建工具。
    /// </summary>
    public static class ResourceBuildService
    {
        /// <summary>
        /// 获取是否正在构建资源。
        /// </summary>
        public static bool IsBuilding
        {
            get; private set;
        }

        /// <summary>
        /// 使用原生构建选项依次构建选中的资源包。
        /// </summary>
        /// <param name="collector">YooAsset 收集配置。</param>
        /// <param name="packageNames">要构建的资源包名称。</param>
        /// <returns>每个已构建资源包及其输出目录。</returns>
        public static Dictionary<string, string> BuildPackages(BundleCollectorSetting collector, IEnumerable<string> packageNames)
        {
            if (collector == null)
            {
                throw new ArgumentNullException(nameof(collector));
            }
            if (packageNames == null)
            {
                throw new ArgumentNullException(nameof(packageNames));
            }
            string[] names = packageNames.Distinct(StringComparer.Ordinal).ToArray();
            if (IsBuilding || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode and finish the current resource build first.");
            }
            // 先检查全部包名，避免输入错误时只构建了前半部分。
            foreach (string name in names)
            {
                collector.GetPackage(name);
            }
            var outputs = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                for (int index = 0; index < names.Length; index++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Build YooAsset Packages", names[index], (float)index / names.Length))
                    {
                        throw new OperationCanceledException("The remaining resource package builds were cancelled.");
                    }
                    // YooAsset 的构建管线按包执行；批量入口复用每个包在原生 Builder 中保存的选项。
                    outputs.Add(names[index], Build(collector, names[index], false, null, BundleBuilderHelper.GetStreamingAssetsRoot()));
                }
                return outputs;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// 获取运行配置引用的 YooAsset 收集配置。
        /// </summary>
        /// <param name="settings">运行时资源配置。</param>
        /// <returns>YooAsset 收集配置。</returns>
        public static BundleCollectorSetting GetCollector(ResourceSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            var serialized = new SerializedObject(settings);
            var collector = serialized.FindProperty("m_EditorCollector").objectReferenceValue as BundleCollectorSetting;
            if (collector == null)
            {
                throw new InvalidOperationException("Assign a YooAsset Collector in ResourceSettings: " + AssetDatabase.GetAssetPath(settings));
            }
            return collector;
        }

        /// <summary>
        /// 获取首包资源输出目录。
        /// </summary>
        /// <param name="collector">YooAsset 收集配置。</param>
        /// <param name="packageName">资源包名称。</param>
        /// <returns>当前收集配置下的首包目录。</returns>
        public static string GetBundledOutputDirectory(BundleCollectorSetting collector, string packageName)
        {
            if (collector == null)
            {
                throw new ArgumentNullException(nameof(collector));
            }
            collector.GetPackage(packageName);
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(collector));
            if (string.IsNullOrEmpty(guid))
            {
                throw new InvalidOperationException("Save the YooAsset collector before building.");
            }
            string staging = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), "QgfBuild~", guid, "B"));
            string output = Path.GetFullPath(Path.Combine(staging, packageName));
            if (!string.Equals(Path.GetDirectoryName(output), staging, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Resource package name must be a directory name.");
            }
            return output;
        }

        /// <summary>
        /// 构建指定资源包。
        /// </summary>
        /// <param name="collector">YooAsset 收集配置。</param>
        /// <param name="packageName">资源包名称。</param>
        /// <param name="simulate">是否构建编辑器模拟资源。</param>
        /// <returns>资源包输出目录。</returns>
        public static string Build(BundleCollectorSetting collector, string packageName, bool simulate)
        {
            return Build(collector, packageName, simulate, null);
        }

        /// <summary>
        /// 构建资源包并指定本次首包拷贝选项。
        /// </summary>
        /// <param name="collector">YooAsset 收集配置。</param>
        /// <param name="packageName">资源包名称。</param>
        /// <param name="simulate">是否构建编辑器模拟资源。</param>
        /// <param name="bundledCopyOption">首包拷贝选项，为空时使用 YooAsset 原生窗口的设置。</param>
        /// <returns>资源包输出目录。</returns>
        public static string Build(BundleCollectorSetting collector, string packageName, bool simulate, EBundledCopyOption? bundledCopyOption)
        {
            return Build(collector, packageName, simulate, bundledCopyOption, null);
        }

        /// <summary>
        /// 使用指定首包目录执行资源构建。
        /// </summary>
        /// <param name="collector">YooAsset 收集配置。</param>
        /// <param name="packageName">资源包名称。</param>
        /// <param name="simulate">是否构建编辑器模拟资源。</param>
        /// <param name="bundledCopyOption">首包拷贝选项，为空时使用原生设置。</param>
        /// <param name="bundledFileRoot">首包根目录，为空时使用隔离的工具输出目录。</param>
        /// <returns>资源包输出目录。</returns>
        private static string Build(BundleCollectorSetting collector, string packageName, bool simulate, EBundledCopyOption? bundledCopyOption, string bundledFileRoot)
        {
            if (collector == null)
            {
                throw new ArgumentNullException(nameof(collector));
            }
            if (IsBuilding)
            {
                throw new InvalidOperationException("A resource build is already in progress.");
            }
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(collector));
            if (string.IsNullOrEmpty(guid))
            {
                throw new InvalidOperationException("Save the YooAsset collector before building.");
            }
            string version = ResourceCollectorIntegration.GetBuildVersion(collector, packageName);
            if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(version))
            {
                throw new InvalidOperationException("Resource package name and version are required.");
            }
            if (version == "." || version == ".." || version.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidOperationException("Resource package version must be a directory name.");
            }

            BundleCollectorSetting setting = BundleCollectorSettingData.Setting;
            List<BundleCollectorPackage> packages = setting.Packages;
            bool uniqueBundleName = setting.UniqueBundleName;
            IsBuilding = true;
            try
            {
                // 仅替换构建期间的内存引用；不保存、清空或覆盖用户的收集器资产。
                var package = JsonUtility.FromJson<BundleCollectorPackage>(JsonUtility.ToJson(collector.GetPackage(packageName)));
                ResolveCollectorPaths(package);
                setting.Packages = new List<BundleCollectorPackage> { package };
                setting.UniqueBundleName = collector.UniqueBundleName;

                if (simulate)
                {
                    // 官方入口使用 Simulate 版本，不覆盖同一包已经构建的发布版本。
                    return BundleSimulateBuilder.SimulateBuild(new PackageBuildParameters(package.PackageName)
                    {
                        BuildPipelineName = nameof(EBuildPipeline.EditorSimulateBuildPipeline),
                        BuildBundleType = (int)GetSimulationType(GetBundleType(collector, packageName))
                    }).PackageRootDirectory;
                }

                BuildParameters parameters = CreateBuildParameters(collector, packageName, bundledCopyOption);
                if (!string.IsNullOrEmpty(bundledFileRoot))
                {
                    parameters.BundledFileRoot = bundledFileRoot;
                }
                IBuildPipeline pipeline = CreateBuildPipeline(parameters.BuildPipeline);
                if (parameters.BuildTarget == BuildTarget.NoTarget)
                {
                    throw new InvalidOperationException("Select a build target before building a YooAsset package.");
                }

                // 重建只覆盖本配置的当前版本产物，保留 SBP 增量缓存和其他版本。
                string packageRoot = Path.GetFullPath(parameters.GetPackageRootDirectory());
                string packageOutput = Path.GetFullPath(parameters.GetPackageOutputDirectory());
                if (!string.Equals(Path.GetDirectoryName(packageOutput), packageRoot, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Resource package output must remain inside its package directory.");
                }
                if (Directory.Exists(packageOutput))
                {
                    Directory.Delete(packageOutput, true);
                }

                YooAsset.Editor.BuildResult result = pipeline.Run(parameters, false);
                if (!result.Success)
                {
                    throw new InvalidOperationException("Resource build failed: " + result.ErrorInfo);
                }
                return result.OutputPackageDirectory;
            }
            finally
            {
                setting.Packages = packages;
                setting.UniqueBundleName = uniqueBundleName;
                IsBuilding = false;
            }
        }

        /// <summary>
        /// 构建 Player 和对应的离线资源包。
        /// </summary>
        /// <param name="collector">YooAsset 收集配置。</param>
        /// <param name="packageName">资源包名称。</param>
        /// <param name="scenePath">启动场景路径。</param>
        /// <param name="output">Player 输出文件路径。</param>
        /// <returns>Player 输出文件路径。</returns>
        public static string BuildPlayer(BundleCollectorSetting collector, string packageName, string scenePath, string output)
        {
            if (collector == null)
            {
                throw new ArgumentNullException(nameof(collector));
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                throw new InvalidOperationException("Player launch scene does not exist: " + scenePath);
            }
            if (string.IsNullOrWhiteSpace(output))
            {
                throw new ArgumentException("Player output path is required.", nameof(output));
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before building a Player.");
            }
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            if (target != BuildTarget.StandaloneWindows64 || !string.Equals(Path.GetExtension(output), ".exe", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("This offline Player example requires Windows64 and an .exe output path.");
            }

            output = Path.GetFullPath(output);
            string resources = GetBundledOutputDirectory(collector, packageName);
            using (var settings = new ProjectSettingsSnapshot())
            {
                Build(collector, packageName, false, EBundledCopyOption.ClearAndCopyAll);
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                UnityBuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { scenePath },
                    target = target,
                    locationPathName = output,
                    options = BuildOptions.Development
                });
                if (report.summary.result != UnityBuildResult.Succeeded)
                {
                    throw new IOException("Player build failed: " + report.summary.result);
                }
            }

            string data = Path.Combine(Path.GetDirectoryName(output), Path.GetFileNameWithoutExtension(output) + "_Data");
            string destination = Path.GetFullPath(Path.Combine(data, "StreamingAssets", "yoo", packageName));
            string expected = Path.GetFullPath(data).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!destination.StartsWith(expected, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("Invalid Player resource destination.");
            }
            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, true);
            }
            foreach (string source in Directory.GetFiles(resources, "*", SearchOption.AllDirectories))
            {
                string targetPath = Path.Combine(destination, Path.GetRelativePath(resources, source));
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Copy(source, targetPath);
            }
            return output;
        }

        /// <summary>
        /// 构建完成后恢复宿主项目的序列化设置。
        /// </summary>
        private sealed class ProjectSettingsSnapshot : IDisposable
        {
            private readonly (UnityEngine.Object Asset, string Path, string Json, byte[] Bytes)[] m_Entries;

            /// <summary>
            /// 保存构建前的宿主项目设置。
            /// </summary>
            public ProjectSettingsSnapshot()
            {
                m_Entries = new[] { "PlayerSettings", "GraphicsSettings", "QualitySettings" }.Select(name =>
                {
                    var asset = Unsupported.GetSerializedAssetInterfaceSingleton(name);
                    if (asset == null)
                    {
                        throw new InvalidOperationException("Cannot capture " + name + ".");
                    }
                    string path = "ProjectSettings/" + (name == "PlayerSettings" ? "ProjectSettings" : name) + ".asset";
                    return (asset, path, EditorJsonUtility.ToJson(asset), File.ReadAllBytes(path));
                }).ToArray();
            }

            /// <summary>
            /// 恢复构建前的宿主项目设置。
            /// </summary>
            public void Dispose()
            {
                foreach (var entry in m_Entries)
                {
                    if (EditorJsonUtility.ToJson(entry.Asset) != entry.Json)
                    {
                        EditorJsonUtility.FromJsonOverwrite(entry.Json, entry.Asset);
                        EditorUtility.ClearDirty(entry.Asset);
                    }
                    if (!File.ReadAllBytes(entry.Path).SequenceEqual(entry.Bytes))
                    {
                        File.WriteAllBytes(entry.Path, entry.Bytes);
                    }
                }
            }
        }

        /// <summary>
        /// 根据 YooAsset 原生设置创建构建参数。
        /// </summary>
        /// <param name="collector">资源收集配置。</param>
        /// <param name="packageName">资源包名称。</param>
        /// <param name="bundledCopyOption">可选的首包复制方式。</param>
        /// <returns>资源构建参数。</returns>
        internal static BuildParameters CreateBuildParameters(BundleCollectorSetting collector, string packageName, EBundledCopyOption? bundledCopyOption)
        {
            ResourceCollectorIntegration.EnsureBuildDefaults(collector);
            string pipelineName = BundleBuilderSetting.GetPackageBuildPipeline(packageName);
            EBundleType bundleType = GetBundleType(collector, packageName);
            BuildParameters parameters;
            if (bundleType == EBundleType.RawBundle)
            {
                parameters = new RawFileBuildParameters();
            }
            else if (bundleType == EBundleType.AssetBundle && pipelineName == nameof(EBuildPipeline.ScriptableBuildPipeline))
            {
                parameters = new ScriptableBuildParameters
                {
                    CompressOption = BundleBuilderSetting.GetPackageCompressOption(packageName, pipelineName),
                    BuiltinShadersBundleName = DefaultBundlePackRule.CreateShadersPackRuleResult()
                        .GetBundleName(packageName, collector.UniqueBundleName)
                };
            }
            else if (bundleType == EBundleType.AssetBundle && pipelineName == nameof(EBuildPipeline.LegacyBuildPipeline))
            {
                parameters = new LegacyBuildParameters
                {
                    CompressOption = BundleBuilderSetting.GetPackageCompressOption(packageName, pipelineName)
                };
            }
            else
            {
                throw new NotSupportedException("Select a compatible YooAsset build pipeline for " + bundleType + ": " + pipelineName);
            }

            // 与原生 Builder 读取同一份设置；QGF 只隔离首包目录，并允许离线示例显式要求完整首包。
            parameters.BuildOutputRoot = BundleBuilderHelper.GetDefaultBuildOutputRoot();
            parameters.BundledFileRoot = Path.GetDirectoryName(GetBundledOutputDirectory(collector, packageName));
            parameters.BuildPipeline = pipelineName;
            parameters.BuildBundleType = (int)bundleType;
            parameters.BuildTarget = EditorUserBuildSettings.activeBuildTarget;
            parameters.PackageName = packageName;
            parameters.PackageVersion = ResourceCollectorIntegration.GetBuildVersion(collector, packageName);
            parameters.EnableSharePackRule = bundleType == EBundleType.AssetBundle;
            parameters.VerifyBuildingResult = true;
            parameters.FileNameStyle = BundleBuilderSetting.GetPackageFileNameStyle(packageName, pipelineName);
            parameters.BundledCopyOption = bundledCopyOption ?? BundleBuilderSetting.GetPackageBundledCopyOption(packageName, pipelineName);
            parameters.BundledCopyParams = BundleBuilderSetting.GetPackageBundledCopyParams(packageName, pipelineName);
            parameters.ClearBuildCacheFiles = BundleBuilderSetting.GetPackageClearBuildCache(packageName, pipelineName);
            parameters.UseAssetDependencyDB = BundleBuilderSetting.GetPackageUseAssetDependencyDB(packageName, pipelineName);
            parameters.BundleEncryptor = CreateBuildService<IBundleEncryptor>(BundleBuilderSetting.GetPackageBundleEncryptorClassName(packageName, pipelineName));
            parameters.ManifestEncryptor = CreateBuildService<IManifestEncryptor>(BundleBuilderSetting.GetPackageManifestEncryptorClassName(packageName, pipelineName));
            parameters.ManifestDecryptor = CreateBuildService<IManifestDecryptor>(BundleBuilderSetting.GetPackageManifestDecryptorClassName(packageName, pipelineName));
            return parameters;
        }

        /// <summary>
        /// 根据名称创建 YooAsset 构建管线。
        /// </summary>
        /// <param name="pipelineName">构建管线名称。</param>
        /// <returns>构建管线。</returns>
        private static IBuildPipeline CreateBuildPipeline(string pipelineName)
        {
            switch (pipelineName)
            {
                case nameof(EBuildPipeline.ScriptableBuildPipeline):
                    return new ScriptableBuildPipeline();
                case nameof(EBuildPipeline.LegacyBuildPipeline):
                    return new LegacyBuildPipeline();
                case nameof(EBuildPipeline.RawFileBuildPipeline):
                    return new RawFileBuildPipeline();
                default:
                    throw new NotSupportedException("Unsupported YooAsset build pipeline: " + pipelineName);
            }
        }

        /// <summary>
        /// 按类型名称创建 YooAsset 构建服务。
        /// </summary>
        /// <typeparam name="T">构建服务接口类型。</typeparam>
        /// <param name="className">服务类型全名。</param>
        /// <returns>构建服务；未配置时返回空。</returns>
        private static T CreateBuildService<T>(string className) where T : class
        {
            if (string.IsNullOrEmpty(className))
            {
                return null;
            }
            foreach (System.Type type in YooAsset.Editor.EditorAssemblyUtility.GetAssignableTypes(typeof(T)))
            {
                if (type.FullName == className)
                {
                    return (T)Activator.CreateInstance(type);
                }
            }
            throw new InvalidOperationException("YooAsset build service is missing: " + className);
        }

        /// <summary>
        /// 根据包构建管线确定资源包类型。
        /// </summary>
        /// <param name="collector">资源收集配置。</param>
        /// <param name="packageName">资源包名称。</param>
        /// <returns>资源包类型。</returns>
        private static EBundleType GetBundleType(BundleCollectorSetting collector, string packageName)
        {
            ResourceCollectorIntegration.EnsureBuildDefaults(collector);
            string pipeline = BundleBuilderSetting.GetPackageBuildPipeline(packageName);
            return pipeline == nameof(EBuildPipeline.RawFileBuildPipeline) ? EBundleType.RawBundle : EBundleType.AssetBundle;
        }

        /// <summary>
        /// 将正式包类型转换为编辑器模拟包类型。
        /// </summary>
        /// <param name="bundleType">正式包类型。</param>
        /// <returns>对应的模拟包类型。</returns>
        private static EBundleType GetSimulationType(EBundleType bundleType)
        {
            switch (bundleType)
            {
                case EBundleType.AssetBundle:
                    return EBundleType.VirtualAssetBundle;
                case EBundleType.RawBundle:
                    return EBundleType.VirtualRawBundle;
                default:
                    throw new NotSupportedException("Unsupported simulation bundle type: " + bundleType);
            }
        }

        /// <summary>
        /// 将收集器 GUID 解析为资产路径并校验。
        /// </summary>
        /// <param name="package">资源包收集配置。</param>
        internal static void ResolveCollectorPaths(BundleCollectorPackage package)
        {
            foreach (BundleCollectorGroup group in package.Groups)
            {
                foreach (BundleCollector collector in group.Collectors)
                {
                    if (!string.IsNullOrEmpty(collector.CollectorGUID))
                    {
                        collector.CollectPath = AssetDatabase.GUIDToAssetPath(collector.CollectorGUID);
                    }
                    if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(collector.CollectPath)))
                    {
                        throw new InvalidOperationException("Resource collection path is missing: " + collector.CollectPath);
                    }
                }
            }
        }

    }
}
