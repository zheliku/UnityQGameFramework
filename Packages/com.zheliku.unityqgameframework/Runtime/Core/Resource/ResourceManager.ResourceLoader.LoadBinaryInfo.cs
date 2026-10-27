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
            /// <summary>
            /// 记录二进制资源加载所需的信息。
            /// </summary>
            private sealed class LoadBinaryInfo : IReference
            {
                /// <summary>
                /// 初始化二进制加载请求。
                /// </summary>
                public LoadBinaryInfo()
                {
                    BinaryAssetName = null;
                    ResourceInfo = null;
                    LoadBinaryCallbacks = null;
                    UserData = null;
                }

                /// <summary>
                /// 获取二进制资产名称。
                /// </summary>
                public string BinaryAssetName { get; private set; }

                /// <summary>
                /// 获取二进制资产所属的资源记录。
                /// </summary>
                public ResourceInfo ResourceInfo { get; private set; }

                /// <summary>
                /// 获取二进制加载回调。
                /// </summary>
                public LoadBinaryCallbacks LoadBinaryCallbacks { get; private set; }

                /// <summary>
                /// 获取用户自定义数据。
                /// </summary>
                public object UserData { get; private set; }

                /// <summary>
                /// 从引用池获取二进制加载请求。
                /// </summary>
                /// <param name="binaryAssetName">二进制资产名称。</param>
                /// <param name="resourceInfo">资产所属的资源记录。</param>
                /// <param name="loadBinaryCallbacks">二进制加载回调。</param>
                /// <param name="userData">用户自定义数据。</param>
                /// <returns>二进制加载请求。</returns>
                public static LoadBinaryInfo Create(string binaryAssetName, ResourceInfo resourceInfo, LoadBinaryCallbacks loadBinaryCallbacks, object userData)
                {
                    LoadBinaryInfo loadBinaryInfo = ReferencePool.Acquire<LoadBinaryInfo>();
                    loadBinaryInfo.BinaryAssetName = binaryAssetName;
                    loadBinaryInfo.ResourceInfo = resourceInfo;
                    loadBinaryInfo.LoadBinaryCallbacks = loadBinaryCallbacks;
                    loadBinaryInfo.UserData = userData;
                    return loadBinaryInfo;
                }

                /// <summary>
                /// 清理二进制加载请求。
                /// </summary>
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
