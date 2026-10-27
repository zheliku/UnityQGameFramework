//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 处理开始、设置、关于和退出请求。
    /// </summary>
    public class MenuForm : UGuiForm
    {
        [SerializeField]
        private GameObject m_QuitButton = null;
        private ProcedureMenu m_ProcedureMenu = null;

        /// <summary>
        /// 请求开始游戏。
        /// </summary>
        public void OnStartButtonClick()
        {
            m_ProcedureMenu.StartGame();
        }

        /// <summary>
        /// 打开设置界面。
        /// </summary>
        public void OnSettingButtonClick()
        {
            // 业务扩展用 UIFormId 查询 Luban 表，再调用 UIComponent 的资产名入口。
            GameEntry.UI.OpenUIForm(UIFormId.SettingForm);
        }

        /// <summary>
        /// 打开关于界面。
        /// </summary>
        public void OnAboutButtonClick()
        {
            GameEntry.UI.OpenUIForm(UIFormId.AboutForm);
        }

        /// <summary>
        /// 请求退出游戏。
        /// </summary>
        public void OnQuitButtonClick()
        {
            GameEntry.UI.OpenDialog(new DialogParams() { Mode = 2, Title = GameEntry.Localization.GetString("AskQuitGame.Title"), Message = GameEntry.Localization.GetString("AskQuitGame.Message"), OnClickConfirm = delegate (object userData)
            {
                UnityQGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Quit);
            }, });
        }

        /// <summary>
        /// 打开界面并接收业务数据。
        /// </summary>
        /// <param name="userData">菜单所属的 ProcedureMenu。</param>
        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            m_ProcedureMenu = (ProcedureMenu)userData;
            if (m_ProcedureMenu == null)
            {
                Log.Warning("ProcedureMenu is invalid when open MenuForm.");
                return;
            }

            m_QuitButton.SetActive(Application.platform != RuntimePlatform.IPhonePlayer);
        }

        /// <summary>
        /// 关闭界面并清理本次打开的状态。
        /// </summary>
        /// <param name="isShutdown">是否是关闭界面管理器时触发。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnClose(bool isShutdown, object userData)
        {
            m_ProcedureMenu = null;
            base.OnClose(isShutdown, userData);
        }
    }
}
