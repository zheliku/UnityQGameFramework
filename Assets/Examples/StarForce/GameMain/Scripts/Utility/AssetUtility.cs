//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;

namespace StarForce
{
    public static class AssetUtility
    {
        private static string s_GameMainPath;

        /// <summary>Resolve the sample in Assets/Examples or in a Package Manager sample import.</summary>
        public static string GameMainPath
        {
            get
            {
                if (!string.IsNullOrEmpty(s_GameMainPath))
                {
                    return s_GameMainPath;
                }

#if UNITY_EDITOR
                s_GameMainPath = UnityEditor.AssetDatabase.GUIDToAssetPath("db45535d062257049927cea5e857962f");
#else
                // The launcher is the first scene; cache its directory before changing scenes.
                string launcherPath = UnityEngine.SceneManagement.SceneManager.GetSceneAt(0).path;
                s_GameMainPath = System.IO.Path.GetDirectoryName(launcherPath).Replace('\\', '/') + "/GameMain";
#endif
                if (string.IsNullOrEmpty(s_GameMainPath))
                {
                    throw new System.InvalidOperationException("StarForce sample assets could not be located. Import the complete sample and start from StarForce Launcher.");
                }

                return s_GameMainPath;
            }
        }
        public static string GetConfigAsset(string assetName, bool fromBytes)
        {
            return Utility.Text.Format(GameMainPath + "/Configs/{0}.{1}", assetName, fromBytes ? "bytes" : "txt");
        }

        public static string GetDataTableAsset(string assetName, bool fromBytes)
        {
            return Utility.Text.Format(GameMainPath + "/DataTables/{0}.{1}", assetName, fromBytes ? "bytes" : "txt");
        }

        public static string GetDictionaryAsset(string assetName, bool fromBytes)
        {
            return Utility.Text.Format(GameMainPath + "/Localization/{0}/Dictionaries/{1}.{2}", GameEntry.Localization.Language, assetName, fromBytes ? "bytes" : "xml");
        }

        public static string GetFontAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/Fonts/{0}.ttf", assetName);
        }

        public static string GetSceneAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/Scenes/{0}.unity", assetName);
        }

        public static string GetMusicAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/Music/{0}.mp3", assetName);
        }

        public static string GetSoundAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/Sounds/{0}.wav", assetName);
        }

        public static string GetEntityAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/Entities/{0}.prefab", assetName);
        }

        public static string GetUIFormAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/UI/UIForms/{0}.prefab", assetName);
        }

        public static string GetUISoundAsset(string assetName)
        {
            return Utility.Text.Format(GameMainPath + "/UI/UISounds/{0}.wav", assetName);
        }
    }
}
