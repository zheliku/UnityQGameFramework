using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Runtime;
using YooAsset;
using YooAsset.Editor;

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
        public static bool IsBuilding { get; private set; }

        /// <summary>
        /// 获取指定资源包的构建配置。
        /// </summary>
        /// <param name="settings">运行时资源配置。</param>
        /// <param name="packageName">资源包名称。</param>
        /// <returns>构建配置。</returns>
        public static ResourceBuildProfile GetProfile(ResourceSettings settings, string packageName)
        {
            ResourceBuildProfile result = FindProfile(settings, packageName);
            if (result == null)
            {
                throw new InvalidOperationException("Resource build profile is missing: " + packageName);
            }
            return result;
        }

        /// <summary>
        /// 获取首包资源输出目录。
        /// </summary>
        /// <param name="profile">资源构建配置。</param>
        /// <returns>由构建配置生成的首包目录。</returns>
        public static string GetBundledOutputDirectory(ResourceBuildProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }
            string root = Path.GetDirectoryName(Application.dataPath);
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile));
            if (string.IsNullOrEmpty(guid))
            {
                throw new InvalidOperationException("Save the resource build profile before building.");
            }
            string staging = Path.GetFullPath(Path.Combine(root, "QgfBuild~", guid, "B"));
            string output = Path.GetFullPath(Path.Combine(staging, profile.Package.PackageName));
            if (!string.Equals(Path.GetDirectoryName(output), staging, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Resource package name must be a directory name.");
            }
            return output;
        }

        internal static ResourceBuildProfile FindProfile(ResourceSettings settings, string packageName)
        {
            ResourceBuildProfile result = null;
            foreach (string guid in AssetDatabase.FindAssets("t:ResourceBuildProfile", new[] { "Assets" }))
            {
                var profile = AssetDatabase.LoadAssetAtPath<ResourceBuildProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if (profile.Settings == settings && profile.Package.PackageName == packageName)
                {
                    if (result != null)
                    {
                        throw new InvalidOperationException("Duplicate resource build profiles: " + packageName);
                    }
                    result = profile;
                }
            }
            return result;
        }

        /// <summary>
        /// 构建指定资源包。
        /// </summary>
        /// <param name="profile">构建配置。</param>
        /// <param name="simulate">是否构建编辑器模拟资源。</param>
        /// <returns>资源包输出目录。</returns>
        public static string Build(ResourceBuildProfile profile, bool simulate)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }
            if (IsBuilding)
            {
                throw new InvalidOperationException("A resource build is already in progress.");
            }
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile));
            if (string.IsNullOrEmpty(guid))
            {
                throw new InvalidOperationException("Save the resource build profile before building.");
            }
            if (string.IsNullOrWhiteSpace(profile.Package.PackageName) || string.IsNullOrWhiteSpace(profile.Version))
            {
                throw new InvalidOperationException("Resource package name and version are required.");
            }
            if (profile.Version == "." || profile.Version == ".." || profile.Version.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidOperationException("Resource package version must be a directory name.");
            }

            BundleCollectorSetting setting = GetCollectorSetting();
            List<BundleCollectorPackage> packages = setting.Packages;
            bool uniqueBundleName = setting.UniqueBundleName;
            IsBuilding = true;
            try
            {
                // 仅替换构建期间的内存引用；不保存、清空或覆盖用户的收集器资产。
                var package = JsonUtility.FromJson<BundleCollectorPackage>(JsonUtility.ToJson(profile.Package));
                ResolveCollectorPaths(package);
                setting.Packages = new List<BundleCollectorPackage> { package };
                setting.UniqueBundleName = true;

                BuildParameters parameters;
                IBuildPipeline pipeline;
                if (simulate)
                {
                    parameters = new EditorSimulateBuildParameters();
                    parameters.BuildPipeline = EBuildPipeline.EditorSimulateBuildPipeline.ToString();
                    parameters.BuildBundleType = (int)GetSimulationType(profile.BundleType);
                    pipeline = new EditorSimulateBuildPipeline();
                }
                else if (profile.BundleType == EBundleType.RawBundle)
                {
                    parameters = new RawFileBuildParameters();
                    parameters.BuildPipeline = EBuildPipeline.RawFileBuildPipeline.ToString();
                    parameters.BuildBundleType = (int)EBundleType.RawBundle;
                    pipeline = new RawFileBuildPipeline();
                }
                else if (profile.BundleType == EBundleType.AssetBundle)
                {
                    var scriptable = new ScriptableBuildParameters();
                    scriptable.CompressOption = ECompressOption.LZ4;
                    scriptable.BuiltinShadersBundleName = package.PackageName.ToLowerInvariant() + "_shaders";
                    parameters = scriptable;
                    parameters.BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString();
                    parameters.BuildBundleType = (int)EBundleType.AssetBundle;
                    pipeline = new ScriptableBuildPipeline();
                }
                else
                {
                    throw new NotSupportedException("Unsupported resource bundle type: " + profile.BundleType);
                }

                string root = Path.GetDirectoryName(Application.dataPath);
                parameters.BuildOutputRoot = Path.Combine(root, "QgfBuild~", guid, simulate ? "S" : "P");
                parameters.BundledFileRoot = Path.GetDirectoryName(GetBundledOutputDirectory(profile));
                parameters.BuildTarget = EditorUserBuildSettings.activeBuildTarget;
                if (!simulate && parameters.BuildTarget == BuildTarget.NoTarget)
                {
                    throw new InvalidOperationException("Select a build target before building a YooAsset package.");
                }
                parameters.PackageName = package.PackageName;
                parameters.PackageVersion = profile.Version;
                parameters.FileNameStyle = EFileNameStyle.HashName;
                // 同时生成首包目录，供 Offline Player 使用 BuiltinCatalog 和内置 Bundle。
                parameters.BundledCopyOption = simulate ? EBundledCopyOption.None : EBundledCopyOption.ClearAndCopyAll;
                parameters.VerifyBuildingResult = true;
                parameters.UseAssetDependencyDB = false;

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

                BuildResult result = pipeline.Run(parameters, false);
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

        private static void ResolveCollectorPaths(BundleCollectorPackage package)
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

        private static BundleCollectorSetting GetCollectorSetting()
        {
            if (AssetDatabase.FindAssets("t:BundleCollectorSetting").Length == 0)
            {
                const string folder = "Assets/UnityQGameFramework/Resource/Editor";
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
                // 项目只需要一份官方收集器；各 Sample 的规则保存在自己的 Profile 中。
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<BundleCollectorSetting>(), folder + "/YooAssetCollector.asset");
            }
            return BundleCollectorSettingData.Setting;
        }
    }
}
