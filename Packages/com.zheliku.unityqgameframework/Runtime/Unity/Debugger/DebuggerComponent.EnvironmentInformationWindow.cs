//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 显示运行环境与设备信息。
        /// </summary>
        private sealed class EnvironmentInformationWindow : ScrollableDebuggerWindowBase
        {
            private ResourceComponent m_ResourceComponent = null;

            /// <summary>
            /// 获取运行环境组件。
            /// </summary>
            /// <param name="args">窗口初始化参数。</param>
            public override void Initialize(params object[] args)
            {
                m_ResourceComponent = GameEntry.GetComponent<ResourceComponent>();
                if (m_ResourceComponent == null)
                {
                    Log.Fatal("Resource component is invalid.");
                    return;
                }
            }

            /// <summary>
            /// 绘制运行环境信息。
            /// </summary>
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Environment Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Product Name", Application.productName);
                    DrawItem("Company Name", Application.companyName);
                    DrawItem("Game Identifier", Application.identifier);
                    DrawItem("Unity Q Game Framework Version", Version.GameFrameworkVersion);
                    DrawItem("Game Version", Utility.Text.Format("{0} ({1})", Version.GameVersion, Version.InternalGameVersion));
                    DrawItem("Resource Package", m_ResourceComponent.Settings == null ? "Resource Settings missing" : m_ResourceComponent.Settings.DefaultPackageName);
                    DrawItem("Application Version", Application.version);
                    DrawItem("Unity Version", Application.unityVersion);
                    DrawItem("Platform", Application.platform.ToString());
                    DrawItem("System Language", Application.systemLanguage.ToString());
                    DrawItem("Cloud Project Id", Application.cloudProjectId);
                    DrawItem("Build Guid", Application.buildGUID);
                    DrawItem("Target Frame Rate", Application.targetFrameRate.ToString());
                    DrawItem("Internet Reachability", Application.internetReachability.ToString());
                    DrawItem("Background Loading Priority", Application.backgroundLoadingPriority.ToString());
                    DrawItem("Is Playing", Application.isPlaying.ToString());
                    DrawItem("Splash Screen Is Finished", SplashScreen.isFinished.ToString());
                    DrawItem("Run In Background", Application.runInBackground.ToString());
                    DrawItem("Install Name", Application.installerName);
                    DrawItem("Install Mode", Application.installMode.ToString());
                    DrawItem("Sandbox Type", Application.sandboxType.ToString());
                    DrawItem("Is Mobile Platform", Application.isMobilePlatform.ToString());
                    DrawItem("Is Console Platform", Application.isConsolePlatform.ToString());
                    DrawItem("Is Editor", Application.isEditor.ToString());
                    DrawItem("Is Debug Build", Debug.isDebugBuild.ToString());
                    DrawItem("Is Focused", Application.isFocused.ToString());
                    DrawItem("Is Batch Mode", Application.isBatchMode.ToString());
                }
                GUILayout.EndVertical();
            }
        }
    }
}
