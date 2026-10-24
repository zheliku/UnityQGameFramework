//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace StarForce
{
    // JsonUtility 读取字段，名称与版本服务返回的 JSON 一致。
    [System.Serializable]
    public class VersionInfo
    {
        // 是否需要强制更新游戏应用
        public bool ForceUpdateGame;

        // 最新的游戏版本号
        public string LatestGameVersion;

        // 最新的游戏内部版本号
        public int InternalGameVersion;

        // 最新的资源内部版本号
        public int InternalResourceVersion;

        // 资源更新下载地址
        public string UpdatePrefixUri;

        // 资源版本列表长度
        public int VersionListLength;

        // 资源版本列表哈希值
        public int VersionListHashCode;

        // 资源版本列表压缩后长度
        public int VersionListCompressedLength;

        // 资源版本列表压缩后哈希值
        public int VersionListCompressedHashCode;
    }
}
