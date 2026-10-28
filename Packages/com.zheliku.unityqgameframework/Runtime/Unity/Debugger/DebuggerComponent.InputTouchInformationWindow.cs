//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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
                    Touchscreen screen = Touchscreen.current;
                    string touches = GetTouchesString(screen, out int count);
                    DrawItem("Touchscreen Present", (screen != null).ToString());
                    DrawItem("Touch Slots", screen != null ? screen.touches.Count.ToString() : "0");
                    DrawItem("Touch Count", count.ToString());
                    DrawItem("Touches", touches);
                }
                GUILayout.EndVertical();
            }

            /// <summary>
            /// 格式化当前正在进行的触摸点。
            /// </summary>
            /// <param name="screen">当前触摸屏；未连接时为空。</param>
            /// <param name="count">正在进行的触摸点数量。</param>
            /// <returns>触摸点编号、位置、位移、压力和阶段；没有触摸时返回空字符串。</returns>
            private string GetTouchesString(Touchscreen screen, out int count)
            {
                count = 0;
                if (screen == null)
                {
                    return string.Empty;
                }

                var text = new StringBuilder();
                foreach (TouchControl touch in screen.touches)
                {
                    if (!touch.isInProgress)
                    {
                        continue;
                    }

                    if (count++ > 0)
                    {
                        text.Append("; ");
                    }
                    text.Append(touch.touchId.ReadValue()).Append(": ").Append(touch.position.ReadValue())
                        .Append(", ").Append(touch.delta.ReadValue()).Append(", ").Append(touch.pressure.ReadValue())
                        .Append(", ").Append(touch.phase.ReadValue());
                }

                return text.ToString();
            }
        }
    }
}
