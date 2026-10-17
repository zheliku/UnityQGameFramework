//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Tomlyn.Model;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor.ResourceTools
{
    /// <summary>
    /// 资源集合。
    /// </summary>
    public sealed class ResourceCollection
    {
        private const string SceneExtension = ".unity";
        private static readonly Regex ResourceNameRegex = new Regex(@"^([A-Za-z0-9\._-]+/)*[A-Za-z0-9\._-]+$");
        private static readonly Regex ResourceVariantRegex = new Regex(@"^[a-z0-9_-]+$");

        private readonly string m_ConfigurationPath;
        private readonly SortedDictionary<string, Resource> m_Resources;
        private readonly SortedDictionary<string, Asset> m_Assets;

        public ResourceCollection(string configurationPath = null)
        {
            m_ConfigurationPath = configurationPath ?? Type.GetConfigurationPath<ResourceCollectionConfigPathAttribute>() ?? Utility.Path.GetRegularPath(Path.Combine(Application.dataPath, "UnityQGameFramework/Configs/ResourceCollection.toml"));
            m_Resources = new SortedDictionary<string, Resource>(StringComparer.Ordinal);
            m_Assets = new SortedDictionary<string, Asset>(StringComparer.Ordinal);
        }

        public int ResourceCount => m_Resources.Count;

        public int AssetCount => m_Assets.Count;

        public event FrameworkAction<int, int> OnLoadingResource = null;

        public event FrameworkAction<int, int> OnLoadingAsset = null;

        public event FrameworkAction OnLoadCompleted = null;

        public void Clear()
        {
            m_Resources.Clear();
            m_Assets.Clear();
        }

        public bool Load()
        {
            if (!File.Exists(m_ConfigurationPath)) return false;
            try
            {
                var document = EditorToml.Read(m_ConfigurationPath);
                var staged = new ResourceCollection(m_ConfigurationPath);
                var resources = document.Tables("Resources");
                for (int i = 0; i < resources.Count; ++i)
                {
                    OnLoadingResource?.Invoke(i, resources.Count);
                    var entry = resources[i];
                    string name = entry.String("Name");
                    string variant = entry.String("Variant");
                    if (!staged.AddResource(name, variant, entry.String("FileSystem"), entry.EnumValue<LoadType>("LoadType"), entry.Boolean("Packed"), entry.Strings("ResourceGroups")))
                        throw new InvalidDataException("Invalid or duplicate resource: " + GetResourceFullName(name, variant));
                }
                var assets = document.Tables("Assets");
                for (int i = 0; i < assets.Count; ++i)
                {
                    OnLoadingAsset?.Invoke(i, assets.Count);
                    var entry = assets[i];
                    if (staged.m_Assets.ContainsKey(entry.String("Guid")) || !staged.AssignAsset(entry.String("Guid"), entry.String("ResourceName"), entry.String("ResourceVariant")))
                        throw new InvalidDataException("Cannot assign asset: " + entry.String("Guid"));
                }
                Clear();
                foreach (var entry in staged.m_Resources) m_Resources.Add(entry.Key, entry.Value);
                foreach (var entry in staged.m_Assets) m_Assets.Add(entry.Key, entry.Value);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Cannot load resource collection: " + m_ConfigurationPath + "\n" + exception.Message);
                return false;
            }
            finally { OnLoadCompleted?.Invoke(); }
        }

        public bool Save()
        {
            try
            {
                var resources = new TomlTableArray();
                foreach (Resource resource in m_Resources.Values)
                {
                    var entry = new TomlTable { ["Name"] = resource.Name, ["LoadType"] = resource.LoadType.ToString(), ["Packed"] = resource.Packed };
                    if (resource.Variant != null) entry["Variant"] = resource.Variant;
                    if (resource.FileSystem != null) entry["FileSystem"] = resource.FileSystem;
                    entry["ResourceGroups"] = EditorToml.ArrayOf(resource.GetResourceGroups());
                    resources.Add(entry);
                }
                var assets = new TomlTableArray();
                foreach (Asset asset in m_Assets.Values)
                {
                    var entry = new TomlTable { ["Guid"] = asset.Guid, ["ResourceName"] = asset.Resource.Name };
                    if (asset.Resource.Variant != null) entry["ResourceVariant"] = asset.Resource.Variant;
                    assets.Add(entry);
                }
                EditorToml.Write(m_ConfigurationPath, new TomlTable { ["Resources"] = resources, ["Assets"] = assets });
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Cannot save resource collection: " + m_ConfigurationPath + "\n" + exception.Message);
                return false;
            }
        }

        public Resource[] GetResources()
        {
            return m_Resources.Values.ToArray();
        }

        public Resource GetResource(string name, string variant)
        {
            if (!IsValidResourceName(name, variant))
            {
                return null;
            }

            Resource resource = null;
            if (m_Resources.TryGetValue(GetResourceFullName(name, variant).ToLowerInvariant(), out resource))
            {
                return resource;
            }

            return null;
        }

        public bool HasResource(string name, string variant)
        {
            if (!IsValidResourceName(name, variant))
            {
                return false;
            }

            return m_Resources.ContainsKey(GetResourceFullName(name, variant).ToLowerInvariant());
        }

        public bool AddResource(string name, string variant, string fileSystem, LoadType loadType, bool packed)
        {
            return AddResource(name, variant, fileSystem, loadType, packed, null);
        }

        public bool AddResource(string name, string variant, string fileSystem, LoadType loadType, bool packed, string[] resourceGroups)
        {
            if (!IsValidResourceName(name, variant))
            {
                return false;
            }

            if (!IsAvailableResourceName(name, variant, null))
            {
                return false;
            }

            if (fileSystem != null && !ResourceNameRegex.IsMatch(fileSystem))
            {
                return false;
            }

            Resource resource = Resource.Create(name, variant, fileSystem, loadType, packed, resourceGroups);
            m_Resources.Add(resource.FullName.ToLowerInvariant(), resource);

            return true;
        }

        public bool RenameResource(string oldName, string oldVariant, string newName, string newVariant)
        {
            if (!IsValidResourceName(oldName, oldVariant) || !IsValidResourceName(newName, newVariant))
            {
                return false;
            }

            Resource resource = GetResource(oldName, oldVariant);
            if (resource == null)
            {
                return false;
            }

            if (oldName == newName && oldVariant == newVariant)
            {
                return true;
            }

            if (!IsAvailableResourceName(newName, newVariant, resource))
            {
                return false;
            }

            m_Resources.Remove(resource.FullName.ToLowerInvariant());
            resource.Rename(newName, newVariant);
            m_Resources.Add(resource.FullName.ToLowerInvariant(), resource);

            return true;
        }

        public bool RemoveResource(string name, string variant)
        {
            if (!IsValidResourceName(name, variant))
            {
                return false;
            }

            Resource resource = GetResource(name, variant);
            if (resource == null)
            {
                return false;
            }

            Asset[] assets = resource.GetAssets();
            resource.Clear();
            m_Resources.Remove(resource.FullName.ToLowerInvariant());
            foreach (Asset asset in assets)
            {
                m_Assets.Remove(asset.Guid);
            }

            return true;
        }

        public bool SetResourceLoadType(string name, string variant, LoadType loadType)
        {
            if (!IsValidResourceName(name, variant))
            {
                return false;
            }

            Resource resource = GetResource(name, variant);
            if (resource == null)
            {
                return false;
            }

            if ((loadType == LoadType.LoadFromBinary || loadType == LoadType.LoadFromBinaryAndQuickDecrypt || loadType == LoadType.LoadFromBinaryAndDecrypt) && resource.GetAssets().Length > 1)
            {
                return false;
            }

            resource.LoadType = loadType;
            return true;
        }

        public bool SetResourcePacked(string name, string variant, bool packed)
        {
            if (!IsValidResourceName(name, variant))
            {
                return false;
            }

            Resource resource = GetResource(name, variant);
            if (resource == null)
            {
                return false;
            }

            resource.Packed = packed;
            return true;
        }

        public Asset[] GetAssets()
        {
            return m_Assets.Values.ToArray();
        }

        public Asset[] GetAssets(string name, string variant)
        {
            if (!IsValidResourceName(name, variant))
            {
                return new Asset[0];
            }

            Resource resource = GetResource(name, variant);
            if (resource == null)
            {
                return new Asset[0];
            }

            return resource.GetAssets();
        }

        public Asset GetAsset(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            Asset asset = null;
            if (m_Assets.TryGetValue(guid, out asset))
            {
                return asset;
            }

            return null;
        }

        public bool HasAsset(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return false;
            }

            return m_Assets.ContainsKey(guid);
        }

        public bool AssignAsset(string guid, string name, string variant)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return false;
            }

            if (!IsValidResourceName(name, variant))
            {
                return false;
            }

            Resource resource = GetResource(name, variant);
            if (resource == null)
            {
                return false;
            }

            string assetName = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetName))
            {
                return false;
            }

            Asset[] assetsInResource = resource.GetAssets();
            foreach (Asset assetInResource in assetsInResource)
            {
                if (assetInResource.Name == assetName)
                {
                    continue;
                }

                if (assetInResource.Name.ToLowerInvariant() == assetName.ToLowerInvariant())
                {
                    return false;
                }
            }

            bool isScene = assetName.EndsWith(SceneExtension, StringComparison.Ordinal);
            if (isScene && resource.AssetType == AssetType.Asset || !isScene && resource.AssetType == AssetType.Scene)
            {
                return false;
            }

            Asset asset = GetAsset(guid);
            if (resource.IsLoadFromBinary && assetsInResource.Length > 0 && asset != assetsInResource[0])
            {
                return false;
            }

            if (asset == null)
            {
                asset = Asset.Create(guid);
                m_Assets.Add(asset.Guid, asset);
            }

            resource.AssignAsset(asset, isScene);

            return true;
        }

        public bool UnassignAsset(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return false;
            }

            Asset asset = GetAsset(guid);
            if (asset != null)
            {
                asset.Resource.UnassignAsset(asset);
                m_Assets.Remove(asset.Guid);
            }

            return true;
        }

        private string GetResourceFullName(string name, string variant)
        {
            return !string.IsNullOrEmpty(variant) ? Utility.Text.Format("{0}.{1}", name, variant) : name;
        }

        private bool IsValidResourceName(string name, string variant)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (!ResourceNameRegex.IsMatch(name))
            {
                return false;
            }

            if (variant != null && !ResourceVariantRegex.IsMatch(variant))
            {
                return false;
            }

            return true;
        }

        private bool IsAvailableResourceName(string name, string variant, Resource current)
        {
            Resource found = GetResource(name, variant);
            if (found != null && found != current)
            {
                return false;
            }

            string[] foundPathNames = name.Split('/');
            foreach (Resource resource in m_Resources.Values)
            {
                if (current != null && resource == current)
                {
                    continue;
                }

                if (resource.Name == name)
                {
                    if (resource.Variant == null && variant != null)
                    {
                        return false;
                    }

                    if (resource.Variant != null && variant == null)
                    {
                        return false;
                    }
                }

                if (resource.Name.Length > name.Length
                    && resource.Name.IndexOf(name, StringComparison.CurrentCultureIgnoreCase) == 0
                    && resource.Name[name.Length] == '/')
                {
                    return false;
                }

                if (name.Length > resource.Name.Length
                    && name.IndexOf(resource.Name, StringComparison.CurrentCultureIgnoreCase) == 0
                    && name[resource.Name.Length] == '/')
                {
                    return false;
                }

                string[] pathNames = resource.Name.Split('/');
                for (int i = 0; i < foundPathNames.Length - 1 && i < pathNames.Length - 1; i++)
                {
                    if (foundPathNames[i].ToLowerInvariant() != pathNames[i].ToLowerInvariant())
                    {
                        break;
                    }

                    if (foundPathNames[i] != pathNames[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
