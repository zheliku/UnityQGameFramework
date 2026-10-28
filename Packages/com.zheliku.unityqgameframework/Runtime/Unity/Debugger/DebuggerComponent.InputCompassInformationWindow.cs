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
        /// 显示罗盘传感器数据。
        /// </summary>
        private sealed class InputCompassInformationWindow : ScrollableDebuggerWindowBase
        {
            /// <summary>
            /// 绘制指南针信息。
            /// </summary>
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Input Compass Information</b>");
                GUILayout.BeginVertical("box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Enable", GUILayout.Height(30f)))
                        {
                            Input.compass.enabled = true;
                        }
                        if (GUILayout.Button("Disable", GUILayout.Height(30f)))
                        {
                            Input.compass.enabled = false;
                        }
                    }
                    GUILayout.EndHorizontal();

                    DrawItem("Enabled", Input.compass.enabled.ToString());
                    if (Input.compass.enabled)
                    {
                        DrawItem("Heading Accuracy", Input.compass.headingAccuracy.ToString());
                        DrawItem("Magnetic Heading", Input.compass.magneticHeading.ToString());
                        DrawItem("Raw Vector", Input.compass.rawVector.ToString());
                        DrawItem("Timestamp", Input.compass.timestamp.ToString());
                        DrawItem("True Heading", Input.compass.trueHeading.ToString());
                    }
                }
                GUILayout.EndVertical();
            }
        }
    }
}
