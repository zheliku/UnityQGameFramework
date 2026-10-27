//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 提供界面淡入淡出、字体和深度处理。
    /// </summary>
    public abstract class UGuiForm : UIFormLogic
    {
        /// <summary>
        /// 界面深度换算为 Canvas 排序值的倍率。
        /// </summary>
        public const int DepthFactor = 100;
        private const float FadeTime = 0.3f;
        private static Font s_MainFont = null;
        private Canvas m_CachedCanvas = null;
        private CanvasGroup m_CanvasGroup = null;
        private List<Canvas> m_CachedCanvasContainer = new List<Canvas>();
        private CancellationTokenSource m_FadeCancellation;

        /// <summary>
        /// 获取预制体原始的 Canvas 排序值。
        /// </summary>
        public int OriginalDepth { get; private set; }

        /// <summary>
        /// 获取当前界面深度。
        /// </summary>
        public int Depth
        {
            get
            {
                return m_CachedCanvas.sortingOrder;
            }
        }

        /// <summary>
        /// 关闭界面，可选择跳过淡出。
        /// </summary>
        public void Close()
        {
            Close(false);
        }

        /// <summary>
        /// 关闭界面，可选择跳过淡出。
        /// </summary>
        /// <param name="ignoreFade">是否跳过淡出并立即关闭。</param>
        public void Close(bool ignoreFade)
        {
            m_FadeCancellation?.Cancel();
            if (ignoreFade)
            {
                GameEntry.UI.CloseUIForm(this);
            }
            else
            {
                Fade(0f, true).Forget();
            }
        }

        /// <summary>
        /// 按配置播放二维界面声音。
        /// </summary>
        /// <param name="uiSoundId">界面声音配置编号。</param>
        public void PlayUISound(int uiSoundId)
        {
            GameEntry.Sound.PlayUISound(uiSoundId);
        }

        /// <summary>
        /// 替换当前界面文本使用的字体。
        /// </summary>
        /// <param name="mainFont">界面使用的字体。</param>
        public static void SetMainFont(Font mainFont)
        {
            if (mainFont == null)
            {
                Log.Error("Main font is invalid.");
                return;
            }

            s_MainFont = mainFont;
        }

        /// <summary>
        /// 初始化实例。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_CachedCanvas = gameObject.GetOrAddComponent<Canvas>();
            m_CachedCanvas.overrideSorting = true;
            OriginalDepth = m_CachedCanvas.sortingOrder;
            m_CanvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
            RectTransform transform = GetComponent<RectTransform>();
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = Vector2.zero;
            gameObject.GetOrAddComponent<GraphicRaycaster>();
            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i].font = s_MainFont;
                if (!string.IsNullOrEmpty(texts[i].text))
                {
                    texts[i].text = GameEntry.Localization.GetString(texts[i].text);
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
            m_CanvasGroup.alpha = 0f;
            Fade(1f).Forget();
        }

        /// <summary>
        /// 处理界面恢复。
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            m_CanvasGroup.alpha = 0f;
            Fade(1f).Forget();
        }

        /// <summary>
        /// 根据界面深度调整显示顺序。
        /// </summary>
        /// <param name="uiGroupDepth">界面组深度。</param>
        /// <param name="depthInUIGroup">界面在界面组中的深度。</param>
        protected override void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
        {
            int oldDepth = Depth;
            base.OnDepthChanged(uiGroupDepth, depthInUIGroup);
            int deltaDepth = UGuiGroupHelper.DepthFactor * uiGroupDepth + DepthFactor * depthInUIGroup - oldDepth + OriginalDepth;
            GetComponentsInChildren(true, m_CachedCanvasContainer);
            for (int i = 0; i < m_CachedCanvasContainer.Count; i++)
            {
                m_CachedCanvasContainer[i].sortingOrder += deltaDepth;
            }

            m_CachedCanvasContainer.Clear();
        }

        /// <summary>
        /// 停止已隐藏界面的淡入淡出动画。
        /// </summary>
        protected virtual void OnDisable()
        {
            m_FadeCancellation?.Cancel();
        }

        private async UniTask Fade(float alpha, bool close = false)
        {
            m_FadeCancellation?.Cancel();
            var source = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            m_FadeCancellation = source;
            var cancellation = source.Token;
            try
            {
                await m_CanvasGroup.FadeToAlpha(alpha, FadeTime, cancellation);
                if (close && Available)
                {
                    GameEntry.UI.CloseUIForm(this);
                }
            }
            catch (OperationCanceledException)when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
            finally
            {
                if (ReferenceEquals(m_FadeCancellation, source))
                {
                    m_FadeCancellation = null;
                }

                source.Dispose();
            }
        }
    }
}
