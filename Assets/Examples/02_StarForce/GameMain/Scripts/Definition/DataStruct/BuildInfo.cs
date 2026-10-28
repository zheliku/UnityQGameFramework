//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace StarForce
{
    /// <summary>
    /// 应用版本和各平台下载地址。
    /// </summary>
    public class BuildInfo
    {
        /// <summary>
        /// 获取或设置游戏版本号。
        /// </summary>
        public string GameVersion
        {
            get; set;
        }

        /// <summary>
        /// 获取或设置内部游戏版本号。
        /// </summary>
        public int InternalGameVersion
        {
            get; set;
        }

        /// <summary>
        /// 获取或设置版本检查地址。
        /// </summary>
        public string CheckVersionUrl
        {
            get; set;
        }

        /// <summary>
        /// 获取或设置 Windows 应用下载地址。
        /// </summary>
        public string WindowsAppUrl
        {
            get; set;
        }

        /// <summary>
        /// 获取或设置 macOS 应用下载地址。
        /// </summary>
        public string MacOSAppUrl
        {
            get; set;
        }

        /// <summary>
        /// 获取或设置 iOS 应用下载地址。
        /// </summary>
        public string IOSAppUrl
        {
            get; set;
        }

        /// <summary>
        /// 获取或设置 Android 应用下载地址。
        /// </summary>
        public string AndroidAppUrl
        {
            get; set;
        }
    }
}
