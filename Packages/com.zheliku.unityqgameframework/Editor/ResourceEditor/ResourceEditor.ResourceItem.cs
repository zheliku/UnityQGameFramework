//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor.ResourceTools
{
    internal sealed partial class ResourceEditor : EditorWindow
    {
        private sealed class ResourceItem
        {
            private static Texture s_CachedUnknownIcon = null;
            private static Texture s_CachedAssetIcon = null;
            private static Texture s_CachedSceneIcon = null;

            public ResourceItem(string name, Resource resource, ResourceFolder folder)
            {
                if (resource == null)
                {
                    throw new FrameworkException("Resource is invalid.");
                }

                if (folder == null)
                {
                    throw new FrameworkException("Resource folder is invalid.");
                }

                Name = name;
                Resource = resource;
                Folder = folder;
            }

            public string Name { get; private set; }

            public Resource Resource { get; private set; }

            public ResourceFolder Folder { get; private set; }

            public string FromRootPath => Folder.Folder == null ? Name : Utility.Text.Format("{0}/{1}", Folder.FromRootPath, Name);

            public int Depth => Folder != null ? Folder.Depth + 1 : 0;

            public Texture Icon
            {
                get
                {
                    if (Resource.IsLoadFromBinary)
                    {
                        Asset asset = Resource.GetFirstAsset();
                        if (asset != null)
                        {
                            Texture texture = AssetDatabase.GetCachedIcon(AssetDatabase.GUIDToAssetPath(asset.Guid));
                            return texture != null ? texture : CachedUnknownIcon;
                        }
                    }
                    else
                    {
                        switch (Resource.AssetType)
                        {
                            case AssetType.Asset:
                                return CachedAssetIcon;

                            case AssetType.Scene:
                                return CachedSceneIcon;
                        }
                    }

                    return CachedUnknownIcon;
                }
            }

            private static Texture CachedUnknownIcon
            {
                get
                {
                    if (s_CachedUnknownIcon == null)
                    {
                        string iconName = null;
                        iconName = "GameObject Icon";
                        s_CachedUnknownIcon = GetIcon(iconName);
                    }

                    return s_CachedUnknownIcon;
                }
            }

            private static Texture CachedAssetIcon
            {
                get
                {
                    if (s_CachedAssetIcon == null)
                    {
                        string iconName = null;
                        iconName = "Prefab Icon";
                        s_CachedAssetIcon = GetIcon(iconName);
                    }

                    return s_CachedAssetIcon;
                }
            }

            private static Texture CachedSceneIcon
            {
                get
                {
                    if (s_CachedSceneIcon == null)
                    {
                        s_CachedSceneIcon = GetIcon("SceneAsset Icon");
                    }

                    return s_CachedSceneIcon;
                }
            }

            private static Texture GetIcon(string iconName)
            {
                return EditorGUIUtility.IconContent(iconName).image;
            }
        }
    }
}
