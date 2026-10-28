//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityQGameFramework.Runtime;
using ProcedureOwner = UnityQGameFramework.Core.IFsm<UnityQGameFramework.Core.IProcedureManager>;

namespace StarForce
{
    /// <summary>
    /// 初始化资源包并加载当前版本清单。
    /// </summary>
    public class ProcedureInitResources : ProcedureBase
    {
        private bool m_Complete;
        private CancellationTokenSource m_Cancellation;

        /// <summary>
        /// 进入状态时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            m_Complete = false;
            StartInitialization();
        }

        /// <summary>
        /// 更新当前流程。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            if (m_Complete)
            {
                ChangeState<ProcedureUpdateResources>(procedureOwner);
            }
        }

        /// <summary>
        /// 离开状态时调用。
        /// </summary>
        /// <param name="procedureOwner">流程持有者。</param>
        /// <param name="isShutdown">是否是关闭状态机时触发。</param>
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
                    Title = "Resource initialization failed",
                    Message = exception.Message,
                    ConfirmText = "Retry",
                    OnClickConfirm = userData => StartInitialization(),
                    CancelText = "Quit",
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
