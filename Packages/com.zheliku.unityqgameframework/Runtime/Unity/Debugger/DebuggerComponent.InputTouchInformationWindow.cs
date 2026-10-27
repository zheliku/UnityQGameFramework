//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : FrameworkComponent
    {
        /// <summary>
        /// 显示当前触点状态。
        /// </summary>
        private sealed class InputTouchInformationWindow : ScrollableDebuggerWindowBase
        {
            /// <summary>
            /// 绘制触摸输入信息。
            /// </summary>
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Input Touch Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Touch Supported", Input.touchSupported.ToString());
                    DrawItem("Touch Pressure Supported", Input.touchPressureSupported.ToString());
                    DrawItem("Stylus Touch Supported", Input.stylusTouchSupported.ToString());
                    DrawItem("Simulate Mouse With Touches", Input.simulateMouseWithTouches.ToString());
                    DrawItem("Multi Touch Enabled", Input.multiTouchEnabled.ToString());
                    DrawItem("Touch Count", Input.touchCount.ToString());
                    DrawItem("Touches", GetTouchesString(Input.touches));
                }
                GUILayout.EndVertical();
            }

            /// <summary>
            /// 格式化单个触摸点。
            /// </summary>
            /// <param name="touch">触摸点数据。</param>
            /// <returns>触摸点位置、阶段等信息文本。</returns>
            private string GetTouchString(Touch touch)
            {
                return Utility.Text.Format("{0}, {1}, {2}, {3}, {4}", touch.position, touch.deltaPosition, touch.rawPosition, touch.pressure, touch.phase);
            }

            /// <summary>
            /// 格式化触摸点数组。
            /// </summary>
            /// <param name="touches">触摸点数组。</param>
            /// <returns>触摸点列表文本。</returns>
            private string GetTouchesString(Touch[] touches)
            {
                string[] touchStrings = new string[touches.Length];
                for (int i = 0; i < touches.Length; i++)
                {
                    touchStrings[i] = GetTouchString(touches[i]);
                }

                return string.Join("; ", touchStrings);
            }
        }
    }
}
