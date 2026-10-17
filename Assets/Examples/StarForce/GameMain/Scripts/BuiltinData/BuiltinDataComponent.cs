//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using Tomlyn;
using Tomlyn.Model;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    public class BuiltinDataComponent : FrameworkComponent
    {
        [SerializeField]
        private TextAsset m_BuildInfoTextAsset = null;

        [SerializeField]
        private TextAsset m_DefaultDictionaryTextAsset = null;

        [SerializeField]
        private UpdateResourceForm m_UpdateResourceFormTemplate = null;

        public BuildInfo BuildInfo { get; private set; }

        public UpdateResourceForm UpdateResourceFormTemplate => m_UpdateResourceFormTemplate;

        public void InitBuildInfo()
        {
            if (m_BuildInfoTextAsset == null || string.IsNullOrEmpty(m_BuildInfoTextAsset.text))
            {
                Log.Info("Build info can not be found or empty.");
                return;
            }

            try
            {
                var document = Toml.ToModel(m_BuildInfoTextAsset.text);
                var settings = document["BuildInfo"] as TomlTable ?? throw new FormatException("Missing BuildInfo table.");
                string Text(string key) => settings.TryGetValue(key, out object value) && value is string text
                    ? text : throw new FormatException("BuildInfo requires string: " + key);
                if (!(settings["InternalGameVersion"] is long version) || version < 0 || version > int.MaxValue)
                    throw new FormatException("InternalGameVersion must be a non-negative Int32.");
                BuildInfo = new BuildInfo
                {
                    GameVersion = Text("GameVersion"),
                    InternalGameVersion = (int)version,
                    CheckVersionUrl = Text("CheckVersionUrl"),
                    WindowsAppUrl = Text("WindowsAppUrl"),
                    MacOSAppUrl = Text("MacOSAppUrl"),
                    IOSAppUrl = Text("IOSAppUrl"),
                    AndroidAppUrl = Text("AndroidAppUrl")
                };
            }
            catch (Exception exception) { Log.Error("Cannot parse BuildInfo.toml: {0}", exception.Message); }

        }

        public void InitDefaultDictionary()
        {
            if (m_DefaultDictionaryTextAsset == null || string.IsNullOrEmpty(m_DefaultDictionaryTextAsset.text))
            {
                Log.Info("Default dictionary can not be found or empty.");
                return;
            }

            if (!GameEntry.Localization.ParseData(m_DefaultDictionaryTextAsset.text))
            {
                Log.Warning("Parse default dictionary failure.");
                return;
            }
        }
    }
}
