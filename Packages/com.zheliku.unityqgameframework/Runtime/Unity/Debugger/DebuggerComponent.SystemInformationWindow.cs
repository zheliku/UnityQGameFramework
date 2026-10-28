//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 显示操作系统和硬件信息。
        /// </summary>
        private sealed class SystemInformationWindow : ScrollableDebuggerWindowBase
        {
            /// <summary>
            /// 绘制系统信息。
            /// </summary>
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>System Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Device Unique ID", SystemInfo.deviceUniqueIdentifier);
                    DrawItem("Device Name", SystemInfo.deviceName);
                    DrawItem("Device Type", SystemInfo.deviceType.ToString());
                    DrawItem("Device Model", SystemInfo.deviceModel);
                    DrawItem("Processor Type", SystemInfo.processorType);
                    DrawItem("Processor Count", SystemInfo.processorCount.ToString());
                    DrawItem("Processor Frequency", Utility.Text.Format("{0} MHz", SystemInfo.processorFrequency));
                    DrawItem("System Memory Size", Utility.Text.Format("{0} MB", SystemInfo.systemMemorySize));
                    DrawItem("Operating System Family", SystemInfo.operatingSystemFamily.ToString());
                    DrawItem("Operating System", SystemInfo.operatingSystem);
                    DrawItem("Battery Status", SystemInfo.batteryStatus.ToString());
                    DrawItem("Battery Level", GetBatteryLevelString(SystemInfo.batteryLevel));
                    DrawItem("Supports Audio", SystemInfo.supportsAudio.ToString());
                    DrawItem("Supports Location Service", SystemInfo.supportsLocationService.ToString());
                    DrawItem("Supports Accelerometer", SystemInfo.supportsAccelerometer.ToString());
                    DrawItem("Supports Gyroscope", SystemInfo.supportsGyroscope.ToString());
                    DrawItem("Supports Vibration", SystemInfo.supportsVibration.ToString());
                    DrawItem("Genuine", Application.genuine.ToString());
                    DrawItem("Genuine Check Available", Application.genuineCheckAvailable.ToString());
                }
                GUILayout.EndVertical();
            }

            /// <summary>
            /// 格式化电池电量。
            /// </summary>
            /// <param name="batteryLevel">电池剩余电量比例。</param>
            /// <returns>电量百分比或未知状态文本。</returns>
            private string GetBatteryLevelString(float batteryLevel)
            {
                if (batteryLevel < 0f)
                {
                    return "Unavailable";
                }

                return batteryLevel.ToString("P0");
            }
        }
    }
}
