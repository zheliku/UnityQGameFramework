//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityQGameFramework.Runtime;
using YooAsset;
using ProcedureOwner = UnityQGameFramework.Core.Fsm.IFsm<UnityQGameFramework.Core.Procedure.IProcedureManager>;

namespace StarForce
{
    /// <summary>
    /// 清理资源更新后未使用的 Bundle 缓存。
    /// </summary>
    public class ProcedureClearUnusedCache : ProcedureBase
    {
        private bool m_Complete;
        private CancellationTokenSource m_Cancellation;
        private UpdateResourceForm m_Form;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            m_Complete = false;
            m_Cancellation = new CancellationTokenSource();
            ClearCacheAsync(m_Cancellation.Token).Forget();
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            if (m_Complete)
            {
                ChangeState<ProcedurePreload>(procedureOwner);
            }
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            if (m_Cancellation != null)
            {
                if (!m_Cancellation.IsCancellationRequested)
                {
                    m_Cancellation.Cancel();
                }
                m_Cancellation.Dispose();
                m_Cancellation = null;
            }
            DestroyProgressForm();
            base.OnLeave(procedureOwner, isShutdown);
        }

        private async UniTask ClearCacheAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (m_Form == null)
                {
                    m_Form = UnityEngine.Object.Instantiate(GameEntry.BuiltinData.UpdateResourceFormTemplate);
                }
                m_Form.SetProgress(0f, GameEntry.Localization.GetString("UpdateResource.Cleaning"));

                // 只删除当前清单不再引用的缓存 Bundle，保留仍被本包使用的资源。
                await GameEntry.Resource.ClearCacheAsync(
                    ClearCacheMethods.ClearUnusedBundleFiles,
                    cancellationToken: cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                m_Complete = true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Log.Warning("Clear unused resource cache failed: {0}", exception.Message);
                GameEntry.UI.OpenDialog(new DialogParams
                {
                    Mode = 2,
                    Title = GameEntry.Localization.GetString("UpdateResource.Title"),
                    Message = exception.Message,
                    ConfirmText = GameEntry.Localization.GetString("UpdateResource.RetryButton"),
                    OnClickConfirm = userData => ClearCacheAsync(cancellationToken).Forget(),
                    CancelText = GameEntry.Localization.GetString("UpdateResource.QuitButton"),
                    OnClickCancel = userData => UnityQGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Quit)
                });
            }
        }

        private void DestroyProgressForm()
        {
            if (m_Form == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(m_Form.gameObject);
            m_Form = null;
        }
    }
}
