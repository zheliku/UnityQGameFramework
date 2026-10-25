using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityQGameFramework.Core;
using YooAsset;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 资源异步操作。
    /// </summary>
    internal static class ResourceOperation
    {
        /// <summary>
        /// 等待资源操作完成。
        /// </summary>
        /// <param name="operation">资源操作。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>等待任务。</returns>
        internal static async UniTask WaitAsync(AsyncOperationBase operation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            while (!operation.IsDone)
            {
                await UniTask.NextFrame(cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (operation.Status != EOperationStatus.Succeeded)
            {
                throw new FrameworkException(operation.Error);
            }
        }

        /// <summary>
        /// 等待 UniTask，并在返回前释放本次取消注册。
        /// </summary>
        /// <typeparam name="T">任务结果的类型。</typeparam>
        /// <param name="task">要等待的任务。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>原任务的结果。</returns>
        internal static async UniTask<T> WithCancellation<T>(UniTask<T> task, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!cancellationToken.CanBeCanceled)
            {
                return await task;
            }

            var waiter = new CancellationWaiter<T>(cancellationToken);
            waiter.Register();
            CompleteWaitAsync(task, waiter).Forget();
            return await waiter.Task;
        }

        /// <summary>
        /// 等待 UniTask，并在返回前释放本次取消注册。
        /// </summary>
        /// <param name="task">要等待的任务。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>等待任务。</returns>
        internal static async UniTask WithCancellation(UniTask task, CancellationToken cancellationToken)
        {
            await WithCancellation(AsResult(task), cancellationToken);
        }

        private static async UniTask<bool> AsResult(UniTask task)
        {
            await task;
            return true;
        }

        private static async UniTask CompleteWaitAsync<T>(UniTask<T> task, CancellationWaiter<T> waiter)
        {
            try
            {
                waiter.TrySetResult(await task);
            }
            catch (OperationCanceledException exception)
            {
                waiter.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                waiter.TrySetException(exception);
            }
        }

        /// <summary>
        /// 等待资产加载，失败或取消时释放本次句柄。
        /// </summary>
        /// <param name="handle">本次加载持有的资产句柄。</param>
        /// <param name="cancellationToken">取消标记。</param>
        /// <returns>等待任务。</returns>
        internal static async UniTask WaitAssetAsync(AssetHandle handle, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (!handle.IsDone)
                {
                    await UniTask.NextFrame(cancellationToken);
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (!handle.IsValid || handle.Status != EOperationStatus.Succeeded)
                {
                    throw new FrameworkException(handle.IsValid ? handle.Error : "The resource handle was released during loading.");
                }
            }
            catch
            {
                if (handle.IsValid)
                {
                    handle.Release();
                }
                throw;
            }
        }

        private sealed class CancellationWaiter<T>
        {
            private readonly object m_Lock = new object();
            private readonly CancellationToken m_CancellationToken;
            private readonly UniTaskCompletionSource<T> m_Completion = new UniTaskCompletionSource<T>();
            private CancellationTokenRegistration m_Registration;
            private bool m_Registered;
            private bool m_Cancelled;
            private bool m_Completed;

            internal CancellationWaiter(CancellationToken cancellationToken)
            {
                m_CancellationToken = cancellationToken;
            }

            internal UniTask<T> Task
            {
                get
                {
                    return m_Completion.Task;
                }
            }

            internal void Register()
            {
                CancellationTokenRegistration registration = m_CancellationToken.Register(OnCancelled);
                bool cancel;
                lock (m_Lock)
                {
                    m_Registration = registration;
                    m_Registered = true;
                    cancel = m_Cancelled;
                }
                if (cancel)
                {
                    TrySetCanceled(m_CancellationToken);
                }
            }

            internal void TrySetResult(T result)
            {
                CancellationTokenRegistration registration;
                lock (m_Lock)
                {
                    if (m_Completed)
                    {
                        return;
                    }
                    m_Completed = true;
                    registration = m_Registration;
                }
                registration.Dispose();
                m_Completion.TrySetResult(result);
            }

            internal void TrySetCanceled(CancellationToken cancellationToken)
            {
                CancellationTokenRegistration registration;
                lock (m_Lock)
                {
                    if (m_Completed)
                    {
                        return;
                    }
                    m_Completed = true;
                    registration = m_Registration;
                }
                registration.Dispose();
                m_Completion.TrySetCanceled(cancellationToken);
            }

            internal void TrySetException(Exception exception)
            {
                CancellationTokenRegistration registration;
                lock (m_Lock)
                {
                    if (m_Completed)
                    {
                        return;
                    }
                    m_Completed = true;
                    registration = m_Registration;
                }
                registration.Dispose();
                m_Completion.TrySetException(exception);
            }

            private void OnCancelled()
            {
                lock (m_Lock)
                {
                    m_Cancelled = true;
                    if (!m_Registered)
                    {
                        return;
                    }
                }
                TrySetCanceled(m_CancellationToken);
            }
        }
    }
}
