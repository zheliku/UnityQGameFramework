//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core
{
    /// <summary>
    /// 检查资源包清单中是否存在指定资产的结果。
    /// </summary>
    public enum HasAssetResult : byte
    {
        /// <summary>
        /// 已加载的清单不包含指定资产。
        /// </summary>
        NotExist = 0,

        /// <summary>
        /// 资源包清单尚未就绪，无法判断资产是否存在。
        /// </summary>
        NotReady,

        /// <summary>
        /// 清单包含指定资产，加载时仍可能需要下载资源。
        /// </summary>
        Exists
    }
}
