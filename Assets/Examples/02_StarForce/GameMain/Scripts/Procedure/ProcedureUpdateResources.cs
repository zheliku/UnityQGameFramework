//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityQGameFramework.Runtime;
using YooAsset;
using ProcedureOwner = UnityQGameFramework.Core.Fsm.IFsm<UnityQGameFramework.Core.Procedure.IProcedureManager>;

namespace StarForce
{
    /// <summary>
    /// 确认并下载资源更新。
    /// </summary>
    public class ProcedureUpdateResources : ProcedureBase
    {
        private bool m_Complete;
        private bool m_DownloadedResources;
        private CancellationTokenSource m_Cancellation;
        private ResourceDownloaderOperation m_Downloader;
        private UpdateResourceForm m_Form;
        private long m_PreviousDownloadBytes;
        private float m_PreviousProgressTime;
        private float m_DownloadSpeedKB;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            m_Complete = false;
            m_DownloadedResources = false;
            m_Cancellation = new CancellationTokenSource();
            PrepareDownloadAsync(m_Cancellation.Token).Forget();
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            if (m_Form != null && m_Downloader != null)
            {
                UpdateProgress();
            }
            if (!m_Complete)
            {
                return;
            }

            if (m_DownloadedResources)
            {
                ChangeState<ProcedureClearUnusedCache>(procedureOwner);
            }
            else
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

            if (m_Downloader != null && !m_Downloader.IsDone)
            {
                m_Downloader.CancelDownload();
            }
            m_Downloader = null;
            DestroyProgressForm();
            base.OnLeave(procedureOwner, isShutdown);
        }

        private async UniTask PrepareDownloadAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                // QGF 登记下载操作；下载器只包含当前清单中尚未就绪的资源。
                m_Downloader = GameEntry.Resource.CreateResourceDownloader(new ResourceDownloaderOptions(4, 2));
                if (m_Downloader.TotalDownloadCount == 0)
                {
                    // 空下载器也要完成，避免一直作为在途操作占用资源包。
                    await GameEntry.Resource.DownloadResourcesAsync(m_Downloader, cancellationToken);
                    m_Complete = true;
                    return;
                }

                ShowDownloadPrompt(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                ShowFailure(exception, () => PrepareDownloadAsync(cancellationToken).Forget());
            }
        }

        private void ShowDownloadPrompt(CancellationToken cancellationToken)
        {
            bool isCarrierData = Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork;
            string titleKey = isCarrierData ? "UpdateResourceViaCarrierDataNetwork.Title" : "UpdateResource.Title";
            string messageKey = isCarrierData ? "UpdateResourceViaCarrierDataNetwork.Message" : "UpdateResource.Message";
            string confirmKey = isCarrierData ? "UpdateResourceViaCarrierDataNetwork.UpdateButton" : "UpdateResource.DownloadButton";
            string cancelKey = isCarrierData ? "UpdateResourceViaCarrierDataNetwork.QuitButton" : "UpdateResource.QuitButton";
            float totalSizeMB = m_Downloader.TotalDownloadBytes / 1048576f;

            // 资源表尚未加载，QGF.UI 会显示 Launcher 随场景携带的内置对话框。
            GameEntry.UI.OpenDialog(new DialogParams
            {
                Mode = 2,
                Title = GameEntry.Localization.GetString(titleKey),
                Message = GameEntry.Localization.GetString(messageKey, m_Downloader.TotalDownloadCount, totalSizeMB),
                ConfirmText = GameEntry.Localization.GetString(confirmKey),
                OnClickConfirm = userData => DownloadAsync(cancellationToken).Forget(),
                CancelText = GameEntry.Localization.GetString(cancelKey),
                OnClickCancel = userData => UnityQGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Quit)
            });
        }

        private async UniTask DownloadAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                m_Form = UnityEngine.Object.Instantiate(GameEntry.BuiltinData.UpdateResourceFormTemplate);
                m_PreviousDownloadBytes = 0L;
                m_PreviousProgressTime = Time.realtimeSinceStartup;
                m_DownloadSpeedKB = 0f;

                // QGF 负责等待、失败异常和取消下载，流程只处理界面与下一阶段。
                await GameEntry.Resource.DownloadResourcesAsync(m_Downloader, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                UpdateProgress();
                m_DownloadedResources = true;
                m_Complete = true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                DestroyProgressForm();
                ShowFailure(exception, () => PrepareDownloadAsync(cancellationToken).Forget());
            }
        }

        private void UpdateProgress()
        {
            long currentDownloadBytes = m_Downloader.CurrentDownloadBytes;
            float currentTime = Time.realtimeSinceStartup;
            float elapsedTime = currentTime - m_PreviousProgressTime;
            if (elapsedTime >= 0.5f)
            {
                m_DownloadSpeedKB = (currentDownloadBytes - m_PreviousDownloadBytes) / elapsedTime / 1024f;
                m_PreviousDownloadBytes = currentDownloadBytes;
                m_PreviousProgressTime = currentTime;
            }

            float progress = m_Downloader.TotalDownloadBytes == 0L
                ? 1f
                : (float)currentDownloadBytes / m_Downloader.TotalDownloadBytes;
            string description = GameEntry.Localization.GetString(
                "UpdateResource.Tips",
                m_Downloader.CurrentDownloadCount,
                m_Downloader.TotalDownloadCount,
                currentDownloadBytes / 1048576f,
                m_Downloader.TotalDownloadBytes / 1048576f,
                progress,
                m_DownloadSpeedKB);
            m_Form.SetProgress(progress, description);
        }

        private void ShowFailure(Exception exception, Action retryAction)
        {
            if (m_Downloader != null && !m_Downloader.IsDone)
            {
                m_Downloader.CancelDownload();
            }
            m_Downloader = null;
            DestroyProgressForm();
            Log.Warning("Resource update failed: {0}", exception.Message);
            GameEntry.UI.OpenDialog(new DialogParams
            {
                Mode = 2,
                Title = GameEntry.Localization.GetString("UpdateResource.Title"),
                Message = exception.Message,
                ConfirmText = GameEntry.Localization.GetString("UpdateResource.RetryButton"),
                OnClickConfirm = userData => retryAction(),
                CancelText = GameEntry.Localization.GetString("UpdateResource.QuitButton"),
                OnClickCancel = userData => UnityQGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Quit)
            });
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
