//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 内置版本资源列表序列化器。
    /// </summary>
    public static partial class BuiltinVersionListSerializer
    {
        private const string DefaultExtension = "dat";
        private const int CachedHashBytesLength = 4;
        private static readonly byte[] s_CachedHashBytes = new byte[CachedHashBytesLength];

        /// <summary>
        /// 按资产名称比较依赖映射记录。
        /// </summary>
        /// <param name="a">第一条映射记录。</param>
        /// <param name="b">第二条映射记录。</param>
        /// <returns>表示资产名称排序关系的有符号整数。</returns>
        private static int AssetNameToDependencyAssetNamesComparer(KeyValuePair<string, string[]> a, KeyValuePair<string, string[]> b)
        {
            return a.Key.CompareTo(b.Key);
        }

        /// <summary>
        /// 查找资产在依赖映射列表中的索引。
        /// </summary>
        /// <param name="assetNameToDependencyAssetNames">按资产名称排序的依赖映射列表。</param>
        /// <param name="assetName">要查找的资产名称。</param>
        /// <returns>资产索引；不存在时返回 -1。</returns>
        private static int GetAssetNameIndex(List<KeyValuePair<string, string[]>> assetNameToDependencyAssetNames, string assetName)
        {
            return GetAssetNameIndexWithBinarySearch(assetNameToDependencyAssetNames, assetName, 0, assetNameToDependencyAssetNames.Count - 1);
        }

        /// <summary>
        /// 在指定范围内二分查找资产索引。
        /// </summary>
        /// <param name="assetNameToDependencyAssetNames">按资产名称排序的依赖映射列表。</param>
        /// <param name="assetName">要查找的资产名称。</param>
        /// <param name="leftIndex">查找范围左边界。</param>
        /// <param name="rightIndex">查找范围右边界。</param>
        /// <returns>资产索引；不存在时返回 -1。</returns>
        private static int GetAssetNameIndexWithBinarySearch(List<KeyValuePair<string, string[]>> assetNameToDependencyAssetNames, string assetName, int leftIndex, int rightIndex)
        {
            if (leftIndex > rightIndex)
            {
                return -1;
            }

            int middleIndex = (leftIndex + rightIndex) / 2;
            if (assetNameToDependencyAssetNames[middleIndex].Key == assetName)
            {
                return middleIndex;
            }

            if (assetNameToDependencyAssetNames[middleIndex].Key.CompareTo(assetName) > 0)
            {
                return GetAssetNameIndexWithBinarySearch(assetNameToDependencyAssetNames, assetName, leftIndex, middleIndex - 1);
            }
            else
            {
                return GetAssetNameIndexWithBinarySearch(assetNameToDependencyAssetNames, assetName, middleIndex + 1, rightIndex);
            }
        }
    }
}
