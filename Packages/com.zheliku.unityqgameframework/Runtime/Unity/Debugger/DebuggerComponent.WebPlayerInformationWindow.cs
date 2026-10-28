//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 显示 Web 播放环境信息。
        /// </summary>
        private sealed class WebPlayerInformationWindow : ScrollableDebuggerWindowBase
        {
            /// <summary>
            /// 绘制浏览器运行信息。
            /// </summary>
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Web Player Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Absolute URL", Application.absoluteURL);
                }
                GUILayout.EndVertical();
            }
        }
    }
}
