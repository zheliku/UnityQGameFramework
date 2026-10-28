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
    /// <summary>
    /// 管理启动场景携带的配置和界面模板。
    /// </summary>
    public class BuiltinDataComponent : GameFrameworkComponent
    {
        [SerializeField]
        private TextAsset m_BuildInfoTextAsset = null;

        [SerializeField]
        private TextAsset m_DefaultDictionaryTextAsset = null;

        [SerializeField]
        private UpdateResourceForm m_UpdateResourceFormTemplate = null;

        [SerializeField]
        private BuiltinDialogForm m_BuiltinDialogFormTemplate = null;

        private BuiltinDialogForm m_Dialog;

        /// <summary>
        /// 获取构建信息。
        /// </summary>
        public BuildInfo BuildInfo
        {
            get; private set;
        }

        /// <summary>
        /// 获取资源更新进度界面模板。
        /// </summary>
        public UpdateResourceForm UpdateResourceFormTemplate
        {
            get
            {
                return m_UpdateResourceFormTemplate;
            }
        }

        /// <summary>
        /// 打开不依赖资源包的启动对话框。
        /// </summary>
        /// <param name="parameters">对话框参数。</param>
        public void OpenDialog(DialogParams parameters)
        {
            // 关闭按钮先停用旧窗口，Destroy 到帧末才生效；回调中可立即打开重试提示。
            if (m_Dialog != null && m_Dialog.gameObject.activeSelf)
            {
                return;
            }
            // 启动场景直接携带模板，资源清单和 Luban 表尚未就绪时也能显示。
            m_Dialog = Instantiate(m_BuiltinDialogFormTemplate, transform);
            m_Dialog.Show(parameters);
        }

        /// <summary>
        /// 解析内置构建信息。
        /// </summary>
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
                string Text(string key)
                {
                    return settings.TryGetValue(key, out object value) && value is string text
                        ? text : throw new FormatException("BuildInfo requires string: " + key);
                }
                if (!(settings["InternalGameVersion"] is long version) || version < 0 || version > int.MaxValue)
                {
                    throw new FormatException("InternalGameVersion must be a non-negative Int32.");
                }
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
            catch (Exception exception)
            {
                Log.Error("Cannot parse BuildInfo.toml: {0}", exception.Message);
            }
        }

        /// <summary>
        /// 解析启动界面使用的内置字典。
        /// </summary>
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
