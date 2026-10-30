//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace StarForce
{
    /// <summary>
    /// 编辑音量、静音和语言设置。
    /// </summary>
    public class SettingForm : UGuiForm
    {
        [SerializeField]
        private Toggle m_MusicMuteToggle = null;

        [SerializeField]
        private Slider m_MusicVolumeSlider = null;

        [SerializeField]
        private Toggle m_SoundMuteToggle = null;

        [SerializeField]
        private Slider m_SoundVolumeSlider = null;

        [SerializeField]
        private Toggle m_UISoundMuteToggle = null;

        [SerializeField]
        private Slider m_UISoundVolumeSlider = null;

        [SerializeField]
        private Toggle m_EnglishToggle = null;

        [SerializeField]
        private Toggle m_ChineseSimplifiedToggle = null;

        [SerializeField]
        private Toggle m_ChineseTraditionalToggle = null;

        [SerializeField]
        private Toggle m_KoreanToggle = null;

        private Language m_SelectedLanguage = Language.Unspecified;
        private CancellationTokenSource m_LanguageCancellation;
        private bool m_ApplyingLanguage;

        /// <summary>
        /// 修改音乐静音状态。
        /// </summary>
        /// <param name="isOn">对应开关是否选中。</param>
        public void OnMusicMuteChanged(bool isOn)
        {
            GameEntry.Sound.Mute("Music", !isOn);
            m_MusicVolumeSlider.gameObject.SetActive(isOn);
        }

        /// <summary>
        /// 修改音乐音量。
        /// </summary>
        /// <param name="volume">音量，范围为 0 到 1。</param>
        public void OnMusicVolumeChanged(float volume)
        {
            GameEntry.Sound.SetVolume("Music", volume);
        }

        /// <summary>
        /// 修改音效静音状态。
        /// </summary>
        /// <param name="isOn">对应开关是否选中。</param>
        public void OnSoundMuteChanged(bool isOn)
        {
            GameEntry.Sound.Mute("Sound", !isOn);
            m_SoundVolumeSlider.gameObject.SetActive(isOn);
        }

        /// <summary>
        /// 修改音效音量。
        /// </summary>
        /// <param name="volume">音量，范围为 0 到 1。</param>
        public void OnSoundVolumeChanged(float volume)
        {
            GameEntry.Sound.SetVolume("Sound", volume);
        }

        /// <summary>
        /// 修改界面声音静音状态。
        /// </summary>
        /// <param name="isOn">对应开关是否选中。</param>
        public void OnUISoundMuteChanged(bool isOn)
        {
            GameEntry.Sound.Mute("UISound", !isOn);
            m_UISoundVolumeSlider.gameObject.SetActive(isOn);
        }

        /// <summary>
        /// 修改界面声音音量。
        /// </summary>
        /// <param name="volume">音量，范围为 0 到 1。</param>
        public void OnUISoundVolumeChanged(float volume)
        {
            GameEntry.Sound.SetVolume("UISound", volume);
        }

        /// <summary>
        /// 选中后记录英语选项。
        /// </summary>
        /// <param name="isOn">对应开关是否选中。</param>
        public void OnEnglishSelected(bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            m_SelectedLanguage = Language.English;
        }

        /// <summary>
        /// 选中后记录简体中文选项。
        /// </summary>
        /// <param name="isOn">对应开关是否选中。</param>
        public void OnChineseSimplifiedSelected(bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            m_SelectedLanguage = Language.ChineseSimplified;
        }

        /// <summary>
        /// 选中后记录繁体中文选项。
        /// </summary>
        /// <param name="isOn">对应开关是否选中。</param>
        public void OnChineseTraditionalSelected(bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            m_SelectedLanguage = Language.ChineseTraditional;
        }

        /// <summary>
        /// 选中后记录韩语选项。
        /// </summary>
        /// <param name="isOn">对应开关是否选中。</param>
        public void OnKoreanSelected(bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            m_SelectedLanguage = Language.Korean;
        }

        /// <summary>
        /// 提交设置并关闭界面。
        /// </summary>
        public async void OnSubmitButtonClick()
        {
            if (m_ApplyingLanguage)
            {
                return;
            }
            if (m_SelectedLanguage == GameEntry.Localization.Language)
            {
                Close();
                return;
            }

            m_ApplyingLanguage = true;
            var request = m_LanguageCancellation;
            CanvasGroup interaction = GetComponent<CanvasGroup>();
            interaction.interactable = false;
            Language language = m_SelectedLanguage;
            CancellationToken cancellationToken = request.Token;
            try
            {
                // 字典与字体完整准备后统一提交，已打开的窗口由 Localize 组件刷新。
                await GameEntry.Localization.SetLanguageAsync(language, cancellationToken);
                GameEntry.Setting.SetString(Constant.Setting.Language, language.Code);
                if (!GameEntry.Setting.Save())
                {
                    Log.Warning("Language changed, but the preference could not be saved.");
                }
                Close();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Log.Error("Cannot change language: {0}", exception.Message);
                GameEntry.BuiltinData.OpenDialog(new DialogParams
                {
                    Title = "Language change failed",
                    Message = exception.Message
                });
            }
            finally
            {
                if (ReferenceEquals(m_LanguageCancellation, request))
                {
                    m_ApplyingLanguage = false;
                    interaction.interactable = true;
                }
            }
        }

        /// <summary>
        /// 打开界面并接收业务数据。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            m_LanguageCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            m_ApplyingLanguage = false;
            GetComponent<CanvasGroup>().interactable = true;

            m_MusicMuteToggle.isOn = !GameEntry.Sound.IsMuted("Music");
            m_MusicVolumeSlider.value = GameEntry.Sound.GetVolume("Music");

            m_SoundMuteToggle.isOn = !GameEntry.Sound.IsMuted("Sound");
            m_SoundVolumeSlider.value = GameEntry.Sound.GetVolume("Sound");

            m_UISoundMuteToggle.isOn = !GameEntry.Sound.IsMuted("UISound");
            m_UISoundVolumeSlider.value = GameEntry.Sound.GetVolume("UISound");

            m_SelectedLanguage = GameEntry.Localization.Language;
            m_EnglishToggle.isOn = m_SelectedLanguage == Language.English;
            m_ChineseSimplifiedToggle.isOn = m_SelectedLanguage == Language.ChineseSimplified;
            m_ChineseTraditionalToggle.isOn = m_SelectedLanguage == Language.ChineseTraditional;
            m_KoreanToggle.isOn = m_SelectedLanguage == Language.Korean;
        }

        /// <summary>
        /// 关闭窗口时取消尚未提交的切换，避免池内窗口继续处理旧请求。
        /// </summary>
        /// <param name="isShutdown">是否关闭框架。</param>
        /// <param name="userData">关闭时传入的数据。</param>
        protected override void OnClose(bool isShutdown, object userData)
        {
            GetComponent<CanvasGroup>().interactable = true;
            if (m_LanguageCancellation != null)
            {
                m_LanguageCancellation.Cancel();
                m_LanguageCancellation.Dispose();
                m_LanguageCancellation = null;
            }
            base.OnClose(isShutdown, userData);
        }

    }
}
