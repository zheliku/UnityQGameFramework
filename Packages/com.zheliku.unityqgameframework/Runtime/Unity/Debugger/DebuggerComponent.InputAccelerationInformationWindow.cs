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
        /// 显示加速度传感器数据。
        /// </summary>
        private sealed class InputAccelerationInformationWindow : ScrollableDebuggerWindowBase
        {
            /// <summary>
            /// 绘制加速度输入信息。
            /// </summary>
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Input Acceleration Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Acceleration", Input.acceleration.ToString());
                    DrawItem("Acceleration Event Count", Input.accelerationEventCount.ToString());
                    DrawItem("Acceleration Events", GetAccelerationEventsString(Input.accelerationEvents));
                }
                GUILayout.EndVertical();
            }

            /// <summary>
            /// 格式化单次加速度采样。
            /// </summary>
            /// <param name="accelerationEvent">加速度采样。</param>
            /// <returns>采样值及时间间隔文本。</returns>
            private string GetAccelerationEventString(AccelerationEvent accelerationEvent)
            {
                return Utility.Text.Format("{0}, {1}", accelerationEvent.acceleration, accelerationEvent.deltaTime);
            }

            /// <summary>
            /// 格式化加速度采样数组。
            /// </summary>
            /// <param name="accelerationEvents">加速度采样数组。</param>
            /// <returns>加速度采样列表文本。</returns>
            private string GetAccelerationEventsString(AccelerationEvent[] accelerationEvents)
            {
                string[] accelerationEventStrings = new string[accelerationEvents.Length];
                for (int i = 0; i < accelerationEvents.Length; i++)
                {
                    accelerationEventStrings[i] = GetAccelerationEventString(accelerationEvents[i]);
                }

                return string.Join("; ", accelerationEventStrings);
            }
        }
    }
}
