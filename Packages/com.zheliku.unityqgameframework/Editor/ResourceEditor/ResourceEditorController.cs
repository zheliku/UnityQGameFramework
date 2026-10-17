//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tomlyn.Model;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor.ResourceTools
{
    public sealed class ResourceEditorController
    {
        private const string DefaultSourceAssetRootPath = "Assets";

        private readonly string m_ConfigurationPath;
        private readonly ResourceCollection m_ResourceCollection;
        private readonly List<string> m_SourceAssetSearchPaths;
        private readonly List<string> m_SourceAssetSearchRelativePaths;
        private readonly Dictionary<string, SourceAsset> m_SourceAssets;
        private string m_SourceAssetRootPath;
        private string m_SourceAssetUnionTypeFilter;
        private string m_SourceAssetUnionLabelFilter;
        private string m_SourceAssetExceptTypeFilter;
        private string m_SourceAssetExceptLabelFilter;
        private AssetSorterType m_AssetSorter;

        public ResourceEditorController(string configurationPath = null, string collectionPath = null)
        {
            m_ConfigurationPath = configurationPath ?? Type.GetConfigurationPath<ResourceEditorConfigPathAttribute>() ?? Utility.Path.GetRegularPath(Path.Combine(Application.dataPath, "UnityQGameFramework/Configs/ResourceEditor.toml"));
            m_ResourceCollection = new ResourceCollection(collectionPath);
            m_ResourceCollection.OnLoadingResource += delegate (int index, int count)
            {
                if (OnLoadingResource != null)
                {
                    OnLoadingResource(index, count);
                }
            };

            m_ResourceCollection.OnLoadingAsset += delegate (int index, int count)
            {
                if (OnLoadingAsset != null)
                {
                    OnLoadingAsset(index, count);
                }
            };

            m_ResourceCollection.OnLoadCompleted += delegate ()
            {
                if (OnLoadCompleted != null)
                {
                    OnLoadCompleted();
                }
            };

            m_SourceAssetSearchPaths = new List<string>();
            m_SourceAssetSearchRelativePaths = new List<string>();
            m_SourceAssets = new Dictionary<string, SourceAsset>(StringComparer.Ordinal);
            SourceAssetRoot = null;
            m_SourceAssetRootPath = null;
            m_SourceAssetUnionTypeFilter = null;
            m_SourceAssetUnionLabelFilter = null;
            m_SourceAssetExceptTypeFilter = null;
            m_SourceAssetExceptLabelFilter = null;
            m_AssetSorter = AssetSorterType.Path;

            SourceAssetRootPath = DefaultSourceAssetRootPath;
        }

        public int ResourceCount => m_ResourceCollection.ResourceCount;

        public int AssetCount => m_ResourceCollection.AssetCount;

        public SourceFolder SourceAssetRoot { get; private set; }

        public string SourceAssetRootPath
        {
            get
            {
                return m_SourceAssetRootPath;
            }
            set
            {
                if (m_SourceAssetRootPath == value)
                {
                    return;
                }

                m_SourceAssetRootPath = value.Replace('\\', '/');
                SourceAssetRoot = new SourceFolder(m_SourceAssetRootPath, null);
                RefreshSourceAssetSearchPaths();
            }
        }

        public string SourceAssetUnionTypeFilter
        {
            get
            {
                return m_SourceAssetUnionTypeFilter;
            }
            set
            {
                if (m_SourceAssetUnionTypeFilter == value)
                {
                    return;
                }

                m_SourceAssetUnionTypeFilter = value;
            }
        }

        public string SourceAssetUnionLabelFilter
        {
            get
            {
                return m_SourceAssetUnionLabelFilter;
            }
            set
            {
                if (m_SourceAssetUnionLabelFilter == value)
                {
                    return;
                }

                m_SourceAssetUnionLabelFilter = value;
            }
        }

        public string SourceAssetExceptTypeFilter
        {
            get
            {
                return m_SourceAssetExceptTypeFilter;
            }
            set
            {
                if (m_SourceAssetExceptTypeFilter == value)
                {
                    return;
                }

                m_SourceAssetExceptTypeFilter = value;
            }
        }

        public string SourceAssetExceptLabelFilter
        {
            get
            {
                return m_SourceAssetExceptLabelFilter;
            }
            set
            {
                if (m_SourceAssetExceptLabelFilter == value)
                {
                    return;
                }

                m_SourceAssetExceptLabelFilter = value;
            }
        }

        public AssetSorterType AssetSorter
        {
            get
            {
                return m_AssetSorter;
            }
            set
            {
                if (m_AssetSorter == value)
                {
                    return;
                }

                m_AssetSorter = value;
            }
        }

        public event FrameworkAction<int, int> OnLoadingResource = null;

        public event FrameworkAction<int, int> OnLoadingAsset = null;

        public event FrameworkAction OnLoadCompleted = null;

        public event FrameworkAction<SourceAsset[]> OnAssetAssigned = null;

        public event FrameworkAction<SourceAsset[]> OnAssetUnassigned = null;

        public bool Load()
        {
            if (!File.Exists(m_ConfigurationPath)) return false;
            try
            {
                var settings = EditorToml.Read(m_ConfigurationPath).Table("ResourceEditor");
                string root = ConfigurationPath.ResolveAssetPath(m_ConfigurationPath, settings.String("SourceAssetRootPath"));
                string[] paths = settings.Strings("SourceAssetSearchPaths");
                var sorter = settings.EnumValue<AssetSorterType>("AssetSorter");
                string unionType = settings.String("SourceAssetUnionTypeFilter", string.Empty);
                string unionLabel = settings.String("SourceAssetUnionLabelFilter", string.Empty);
                string exceptType = settings.String("SourceAssetExceptTypeFilter", string.Empty);
                string exceptLabel = settings.String("SourceAssetExceptLabelFilter", string.Empty);
                SourceAssetRootPath = root;
                m_SourceAssetSearchRelativePaths.Clear();
                m_SourceAssetSearchRelativePaths.AddRange(paths);
                SourceAssetUnionTypeFilter = unionType;
                SourceAssetUnionLabelFilter = unionLabel;
                SourceAssetExceptTypeFilter = exceptType;
                SourceAssetExceptLabelFilter = exceptLabel;
                AssetSorter = sorter;
                RefreshSourceAssetSearchPaths();
                ScanSourceAssets();
                return m_ResourceCollection.Load();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Cannot load resource editor settings: " + m_ConfigurationPath + "\n" + exception.Message);
                return false;
            }
        }

        public bool Save()
        {
            try
            {
                var settings = new TomlTable
                {
                    ["SourceAssetRootPath"] = ConfigurationPath.RelativeToConfiguration(m_ConfigurationPath, SourceAssetRootPath),
                    ["SourceAssetSearchPaths"] = EditorToml.ArrayOf(m_SourceAssetSearchRelativePaths.ToArray()),
                    ["SourceAssetUnionTypeFilter"] = SourceAssetUnionTypeFilter ?? string.Empty,
                    ["SourceAssetUnionLabelFilter"] = SourceAssetUnionLabelFilter ?? string.Empty,
                    ["SourceAssetExceptTypeFilter"] = SourceAssetExceptTypeFilter ?? string.Empty,
                    ["SourceAssetExceptLabelFilter"] = SourceAssetExceptLabelFilter ?? string.Empty,
                    ["AssetSorter"] = AssetSorter.ToString()
                };
                EditorToml.Write(m_ConfigurationPath, new TomlTable { ["ResourceEditor"] = settings });
                AssetDatabase.Refresh();
                return m_ResourceCollection.Save();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Cannot save resource editor settings: " + m_ConfigurationPath + "\n" + exception.Message);
                return false;
            }
        }

        public Resource[] GetResources()
        {
            return m_ResourceCollection.GetResources();
        }

        public Resource GetResource(string name, string variant)
        {
            return m_ResourceCollection.GetResource(name, variant);
        }

        public bool HasResource(string name, string variant)
        {
            return m_ResourceCollection.HasResource(name, variant);
        }

        public bool AddResource(string name, string variant, string fileSystem, LoadType loadType, bool packed)
        {
            return m_ResourceCollection.AddResource(name, variant, fileSystem, loadType, packed);
        }

        public bool RenameResource(string oldName, string oldVariant, string newName, string newVariant)
        {
            return m_ResourceCollection.RenameResource(oldName, oldVariant, newName, newVariant);
        }

        public bool RemoveResource(string name, string variant)
        {
            Asset[] assetsToRemove = m_ResourceCollection.GetAssets(name, variant);
            if (m_ResourceCollection.RemoveResource(name, variant))
            {
                List<SourceAsset> unassignedSourceAssets = new List<SourceAsset>();
                foreach (Asset asset in assetsToRemove)
                {
                    SourceAsset sourceAsset = GetSourceAsset(asset.Guid);
                    if (sourceAsset != null)
                    {
                        unassignedSourceAssets.Add(sourceAsset);
                    }
                }

                if (OnAssetUnassigned != null)
                {
                    OnAssetUnassigned(unassignedSourceAssets.ToArray());
                }

                return true;
            }

            return false;
        }

        public bool SetResourceLoadType(string name, string variant, LoadType loadType)
        {
            return m_ResourceCollection.SetResourceLoadType(name, variant, loadType);
        }

        public bool SetResourcePacked(string name, string variant, bool packed)
        {
            return m_ResourceCollection.SetResourcePacked(name, variant, packed);
        }

        public int RemoveUnusedResources()
        {
            List<Resource> resources = new List<Resource>(m_ResourceCollection.GetResources());
            List<Resource> removeResources = resources.FindAll(resource => GetAssets(resource.Name, resource.Variant).Length <= 0);
            foreach (Resource removeResource in removeResources)
            {
                m_ResourceCollection.RemoveResource(removeResource.Name, removeResource.Variant);
            }

            return removeResources.Count;
        }

        public Asset[] GetAssets(string name, string variant)
        {
            List<Asset> assets = new List<Asset>(m_ResourceCollection.GetAssets(name, variant));
            switch (AssetSorter)
            {
                case AssetSorterType.Path:
                    assets.Sort(AssetPathComparer);
                    break;

                case AssetSorterType.Name:
                    assets.Sort(AssetNameComparer);
                    break;

                case AssetSorterType.Guid:
                    assets.Sort(AssetGuidComparer);
                    break;
            }

            return assets.ToArray();
        }

        public Asset GetAsset(string guid)
        {
            return m_ResourceCollection.GetAsset(guid);
        }

        public bool AssignAsset(string guid, string name, string variant)
        {
            if (m_ResourceCollection.AssignAsset(guid, name, variant))
            {
                if (OnAssetAssigned != null)
                {
                    OnAssetAssigned(new SourceAsset[] { GetSourceAsset(guid) });
                }

                return true;
            }

            return false;
        }

        public bool UnassignAsset(string guid)
        {
            if (m_ResourceCollection.UnassignAsset(guid))
            {
                SourceAsset sourceAsset = GetSourceAsset(guid);
                if (sourceAsset != null)
                {
                    if (OnAssetUnassigned != null)
                    {
                        OnAssetUnassigned(new SourceAsset[] { sourceAsset });
                    }
                }

                return true;
            }

            return false;
        }

        public int RemoveUnknownAssets()
        {
            List<Asset> assets = new List<Asset>(m_ResourceCollection.GetAssets());
            List<Asset> removeAssets = assets.FindAll(asset => GetSourceAsset(asset.Guid) == null);
            foreach (Asset asset in removeAssets)
            {
                m_ResourceCollection.UnassignAsset(asset.Guid);
            }

            return removeAssets.Count;
        }

        public SourceAsset[] GetSourceAssets()
        {
            int count = 0;
            SourceAsset[] sourceAssets = new SourceAsset[m_SourceAssets.Count];
            foreach (KeyValuePair<string, SourceAsset> sourceAsset in m_SourceAssets)
            {
                sourceAssets[count++] = sourceAsset.Value;
            }

            return sourceAssets;
        }

        public SourceAsset GetSourceAsset(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            SourceAsset sourceAsset = null;
            if (m_SourceAssets.TryGetValue(guid, out sourceAsset))
            {
                return sourceAsset;
            }

            return null;
        }

        public void ScanSourceAssets()
        {
            m_SourceAssets.Clear();
            SourceAssetRoot.Clear();

            string[] sourceAssetSearchPaths = m_SourceAssetSearchPaths.ToArray();
            HashSet<string> tempGuids = new HashSet<string>();
            tempGuids.UnionWith(AssetDatabase.FindAssets(SourceAssetUnionTypeFilter, sourceAssetSearchPaths));
            tempGuids.UnionWith(AssetDatabase.FindAssets(SourceAssetUnionLabelFilter, sourceAssetSearchPaths));
            tempGuids.ExceptWith(AssetDatabase.FindAssets(SourceAssetExceptTypeFilter, sourceAssetSearchPaths));
            tempGuids.ExceptWith(AssetDatabase.FindAssets(SourceAssetExceptLabelFilter, sourceAssetSearchPaths));

            string[] guids = new List<string>(tempGuids).ToArray();
            foreach (string guid in guids)
            {
                string fullPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(fullPath))
                {
                    // Skip folder.
                    continue;
                }

                string assetPath = fullPath.Substring(SourceAssetRootPath.Length + 1);
                string[] splitedPath = assetPath.Split('/');
                SourceFolder folder = SourceAssetRoot;
                for (int i = 0; i < splitedPath.Length - 1; i++)
                {
                    SourceFolder subFolder = folder.GetFolder(splitedPath[i]);
                    folder = subFolder == null ? folder.AddFolder(splitedPath[i]) : subFolder;
                }

                SourceAsset asset = folder.AddAsset(guid, fullPath, splitedPath[splitedPath.Length - 1]);
                m_SourceAssets.Add(asset.Guid, asset);
            }
        }

        private void RefreshSourceAssetSearchPaths()
        {
            m_SourceAssetSearchPaths.Clear();

            if (string.IsNullOrEmpty(m_SourceAssetRootPath))
            {
                SourceAssetRootPath = DefaultSourceAssetRootPath;
            }

            if (m_SourceAssetSearchRelativePaths.Count > 0)
            {
                foreach (string sourceAssetSearchRelativePath in m_SourceAssetSearchRelativePaths)
                {
                    m_SourceAssetSearchPaths.Add(Utility.Path.GetRegularPath(Path.Combine(m_SourceAssetRootPath, sourceAssetSearchRelativePath)));
                }
            }
            else
            {
                m_SourceAssetSearchPaths.Add(m_SourceAssetRootPath);
            }
        }

        private int AssetPathComparer(Asset a, Asset b)
        {
            SourceAsset sourceAssetA = GetSourceAsset(a.Guid);
            SourceAsset sourceAssetB = GetSourceAsset(b.Guid);

            if (sourceAssetA != null && sourceAssetB != null)
            {
                return sourceAssetA.Path.CompareTo(sourceAssetB.Path);
            }

            if (sourceAssetA == null && sourceAssetB == null)
            {
                return a.Guid.CompareTo(b.Guid);
            }

            if (sourceAssetA == null)
            {
                return -1;
            }

            if (sourceAssetB == null)
            {
                return 1;
            }

            return 0;
        }

        private int AssetNameComparer(Asset a, Asset b)
        {
            SourceAsset sourceAssetA = GetSourceAsset(a.Guid);
            SourceAsset sourceAssetB = GetSourceAsset(b.Guid);

            if (sourceAssetA != null && sourceAssetB != null)
            {
                return sourceAssetA.Name.CompareTo(sourceAssetB.Name);
            }

            if (sourceAssetA == null && sourceAssetB == null)
            {
                return a.Guid.CompareTo(b.Guid);
            }

            if (sourceAssetA == null)
            {
                return -1;
            }

            if (sourceAssetB == null)
            {
                return 1;
            }

            return 0;
        }

        private int AssetGuidComparer(Asset a, Asset b)
        {
            SourceAsset sourceAssetA = GetSourceAsset(a.Guid);
            SourceAsset sourceAssetB = GetSourceAsset(b.Guid);

            if (sourceAssetA != null && sourceAssetB != null || sourceAssetA == null && sourceAssetB == null)
            {
                return a.Guid.CompareTo(b.Guid);
            }

            if (sourceAssetA == null)
            {
                return -1;
            }

            if (sourceAssetB == null)
            {
                return 1;
            }

            return 0;
        }
    }
}
