//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;

namespace StarForce
{
    /// <summary>
    /// 根据游戏资源目录构造可读资产路径。
    /// </summary>
    public static class AssetUtility
    {
        private static string s_GameMainPath;

        /// <summary>
        /// 获取启动场景所在示例的资源目录。
        /// </summary>
        public static string GameMainPath
        {
            get
            {
                if (!string.IsNullOrEmpty(s_GameMainPath))
                {
                    return s_GameMainPath;
                }

                // 从已加载的启动场景取路径，编辑器和 Player 共用，整个 Sample 移动后仍有效。
                string launcherPath = UnityEngine.SceneManagement.SceneManager.GetSceneByName("StarForce Launcher").path;
                if (string.IsNullOrEmpty(launcherPath))
                {
                    throw new System.InvalidOperationException(
                        "StarForce sample assets could not be located. "
                        + "Import the complete sample and start from StarForce Launcher.");
                }
                s_GameMainPath = System.IO.Path.GetDirectoryName(launcherPath).Replace('\\', '/') + "/GameMain";
                return s_GameMainPath;
            }
        }

        /// <summary>
        /// 取得配置资产路径。
        /// </summary>
        /// <param name="assetName">配置表中的资源名称，不含目录和扩展名。</param>
        /// <returns>相对于项目的完整资产路径。</returns>
        public static string GetConfigAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/Configs/{0}.toml", assetName);
        }

        /// <summary>
        /// 取得语言字典资产路径。
        /// </summary>
        /// <param name="language">需要加载字典的语言。</param>
        /// <returns>相对于项目的完整资产路径。</returns>
        public static string GetDictionaryAsset(Language language)
        {
            return Utility.Text.Format(GameMainPath + "/Localization/{0}.toml", language.Code);
        }

        /// <summary>
        /// 取得字体资产路径。
        /// </summary>
        /// <param name="assetName">配置表中的资源名称，不含目录和扩展名。</param>
        /// <returns>相对于项目的完整资产路径。</returns>
        public static string GetFontAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/Fonts/{0}.asset", assetName);
        }

        /// <summary>
        /// 取得场景资产路径。
        /// </summary>
        /// <param name="assetName">配置表中的资源名称，不含目录和扩展名。</param>
        /// <returns>相对于项目的完整资产路径。</returns>
        public static string GetSceneAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/Scenes/{0}.unity", assetName);
        }

        /// <summary>
        /// 取得音乐资产路径。
        /// </summary>
        /// <param name="assetName">配置表中的资源名称，不含目录和扩展名。</param>
        /// <returns>相对于项目的完整资产路径。</returns>
        public static string GetMusicAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/Music/{0}.mp3", assetName);
        }

        /// <summary>
        /// 取得音效资产路径。
        /// </summary>
        /// <param name="assetName">配置表中的资源名称，不含目录和扩展名。</param>
        /// <returns>相对于项目的完整资产路径。</returns>
        public static string GetSoundAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/Sounds/{0}.wav", assetName);
        }

        /// <summary>
        /// 取得Entity资产路径。
        /// </summary>
        /// <param name="assetName">配置表中的资源名称，不含目录和扩展名。</param>
        /// <returns>相对于项目的完整资产路径。</returns>
        public static string GetEntityAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/Entities/{0}.prefab", assetName);
        }

        /// <summary>
        /// 取得界面预制体资产路径。
        /// </summary>
        /// <param name="assetName">配置表中的资源名称，不含目录和扩展名。</param>
        /// <returns>相对于项目的完整资产路径。</returns>
        public static string GetUIFormAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/UI/UIForms/{0}.prefab", assetName);
        }

        /// <summary>
        /// 取得界面声音资产路径。
        /// </summary>
        /// <param name="assetName">配置表中的资源名称，不含目录和扩展名。</param>
        /// <returns>相对于项目的完整资产路径。</returns>
        public static string GetUISoundAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/UI/UISounds/{0}.wav", assetName);
        }
    }
}
