//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 在调试器中切换游戏语言。
    /// </summary>
    public class ChangeLanguageDebuggerWindow : IDebuggerWindow
    {
        private Vector2 m_ScrollPosition = Vector2.zero;
        private bool m_NeedRestart = false;

        /// <summary>
        /// 初始化本局游戏。
        /// </summary>
        /// <param name="args">调试窗口初始化参数。</param>
        public void Initialize(params object[] args)
        {
        }

        /// <summary>
        /// 结束本局并解除事件订阅。
        /// </summary>
        public void Shutdown()
        {
        }

        /// <summary>
        /// 进入调试窗口。
        /// </summary>
        public void OnEnter()
        {
        }

        /// <summary>
        /// 离开调试窗口。
        /// </summary>
        public void OnLeave()
        {
        }

        /// <summary>
        /// 更新调试窗口状态。
        /// </summary>
        /// <param name="elapseSeconds">逻辑时间间隔，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实时间间隔，以秒为单位。</param>
        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (m_NeedRestart)
            {
                m_NeedRestart = false;
                UnityQGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Restart);
            }
        }

        /// <summary>
        /// 绘制语言切换按钮。
        /// </summary>
        public void OnDraw()
        {
            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
            {
                DrawSectionChangeLanguage();
            }
            GUILayout.EndScrollView();
        }

        private void DrawSectionChangeLanguage()
        {
            GUILayout.Label("<b>Change Language</b>");
            GUILayout.BeginHorizontal("box");
            {
                if (GUILayout.Button("Chinese Simplified", GUILayout.Height(30)))
                {
                    GameEntry.Localization.Language = Language.ChineseSimplified;
                    SaveLanguage();
                }
                if (GUILayout.Button("Chinese Traditional", GUILayout.Height(30)))
                {
                    GameEntry.Localization.Language = Language.ChineseTraditional;
                    SaveLanguage();
                }
                if (GUILayout.Button("English", GUILayout.Height(30)))
                {
                    GameEntry.Localization.Language = Language.English;
                    SaveLanguage();
                }
            }
            GUILayout.EndHorizontal();
        }

        private void SaveLanguage()
        {
            GameEntry.Setting.SetString(Constant.Setting.Language, GameEntry.Localization.Language.ToString());
            GameEntry.Setting.Save();
            m_NeedRestart = true;
        }
    }
}
