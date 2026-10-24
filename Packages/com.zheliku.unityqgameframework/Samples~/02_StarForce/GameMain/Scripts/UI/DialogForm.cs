//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    public class DialogForm : UGuiForm
    {
        [SerializeField]
        private Text m_TitleText = null;

        [SerializeField]
        private Text m_MessageText = null;

        [SerializeField]
        private GameObject[] m_ModeObjects = null;

        [SerializeField]
        private Text[] m_ConfirmTexts = null;

        [SerializeField]
        private Text[] m_CancelTexts = null;

        [SerializeField]
        private Text[] m_OtherTexts = null;

        private int m_DialogMode = 1;
        private FrameworkAction<object> m_OnClickConfirm = null;
        private FrameworkAction<object> m_OnClickCancel = null;
        private FrameworkAction<object> m_OnClickOther = null;
        private bool m_Answered;
        private bool m_PausedByDialog;

        public int DialogMode => m_DialogMode;

        public bool PauseGame { get; private set; }

        public object UserData { get; private set; }

        public void OnConfirmButtonClick()
        {
            Answer(m_OnClickConfirm);
        }

        public void OnCancelButtonClick()
        {
            Answer(m_OnClickCancel);
        }

        public void OnOtherButtonClick()
        {
            Answer(m_OnClickOther);
        }

        private void Answer(FrameworkAction<object> callback)
        {
            // 淡出期间仍可能收到点击，同一次打开只处理一个选择。
            if (m_Answered) return;
            m_Answered = true;
            object userData = UserData;
            Close();
            callback?.Invoke(userData);
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            DialogParams dialogParams = (DialogParams)userData;
            if (dialogParams == null)
            {
                Log.Warning("DialogParams is invalid.");
                return;
            }

            m_DialogMode = dialogParams.Mode;
            m_Answered = false;
            RefreshDialogMode();

            m_TitleText.text = dialogParams.Title;
            m_MessageText.text = dialogParams.Message;

            PauseGame = dialogParams.PauseGame;
            RefreshPauseGame();

            UserData = dialogParams.UserData;

            RefreshConfirmText(dialogParams.ConfirmText);
            m_OnClickConfirm = dialogParams.OnClickConfirm;

            RefreshCancelText(dialogParams.CancelText);
            m_OnClickCancel = dialogParams.OnClickCancel;

            RefreshOtherText(dialogParams.OtherText);
            m_OnClickOther = dialogParams.OnClickOther;
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            if (m_PausedByDialog)
            {
                GameEntry.Base.ResumeGame();
            }
            m_PausedByDialog = false;

            m_DialogMode = 1;
            m_TitleText.text = string.Empty;
            m_MessageText.text = string.Empty;
            PauseGame = false;
            UserData = null;

            RefreshConfirmText(string.Empty);
            m_OnClickConfirm = null;

            RefreshCancelText(string.Empty);
            m_OnClickCancel = null;

            RefreshOtherText(string.Empty);
            m_OnClickOther = null;

            base.OnClose(isShutdown, userData);
        }

        private void RefreshDialogMode()
        {
            for (int i = 1; i <= m_ModeObjects.Length; i++)
            {
                m_ModeObjects[i - 1].SetActive(i == m_DialogMode);
            }
        }

        private void RefreshPauseGame()
        {
            m_PausedByDialog = PauseGame && !GameEntry.Base.IsGamePaused;
            if (m_PausedByDialog)
            {
                GameEntry.Base.PauseGame();
            }
        }

        private void RefreshConfirmText(string confirmText)
        {
            if (string.IsNullOrEmpty(confirmText))
            {
                confirmText = GameEntry.Localization.GetString("Dialog.ConfirmButton");
            }

            for (int i = 0; i < m_ConfirmTexts.Length; i++)
            {
                m_ConfirmTexts[i].text = confirmText;
            }
        }

        private void RefreshCancelText(string cancelText)
        {
            if (string.IsNullOrEmpty(cancelText))
            {
                cancelText = GameEntry.Localization.GetString("Dialog.CancelButton");
            }

            for (int i = 0; i < m_CancelTexts.Length; i++)
            {
                m_CancelTexts[i].text = cancelText;
            }
        }

        private void RefreshOtherText(string otherText)
        {
            if (string.IsNullOrEmpty(otherText))
            {
                otherText = GameEntry.Localization.GetString("Dialog.OtherButton");
            }

            for (int i = 0; i < m_OtherTexts.Length; i++)
            {
                m_OtherTexts[i].text = otherText;
            }
        }
    }
}
