//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Debugger;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : FrameworkComponent
    {
        /// <summary>
        /// 提供可滚动的调试窗口内容区域。
        /// </summary>
        private abstract class ScrollableDebuggerWindowBase : IDebuggerWindow
        {
            private const float TitleWidth = 240f;
            private Vector2 m_ScrollPosition = Vector2.zero;

            /// <summary>
            /// 初始化可滚动调试窗口。
            /// </summary>
            /// <param name="args">窗口初始化参数。</param>
            public virtual void Initialize(params object[] args)
            {
            }

            /// <summary>
            /// 关闭调试窗口。
            /// </summary>
            public virtual void Shutdown()
            {
            }

            /// <summary>
            /// 进入调试窗口。
            /// </summary>
            public virtual void OnEnter()
            {
            }

            /// <summary>
            /// 离开调试窗口。
            /// </summary>
            public virtual void OnLeave()
            {
            }

            /// <summary>
            /// 更新调试窗口。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public virtual void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
            }

            /// <summary>
            /// 绘制滚动区域及窗口内容。
            /// </summary>
            public void OnDraw()
            {
                m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
                {
                    OnDrawScrollableWindow();
                }
                GUILayout.EndScrollView();
            }

            /// <summary>
            /// 绘制滚动区域内的窗口内容。
            /// </summary>
            protected abstract void OnDrawScrollableWindow();

            /// <summary>
            /// 绘制可点击复制的键值信息。
            /// </summary>
            /// <param name="title">信息标题。</param>
            /// <param name="content">信息内容。</param>
            protected static void DrawItem(string title, string content)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(title, GUILayout.Width(TitleWidth));
                    if (GUILayout.Button(content, "label"))
                    {
                        CopyToClipboard(content);
                    }
                }
                GUILayout.EndHorizontal();
            }

            /// <summary>
            /// 将字节数格式化为合适的容量单位。
            /// </summary>
            /// <param name="byteLength">字节数。</param>
            /// <returns>带容量单位的显示文本。</returns>
            protected static string GetByteLengthString(long byteLength)
            {
                if (byteLength < 1024L) // 2 ^ 10
                {
                    return Utility.Text.Format("{0} Bytes", byteLength);
                }

                if (byteLength < 1048576L) // 2 ^ 20
                {
                    return Utility.Text.Format("{0:F2} KB", byteLength / 1024f);
                }

                if (byteLength < 1073741824L) // 2 ^ 30
                {
                    return Utility.Text.Format("{0:F2} MB", byteLength / 1048576f);
                }

                if (byteLength < 1099511627776L) // 2 ^ 40
                {
                    return Utility.Text.Format("{0:F2} GB", byteLength / 1073741824f);
                }

                if (byteLength < 1125899906842624L) // 2 ^ 50
                {
                    return Utility.Text.Format("{0:F2} TB", byteLength / 1099511627776f);
                }

                if (byteLength < 1152921504606846976L) // 2 ^ 60
                {
                    return Utility.Text.Format("{0:F2} PB", byteLength / 1125899906842624f);
                }

                return Utility.Text.Format("{0:F2} EB", byteLength / 1152921504606846976f);
            }
        }
    }
}
