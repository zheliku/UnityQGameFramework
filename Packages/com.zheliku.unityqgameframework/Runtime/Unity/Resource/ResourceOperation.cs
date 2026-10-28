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
                throw new GameFrameworkException(operation.Error);
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

        /// <summary>
        /// 将无返回值任务转换为布尔结果任务。
        /// </summary>
        /// <param name="task">要等待的任务。</param>
        /// <returns>原任务完成后返回 true 的任务。</returns>
        private static async UniTask<bool> AsResult(UniTask task)
        {
            await task;
            return true;
        }

        /// <summary>
        /// 观察原任务并向等待器传递完成结果。
        /// </summary>
        /// <typeparam name="T">原任务的结果类型。</typeparam>
        /// <param name="task">原始任务。</param>
        /// <param name="waiter">接收结果、取消或异常的等待器。</param>
        /// <returns>观察原任务完成的异步任务。</returns>
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
                    throw new GameFrameworkException(handle.IsValid ? handle.Error : "The resource handle was released during loading.");
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

        /// <summary>
        /// 初始化支持独立取消的等待器。
        /// </summary>
        /// <typeparam name="T">资源操作结果类型。</typeparam>
        private sealed class CancellationWaiter<T>
        {
            private readonly object m_Lock = new object();
            private readonly CancellationToken m_CancellationToken;
            private readonly UniTaskCompletionSource<T> m_Completion = new UniTaskCompletionSource<T>();
            private CancellationTokenRegistration m_Registration;
            private bool m_Registered;
            private bool m_Cancelled;
            private bool m_Completed;

            /// <summary>
            /// 初始化支持独立取消的等待器。
            /// </summary>
            /// <param name="cancellationToken">取消异步操作的令牌。</param>
            internal CancellationWaiter(CancellationToken cancellationToken)
            {
                m_CancellationToken = cancellationToken;
            }

            /// <summary>
            /// 获取支持独立取消的等待任务。
            /// </summary>
            internal UniTask<T> Task
            {
                get
                {
                    return m_Completion.Task;
                }
            }

            /// <summary>
            /// 注册取消回调并处理登记期间发生的取消。
            /// </summary>
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

            /// <summary>
            /// 尝试完成等待并释放取消登记。
            /// </summary>
            /// <param name="result">原任务的结果。</param>
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

            /// <summary>
            /// 尝试将等待标记为取消并释放取消登记。
            /// </summary>
            /// <param name="cancellationToken">取消异步操作的令牌。</param>
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

            /// <summary>
            /// 尝试将等待标记为失败并释放取消登记。
            /// </summary>
            /// <param name="exception">原任务发生的异常。</param>
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

            /// <summary>
            /// 记录取消状态并尝试结束等待。
            /// </summary>
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
