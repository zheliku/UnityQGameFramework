//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    public class HPBarItem : MonoBehaviour
    {
        private const float AnimationSeconds = 0.3f;
        private const float KeepSeconds = 0.4f;
        private const float FadeOutSeconds = 0.3f;

        [SerializeField]
        private Slider m_HPBar = null;

        private Canvas m_ParentCanvas = null;
        private RectTransform m_CachedTransform = null;
        private CanvasGroup m_CachedCanvasGroup = null;
        private int m_OwnerId = 0;
        private CancellationTokenSource m_AnimationCancellation;

        public Entity Owner { get; private set; }

        public void Init(Entity owner, Canvas parentCanvas, float fromHPRatio, float toHPRatio)
        {
            if (owner == null)
            {
                Log.Error("Owner is invalid.");
                return;
            }

            m_ParentCanvas = parentCanvas;

            gameObject.SetActive(true);
            m_AnimationCancellation?.Cancel();

            m_CachedCanvasGroup.alpha = 1f;
            if (Owner != owner || m_OwnerId != owner.Id)
            {
                m_HPBar.value = fromHPRatio;
                Owner = owner;
                m_OwnerId = owner.Id;
            }

            Refresh();

            Animate(toHPRatio).Forget();
        }

        public bool Refresh()
        {
            if (m_CachedCanvasGroup.alpha <= 0f)
            {
                return false;
            }

            if (Owner != null && Owner.Available && Owner.Id == m_OwnerId)
            {
                Vector3 worldPosition = Owner.CachedTransform.position + Vector3.forward;
                Vector3 screenPosition = GameEntry.Scene.MainCamera.WorldToScreenPoint(worldPosition);

                Vector2 position;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)m_ParentCanvas.transform, screenPosition,
                    m_ParentCanvas.worldCamera, out position))
                {
                    m_CachedTransform.localPosition = position;
                }
            }

            return true;
        }

        public void Reset()
        {
            m_AnimationCancellation?.Cancel();
            m_CachedCanvasGroup.alpha = 1f;
            m_HPBar.value = 1f;
            Owner = null;
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            m_CachedTransform = GetComponent<RectTransform>();
            if (m_CachedTransform == null)
            {
                Log.Error("RectTransform is invalid.");
                return;
            }

            m_CachedCanvasGroup = GetComponent<CanvasGroup>();
            if (m_CachedCanvasGroup == null)
            {
                Log.Error("CanvasGroup is invalid.");
                return;
            }
        }

        private void OnDisable() => m_AnimationCancellation?.Cancel();

        private async UniTask Animate(float value)
        {
            // 回池、销毁或再次受伤时终止旧动画，避免它继续修改复用后的血条。
            var source = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            m_AnimationCancellation = source;
            var cancellation = source.Token;
            try
            {
                await m_HPBar.SmoothValue(value, AnimationSeconds, cancellation);
                await UniTask.Delay(TimeSpan.FromSeconds(KeepSeconds), ignoreTimeScale: true, cancellationToken: cancellation);
                await m_CachedCanvasGroup.FadeToAlpha(0f, FadeOutSeconds, cancellation);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
            finally
            {
                if (ReferenceEquals(m_AnimationCancellation, source)) m_AnimationCancellation = null;
                source.Dispose();
            }
        }
    }
}
