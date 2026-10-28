//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem;

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
                    Mouse mouse = Mouse.current;
                    Keyboard keyboard = Keyboard.current;
                    DrawItem("Input Devices", InputSystem.devices.Count.ToString());
                    DrawItem("Screen Orientation", Screen.orientation.ToString());
                    DrawItem("Mouse Present", (mouse != null).ToString());
                    DrawItem("Mouse Position", mouse != null ? mouse.position.ReadValue().ToString() : "N/A");
                    DrawItem("Mouse Scroll Delta", mouse != null ? mouse.scroll.ReadValue().ToString() : "N/A");
                    DrawItem("Keyboard Present", (keyboard != null).ToString());
                    DrawItem("Keyboard Layout", keyboard != null ? keyboard.keyboardLayout : "N/A");
                    DrawItem("Any Key", (keyboard != null && keyboard.anyKey.isPressed).ToString());
                    DrawItem("Any Key Down", (keyboard != null && keyboard.anyKey.wasPressedThisFrame).ToString());
                    DrawItem("IME Is Selected", (keyboard != null && keyboard.imeSelected.isPressed).ToString());
                    DrawItem("Compensate Sensors", InputSystem.settings.compensateForScreenOrientation.ToString());
                    DrawItem("Update Mode", InputSystem.settings.updateMode.ToString());
                }
                GUILayout.EndVertical();
            }
        }
    }
}
