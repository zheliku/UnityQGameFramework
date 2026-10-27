using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Editor;
using YooAsset;
using YooAsset.Editor;

/// <summary>
/// 检查 QGF 自动构建是否采用 YooAsset 原生构建选项。
/// </summary>
public static class ResourceBuildOptionsValidation
{
    /// <summary>
    /// 构建小型资源包并检查官方构建报告与首包产物。
    /// </summary>
    /// <returns>构建选项检查结果。</returns>
    public static object Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use the isolated validation project in Edit Mode.");
        }
        const string packageName = "QgfValidation.BuildOptions";
        string root = "Assets/Authoring/BuildOptions_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets/Authoring", Path.GetFileName(root));
        string previousPipeline = BundleBuilderSetting.GetPackageBuildPipeline(packageName);
        UnityEngine.Object previousSelection = Selection.activeObject;
        var outputs = new List<object>();
        try
        {
            var collector = ScriptableObject.CreateInstance<BundleCollectorSetting>();
            collector.ShowPackageView = true;
            // 故意关闭唯一包名，检查 QGF 没有把自己的偏好强加给收集配置。
            collector.UniqueBundleName = false;
            collector.Packages.Add(new BundleCollectorPackage
            {
                PackageName = packageName,
                EnableAddressable = true,
                SupportExtensionless = false,
                Groups = new List<BundleCollectorGroup>
                {
                    new BundleCollectorGroup
                    {
                        GroupName = "Base",
                        AssetTags = "Base",
                        Collectors = new List<BundleCollector>
                        {
                            new BundleCollector
                            {
                                CollectPath = AssetDatabase.GUIDToAssetPath("51d003874f9f553f84bccd96d341285c"),
                                CollectorGUID = "51d003874f9f553f84bccd96d341285c",
                                AddressRuleName = nameof(AddressByFileName),
                                PackRuleName = nameof(PackSeparately),
                                FilterRuleName = nameof(CollectPrefab)
                            }
                        }
                    }
                }
            });
            AssetDatabase.CreateAsset(collector, root + "/Collector.asset");
            Selection.activeObject = collector;
            string source = EditorJsonUtility.ToJson(collector);
            foreach (string pipeline in new[] { nameof(EBuildPipeline.ScriptableBuildPipeline), nameof(EBuildPipeline.LegacyBuildPipeline) })
            {
                collector = AssetDatabase.LoadAssetAtPath<BundleCollectorSetting>(root + "/Collector.asset");
                ECompressOption compression = BundleBuilderSetting.GetPackageCompressOption(packageName, pipeline);
                EFileNameStyle names = BundleBuilderSetting.GetPackageFileNameStyle(packageName, pipeline);
                EBundledCopyOption copy = BundleBuilderSetting.GetPackageBundledCopyOption(packageName, pipeline);
                string tags = BundleBuilderSetting.GetPackageBundledCopyParams(packageName, pipeline);
                bool clear = BundleBuilderSetting.GetPackageClearBuildCache(packageName, pipeline);
                bool dependency = BundleBuilderSetting.GetPackageUseAssetDependencyDB(packageName, pipeline);
                string encryptor = BundleBuilderSetting.GetPackageBundleEncryptorClassName(packageName, pipeline);
                try
                {
                    BundleBuilderSetting.SetPackageBuildPipeline(packageName, pipeline);
                    BundleBuilderSetting.SetPackageCompressOption(packageName, pipeline, ECompressOption.Uncompressed);
                    BundleBuilderSetting.SetPackageFileNameStyle(packageName, pipeline, EFileNameStyle.BundleName);
                    BundleBuilderSetting.SetPackageBundledCopyOption(packageName, pipeline, EBundledCopyOption.OnlyCopyByTags);
                    BundleBuilderSetting.SetPackageBundledCopyParams(packageName, pipeline, "Base");
                    BundleBuilderSetting.SetPackageClearBuildCache(packageName, pipeline, true);
                    BundleBuilderSetting.SetPackageUseAssetDependencyDB(packageName, pipeline, true);
                    string bundled = ResourceBuildService.GetBundledOutputDirectory(collector, packageName);
                    Directory.CreateDirectory(bundled);
                    string marker = Path.Combine(bundled, "keep.txt");
                    File.WriteAllText(marker, "OnlyCopyByTags preserves existing files");
                    string output = ResourceBuildService.Build(collector, packageName, false);
                    BuildReport report = BuildReport.Deserialize(File.ReadAllText(Directory.GetFiles(output, "*.report").Single()));
                    Require(report.Summary.BuildPipeline == pipeline, "The selected native pipeline is used.");
                    Require(report.Summary.CompressOption == ECompressOption.Uncompressed, "Native compression is used.");
                    Require(report.Summary.FileNameStyle == EFileNameStyle.BundleName, "Native bundle filenames are used.");
                    Require(report.Summary.ClearBuildCacheFiles && report.Summary.UseAssetDependencyDB, "Native cache options are used.");
                    Require(!report.Summary.UniqueBundleName, "Collector bundle naming is preserved.");
                    Require(report.Summary.EncryptionServicesClassName == encryptor, "The selected bundle encryptor is used.");
                    Require(report.Summary.ManifestProcessServicesClassName == BundleBuilderSetting.GetPackageManifestEncryptorClassName(packageName, pipeline), "The selected manifest encryptor is used.");
                    Require(report.Summary.ManifestRestoreServicesClassName == BundleBuilderSetting.GetPackageManifestDecryptorClassName(packageName, pipeline), "The selected manifest decryptor is used.");
                    Require(File.Exists(marker) && File.Exists(Path.Combine(bundled, "BuiltinCatalog.bytes")), "Native copy-by-tags preserves existing files and creates the builtin catalog.");
                    Require(source == EditorJsonUtility.ToJson(collector), "The Collector asset is unchanged after building.");

                    // 离线 Player 可显式覆盖本次首包选项，不改用户保存在原生窗口中的设置。
                    // Unity 构建可能卸载暂未使用的资产，下一次构建重新取得配置引用。
                    collector = AssetDatabase.LoadAssetAtPath<BundleCollectorSetting>(root + "/Collector.asset");
                    ResourceBuildService.Build(collector, packageName, false, EBundledCopyOption.ClearAndCopyAll);
                    Require(!File.Exists(marker), "The per-build full-copy option clears stale bundled files.");
                    Require(BundleBuilderSetting.GetPackageBundledCopyOption(packageName, pipeline) == EBundledCopyOption.OnlyCopyByTags,
                        "The per-build override does not overwrite native options.");

                    BundleBuilderSetting.SetPackageBundleEncryptorClassName(packageName, pipeline, "Missing.BuildOptionsEncryptor");
                    collector = AssetDatabase.LoadAssetAtPath<BundleCollectorSetting>(root + "/Collector.asset");
                    bool rejected = false;
                    try
                    {
                        ResourceBuildService.Build(collector, packageName, false);
                    }
                    catch (InvalidOperationException exception)
                    {
                        rejected = exception.Message.Contains("Missing.BuildOptionsEncryptor");
                    }
                    Require(rejected && File.Exists(Directory.GetFiles(output, "*.report").Single()), "A missing encryptor fails before deleting the last build.");
                    outputs.Add(new { pipeline, output, bundled });
                }
                finally
                {
                    BundleBuilderSetting.SetPackageCompressOption(packageName, pipeline, compression);
                    BundleBuilderSetting.SetPackageFileNameStyle(packageName, pipeline, names);
                    BundleBuilderSetting.SetPackageBundledCopyOption(packageName, pipeline, copy);
                    BundleBuilderSetting.SetPackageBundledCopyParams(packageName, pipeline, tags);
                    BundleBuilderSetting.SetPackageClearBuildCache(packageName, pipeline, clear);
                    BundleBuilderSetting.SetPackageUseAssetDependencyDB(packageName, pipeline, dependency);
                    BundleBuilderSetting.SetPackageBundleEncryptorClassName(packageName, pipeline, encryptor);
                }
            }
            var result = new { passed = true, unity = Application.unityVersion, builds = outputs };
            Directory.CreateDirectory("Artifacts~/ResourceExamples");
            File.WriteAllText("Artifacts~/ResourceExamples/build-options.json", JsonConvert.SerializeObject(result, Formatting.Indented));
            return result;
        }
        finally
        {
            BundleBuilderSetting.SetPackageBuildPipeline(packageName, previousPipeline);
            Selection.activeObject = previousSelection;
            AssetDatabase.DeleteAsset(root);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
