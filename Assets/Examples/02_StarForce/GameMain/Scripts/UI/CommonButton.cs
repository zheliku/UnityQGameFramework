//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace StarForce
{
    /// <summary>
    /// 播放按钮指针交互的声音和动画。
    /// </summary>
    public class CommonButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float FadeTime = 0.3f;
        private const float OnHoverAlpha = 0.7f;
        private const float OnClickAlpha = 0.6f;
        [SerializeField]
        private UnityEvent m_OnHover = null;
        [SerializeField]
        private UnityEvent m_OnClick = null;
        private CanvasGroup m_CanvasGroup = null;
        private CancellationTokenSource m_FadeCancellation;
        private void Awake()
        {
            m_CanvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
        }

        private void OnDisable()
        {
            m_FadeCancellation?.Cancel();
            m_CanvasGroup.alpha = 1f;
        }

        /// <summary>
        /// 处理指针进入按钮。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Fade(OnHoverAlpha).Forget();
            m_OnHover.Invoke();
        }

        /// <summary>
        /// 处理指针离开按钮。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Fade(1f).Forget();
        }

        /// <summary>
        /// 处理指针按下按钮。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            m_FadeCancellation?.Cancel();
            m_CanvasGroup.alpha = OnClickAlpha;
            m_OnClick.Invoke();
        }

        /// <summary>
        /// 处理指针松开按钮。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            m_CanvasGroup.alpha = OnHoverAlpha;
        }

        private async UniTask Fade(float alpha)
        {
            m_FadeCancellation?.Cancel();
            var source = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            m_FadeCancellation = source;
            var cancellation = source.Token;
            try
            {
                await m_CanvasGroup.FadeToAlpha(alpha, FadeTime, cancellation);
            }
            catch (OperationCanceledException)when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                UnityQGameFramework.Runtime.Log.Error(exception);
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
