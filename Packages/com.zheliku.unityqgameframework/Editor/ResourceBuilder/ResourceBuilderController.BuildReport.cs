//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tomlyn.Model;
using UnityEditor;

namespace UnityQGameFramework.Editor.ResourceTools
{
    public sealed partial class ResourceBuilderController
    {
        private sealed class BuildReport
        {
            private const string BuildReportName = "BuildReport.toml";
            private const string BuildLogName = "BuildLog.txt";

            private string m_BuildReportName = null;
            private string m_BuildLogName = null;
            private string m_ProductName = null;
            private string m_CompanyName = null;
            private string m_GameIdentifier = null;
            private string m_FrameworkVersion = null;
            private string m_UnityVersion = null;
            private string m_ApplicableGameVersion = null;
            private int m_InternalResourceVersion = 0;
            private Platform m_Platforms = Platform.Undefined;
            private AssetBundleCompressionType m_AssetBundleCompression;
            private string m_CompressionHelperTypeName;
            private bool m_AdditionalCompressionSelected = false;
            private bool m_ForceRebuildAssetBundleSelected = false;
            private string m_BuildEventHandlerTypeName;
            private string m_OutputDirectory;
            private BuildAssetBundleOptions m_BuildAssetBundleOptions = BuildAssetBundleOptions.None;
            private StringBuilder m_LogBuilder = null;
            private SortedDictionary<string, ResourceData> m_ResourceDatas = null;

            public void Initialize(string buildReportPath, string productName, string companyName, string gameIdentifier, string frameworkVersion, string unityVersion, string applicableGameVersion, int internalResourceVersion,
                Platform platforms, AssetBundleCompressionType assetBundleCompression, string compressionHelperTypeName, bool additionalCompressionSelected, bool forceRebuildAssetBundleSelected, string buildEventHandlerTypeName, string outputDirectory, BuildAssetBundleOptions buildAssetBundleOptions, SortedDictionary<string, ResourceData> resourceDatas)
            {
                if (string.IsNullOrEmpty(buildReportPath))
                {
                    throw new FrameworkException("Build report path is invalid.");
                }

                m_BuildReportName = Utility.Path.GetRegularPath(Path.Combine(buildReportPath, BuildReportName));
                m_BuildLogName = Utility.Path.GetRegularPath(Path.Combine(buildReportPath, BuildLogName));
                m_ProductName = productName;
                m_CompanyName = companyName;
                m_GameIdentifier = gameIdentifier;
                m_FrameworkVersion = frameworkVersion;
                m_UnityVersion = unityVersion;
                m_ApplicableGameVersion = applicableGameVersion;
                m_InternalResourceVersion = internalResourceVersion;
                m_Platforms = platforms;
                m_AssetBundleCompression = assetBundleCompression;
                m_CompressionHelperTypeName = compressionHelperTypeName;
                m_AdditionalCompressionSelected = additionalCompressionSelected;
                m_ForceRebuildAssetBundleSelected = forceRebuildAssetBundleSelected;
                m_BuildEventHandlerTypeName = buildEventHandlerTypeName;
                m_OutputDirectory = outputDirectory;
                m_BuildAssetBundleOptions = buildAssetBundleOptions;
                m_LogBuilder = new StringBuilder();
                m_ResourceDatas = resourceDatas;
            }

            public void LogInfo(string format, params object[] args)
            {
                LogInternal("INFO", format, args);
            }

            public void LogWarning(string format, params object[] args)
            {
                LogInternal("WARNING", format, args);
            }

            public void LogError(string format, params object[] args)
            {
                LogInternal("ERROR", format, args);
            }

            public void LogFatal(string format, params object[] args)
            {
                LogInternal("FATAL", format, args);
            }

            public void SaveReport()
            {
                var summary = new TomlTable
                {
                    ["ProductName"] = m_ProductName ?? string.Empty,
                    ["CompanyName"] = m_CompanyName ?? string.Empty,
                    ["GameIdentifier"] = m_GameIdentifier ?? string.Empty,
                    ["FrameworkVersion"] = m_FrameworkVersion ?? string.Empty,
                    ["UnityVersion"] = m_UnityVersion ?? string.Empty,
                    ["ApplicableGameVersion"] = m_ApplicableGameVersion ?? string.Empty,
                    ["InternalResourceVersion"] = (long)m_InternalResourceVersion,
                    ["Platforms"] = m_Platforms.ToString(),
                    ["AssetBundleCompression"] = m_AssetBundleCompression.ToString(),
                    ["CompressionHelperTypeName"] = m_CompressionHelperTypeName ?? string.Empty,
                    ["AdditionalCompressionSelected"] = m_AdditionalCompressionSelected,
                    ["ForceRebuildAssetBundleSelected"] = m_ForceRebuildAssetBundleSelected,
                    ["BuildEventHandlerTypeName"] = m_BuildEventHandlerTypeName ?? string.Empty,
                    ["OutputDirectory"] = m_OutputDirectory ?? string.Empty,
                    ["BuildAssetBundleOptions"] = m_BuildAssetBundleOptions.ToString(),
                    ["ResourceCount"] = (long)m_ResourceDatas.Count
                };
                var resources = new TomlTableArray();
                foreach (ResourceData resource in m_ResourceDatas.Values)
                {
                    var entry = new TomlTable
                    {
                        ["Name"] = resource.Name,
                        ["Extension"] = GetExtension(resource),
                        ["LoadType"] = resource.LoadType.ToString(),
                        ["Packed"] = resource.Packed,
                        ["ResourceGroups"] = EditorToml.ArrayOf(resource.GetResourceGroups())
                    };
                    if (resource.Variant != null) entry["Variant"] = resource.Variant;
                    if (resource.FileSystem != null) entry["FileSystem"] = resource.FileSystem;
                    var assets = new TomlTableArray();
                    foreach (AssetData asset in resource.GetAssetDatas())
                    {
                        assets.Add(new TomlTable
                        {
                            ["Guid"] = asset.Guid,
                            ["Name"] = asset.Name,
                            ["Length"] = (long)asset.Length,
                            ["HashCode"] = (long)asset.HashCode,
                            ["DependencyAssets"] = EditorToml.ArrayOf(asset.GetDependencyAssetNames())
                        });
                    }
                    entry["Assets"] = assets;
                    var codes = new TomlTableArray();
                    foreach (ResourceCode code in resource.GetCodes())
                    {
                        codes.Add(new TomlTable
                        {
                            ["Platform"] = code.Platform.ToString(),
                            ["Length"] = (long)code.Length,
                            ["HashCode"] = (long)code.HashCode,
                            ["CompressedLength"] = (long)code.CompressedLength,
                            ["CompressedHashCode"] = (long)code.CompressedHashCode
                        });
                    }
                    entry["Codes"] = codes;
                    resources.Add(entry);
                }
                EditorToml.Write(m_BuildReportName, new TomlTable { ["Summary"] = summary, ["Resources"] = resources });
                File.WriteAllText(m_BuildLogName, m_LogBuilder.ToString());
            }

            private void LogInternal(string type, string format, object[] args)
            {
                m_LogBuilder.AppendFormat("[{0:HH:mm:ss.fff}][{1}] ", DateTime.UtcNow.ToLocalTime(), type);
                m_LogBuilder.AppendFormat(format, args);
                m_LogBuilder.AppendLine();
            }
        }
    }
}
