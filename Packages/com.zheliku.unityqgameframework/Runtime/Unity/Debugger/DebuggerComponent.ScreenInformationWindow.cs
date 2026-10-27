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
        /// 显示屏幕分辨率和显示设置。
        /// </summary>
        private sealed class ScreenInformationWindow : ScrollableDebuggerWindowBase
        {
            /// <summary>
            /// 绘制屏幕信息。
            /// </summary>
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Screen Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Current Resolution", GetResolutionString(Screen.currentResolution));
                    DrawItem("Screen Width", Utility.Text.Format("{0} px / {1:F2} in / {2:F2} cm", Screen.width, Utility.Converter.GetInchesFromPixels(Screen.width), Utility.Converter.GetCentimetersFromPixels(Screen.width)));
                    DrawItem("Screen Height", Utility.Text.Format("{0} px / {1:F2} in / {2:F2} cm", Screen.height, Utility.Converter.GetInchesFromPixels(Screen.height), Utility.Converter.GetCentimetersFromPixels(Screen.height)));
                    DrawItem("Screen DPI", Screen.dpi.ToString("F2"));
                    DrawItem("Screen Orientation", Screen.orientation.ToString());
                    DrawItem("Is Full Screen", Screen.fullScreen.ToString());
                    DrawItem("Full Screen Mode", Screen.fullScreenMode.ToString());
                    DrawItem("Sleep Timeout", GetSleepTimeoutDescription(Screen.sleepTimeout));
                    DrawItem("Brightness", Screen.brightness.ToString("F2"));
                    DrawItem("Cursor Visible", Cursor.visible.ToString());
                    DrawItem("Cursor Lock State", Cursor.lockState.ToString());
                    DrawItem("Auto Landscape Left", Screen.autorotateToLandscapeLeft.ToString());
                    DrawItem("Auto Landscape Right", Screen.autorotateToLandscapeRight.ToString());
                    DrawItem("Auto Portrait", Screen.autorotateToPortrait.ToString());
                    DrawItem("Auto Portrait Upside Down", Screen.autorotateToPortraitUpsideDown.ToString());
                    DrawItem("Safe Area", Screen.safeArea.ToString());
                    DrawItem("Cutouts", GetCutoutsString(Screen.cutouts));
                    DrawItem("Support Resolutions", GetResolutionsString(Screen.resolutions));
                }
                GUILayout.EndVertical();
            }

            /// <summary>
            /// 获取屏幕休眠设置的描述。
            /// </summary>
            /// <param name="sleepTimeout">屏幕休眠超时值。</param>
            /// <returns>休眠设置的显示文本。</returns>
            private string GetSleepTimeoutDescription(int sleepTimeout)
            {
                if (sleepTimeout == SleepTimeout.NeverSleep)
                {
                    return "Never Sleep";
                }

                if (sleepTimeout == SleepTimeout.SystemSetting)
                {
                    return "System Setting";
                }

                return sleepTimeout.ToString();
            }

            /// <summary>
            /// 格式化屏幕分辨率。
            /// </summary>
            /// <param name="resolution">屏幕分辨率。</param>
            /// <returns>分辨率和刷新率文本。</returns>
            private string GetResolutionString(Resolution resolution)
            {
                return Utility.Text.Format("{0} x {1} @ {2:0.##}Hz", resolution.width, resolution.height, resolution.refreshRateRatio.value);
            }

            /// <summary>
            /// 格式化屏幕缺口区域。
            /// </summary>
            /// <param name="cutouts">屏幕缺口矩形数组。</param>
            /// <returns>屏幕缺口区域文本。</returns>
            private string GetCutoutsString(Rect[] cutouts)
            {
                string[] cutoutStrings = new string[cutouts.Length];
                for (int i = 0; i < cutouts.Length; i++)
                {
                    cutoutStrings[i] = cutouts[i].ToString();
                }

                return string.Join("; ", cutoutStrings);
            }

            /// <summary>
            /// 格式化支持的分辨率列表。
            /// </summary>
            /// <param name="resolutions">分辨率数组。</param>
            /// <returns>分辨率列表文本。</returns>
            private string GetResolutionsString(Resolution[] resolutions)
            {
                string[] resolutionStrings = new string[resolutions.Length];
                for (int i = 0; i < resolutions.Length; i++)
                {
                    resolutionStrings[i] = GetResolutionString(resolutions[i]);
                }

                return string.Join("; ", resolutionStrings);
            }
        }
    }
}
