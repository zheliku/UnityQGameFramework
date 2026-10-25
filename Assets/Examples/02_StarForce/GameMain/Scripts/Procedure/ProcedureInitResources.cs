//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityQGameFramework.Runtime;
using ProcedureOwner = UnityQGameFramework.Core.Fsm.IFsm<UnityQGameFramework.Core.Procedure.IProcedureManager>;

namespace StarForce
{
    /// <summary>
    /// 初始化资源包并加载当前版本清单。
    /// </summary>
    public class ProcedureInitResources : ProcedureBase
    {
        private bool m_Complete;
        private CancellationTokenSource m_Cancellation;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            m_Complete = false;
            StartInitialization();
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            if (m_Complete)
            {
                ChangeState<ProcedureUpdateResources>(procedureOwner);
            }
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            if (m_Cancellation != null && !m_Cancellation.IsCancellationRequested)
            {
                m_Cancellation.Cancel();
            }
            base.OnLeave(procedureOwner, isShutdown);
        }

        private void StartInitialization()
        {
            m_Cancellation = new CancellationTokenSource();
            InitializeAsync(m_Cancellation).Forget();
        }

        private async UniTask InitializeAsync(CancellationTokenSource cancellation)
        {
            CancellationToken cancellationToken = cancellation.Token;
            try
            {
                // ResourceSettings 决定模拟、离线或联机初始化；完成后资源清单已可查询。
                await GameEntry.Resource.InitializePackageAsync(cancellationToken: cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                m_Complete = true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Log.Warning("Resource initialization failed: {0}", exception.Message);
                GameEntry.UI.OpenDialog(new DialogParams
                {
                    Mode = 2,
                    Title = "资源初始化失败",
                    Message = exception.Message,
                    ConfirmText = "重试",
                    OnClickConfirm = userData => StartInitialization(),
                    CancelText = "退出",
                    OnClickCancel = userData => UnityQGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Quit)
                });
            }
            finally
            {
                cancellation.Dispose();
                if (ReferenceEquals(m_Cancellation, cancellation))
                {
                    m_Cancellation = null;
                }
            }
        }
    }
}
