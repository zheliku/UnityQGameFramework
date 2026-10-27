//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : FrameworkComponent
    {
        /// <summary>
        /// 显示输入系统概要。
        /// </summary>
        private sealed class InputSummaryInformationWindow : ScrollableDebuggerWindowBase
        {
            /// <summary>
            /// 绘制输入概况。
            /// </summary>
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Input Summary Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Back Button Leaves App", Input.backButtonLeavesApp.ToString());
                    DrawItem("Device Orientation", Input.deviceOrientation.ToString());
                    DrawItem("Mouse Present", Input.mousePresent.ToString());
                    DrawItem("Mouse Position", Input.mousePosition.ToString());
                    DrawItem("Mouse Scroll Delta", Input.mouseScrollDelta.ToString());
                    DrawItem("Any Key", Input.anyKey.ToString());
                    DrawItem("Any Key Down", Input.anyKeyDown.ToString());
                    DrawItem("Input String", Input.inputString);
                    DrawItem("IME Is Selected", Input.imeIsSelected.ToString());
                    DrawItem("IME Composition Mode", Input.imeCompositionMode.ToString());
                    DrawItem("Compensate Sensors", Input.compensateSensors.ToString());
                    DrawItem("Composition Cursor Position", Input.compositionCursorPos.ToString());
                    DrawItem("Composition String", Input.compositionString);
                }
                GUILayout.EndVertical();
            }
        }
    }
}
