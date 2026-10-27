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
        /// 显示帧时间、运行时间与时间缩放。
        /// </summary>
        private sealed class TimeInformationWindow : ScrollableDebuggerWindowBase
        {
            /// <summary>
            /// 绘制时间信息。
            /// </summary>
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Time Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Time Scale", Utility.Text.Format("{0} [{1}]", Time.timeScale, GetTimeScaleDescription(Time.timeScale)));
                    DrawItem("Realtime Since Startup", Time.realtimeSinceStartup.ToString());
                    DrawItem("Time Since Level Load", Time.timeSinceLevelLoad.ToString());
                    DrawItem("Time", Time.time.ToString());
                    DrawItem("Fixed Time", Time.fixedTime.ToString());
                    DrawItem("Unscaled Time", Time.unscaledTime.ToString());
                    DrawItem("Fixed Unscaled Time", Time.fixedUnscaledTime.ToString());
                    DrawItem("Delta Time", Time.deltaTime.ToString());
                    DrawItem("Fixed Delta Time", Time.fixedDeltaTime.ToString());
                    DrawItem("Unscaled Delta Time", Time.unscaledDeltaTime.ToString());
                    DrawItem("Fixed Unscaled Delta Time", Time.fixedUnscaledDeltaTime.ToString());
                    DrawItem("Smooth Delta Time", Time.smoothDeltaTime.ToString());
                    DrawItem("Maximum Delta Time", Time.maximumDeltaTime.ToString());
                    DrawItem("Maximum Particle Delta Time", Time.maximumParticleDeltaTime.ToString());
                    DrawItem("Frame Count", Time.frameCount.ToString());
                    DrawItem("Rendered Frame Count", Time.renderedFrameCount.ToString());
                    DrawItem("Capture Framerate", Time.captureFramerate.ToString());
                    DrawItem("Capture Delta Time", Time.captureDeltaTime.ToString());
                    DrawItem("In Fixed Time Step", Time.inFixedTimeStep.ToString());
                }
                GUILayout.EndVertical();
            }

            /// <summary>
            /// 获取时间缩放设置的描述。
            /// </summary>
            /// <param name="timeScale">时间缩放比例。</param>
            /// <returns>时间缩放状态的显示文本。</returns>
            private string GetTimeScaleDescription(float timeScale)
            {
                if (timeScale <= 0f)
                {
                    return "Pause";
                }

                if (timeScale < 1f)
                {
                    return "Slower";
                }

                if (timeScale > 1f)
                {
                    return "Faster";
                }

                return "Normal";
            }
        }
    }
}
