//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.Resource
{
    internal sealed partial class ResourceManager : FrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            private sealed class LoadBinaryInfo : IReference
            {
                public LoadBinaryInfo()
                {
                    BinaryAssetName = null;
                    ResourceInfo = null;
                    LoadBinaryCallbacks = null;
                    UserData = null;
                }

                public string BinaryAssetName { get; private set; }

                public ResourceInfo ResourceInfo { get; private set; }

                public LoadBinaryCallbacks LoadBinaryCallbacks { get; private set; }

                public object UserData { get; private set; }

                public static LoadBinaryInfo Create(string binaryAssetName, ResourceInfo resourceInfo, LoadBinaryCallbacks loadBinaryCallbacks, object userData)
                {
                    LoadBinaryInfo loadBinaryInfo = ReferencePool.Acquire<LoadBinaryInfo>();
                    loadBinaryInfo.BinaryAssetName = binaryAssetName;
                    loadBinaryInfo.ResourceInfo = resourceInfo;
                    loadBinaryInfo.LoadBinaryCallbacks = loadBinaryCallbacks;
                    loadBinaryInfo.UserData = userData;
                    return loadBinaryInfo;
                }

                public void Clear()
                {
                    BinaryAssetName = null;
                    ResourceInfo = null;
                    LoadBinaryCallbacks = null;
                    UserData = null;
                }
            }
        }
    }
}
