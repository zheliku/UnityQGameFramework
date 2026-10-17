//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Text;
using UnityEngine;
using UnityQGameFramework.Core.Config;

namespace UnityQGameFramework.Runtime
{
    /// <summary>Loads UTF-8 TOML settings through the existing Config component.</summary>
    public class DefaultConfigHelper : ConfigHelperBase
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private ResourceComponent m_ResourceComponent;

        public override bool ReadData(IConfigManager manager, string assetName, object asset, object userData)
        {
            if (asset is TextAsset text) return manager.ParseData(text.text, userData);
            Log.Warning("Config asset '{0}' is not a TextAsset.", assetName);
            return false;
        }

        public override bool ReadData(IConfigManager manager, string assetName, byte[] bytes, int startIndex, int length, object userData) =>
            ParseData(manager, bytes, startIndex, length, userData);

        public override bool ParseData(IConfigManager manager, string text, object userData)
        {
            try { TomlConfigParser.ParseAndAdd(manager, text); return true; }
            catch (Exception exception) { Log.Warning("Cannot parse TOML configuration: {0}", exception.Message); return false; }
        }

        public override bool ParseData(IConfigManager manager, byte[] bytes, int startIndex, int length, object userData)
        {
            try { return ParseData(manager, Utf8.GetString(bytes, startIndex, length), userData); }
            catch (Exception exception) { Log.Warning("Cannot decode UTF-8 TOML configuration: {0}", exception.Message); return false; }
        }

        public override void ReleaseDataAsset(IConfigManager manager, object asset) => m_ResourceComponent.UnloadAsset(asset);

        private void Start()
        {
            m_ResourceComponent = GameEntry.GetComponent<ResourceComponent>();
            if (m_ResourceComponent == null) Log.Fatal("Resource component is invalid.");
        }
    }
}
