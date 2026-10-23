using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    // 手动调用 Core 的初始化和启动接口。
    public sealed class CoreProcedureStart : MonoBehaviour
    {
        [SerializeField] private Dropdown startMode;
        [SerializeField] private Button initializeButton, startButton;
        [SerializeField] private Text stateText, queryText;
        private IProcedureManager manager;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                startMode.ClearOptions();
                startMode.AddOptions(new List<string> { "StartProcedure<T>", "StartProcedure(Type)" });
                initializeButton.onClick.AddListener(() =>
                {
                    // GetModule 取得框架管理器；这个场景不放自动启动流程的 Procedure 组件。
                    manager = FrameworkEntry.GetModule<IProcedureManager>();
                    // 先注册全部流程及 FSM 管理器；Initialize 本身不会进入入口流程。
                    manager.Initialize(FrameworkEntry.GetModule<IFsmManager>(), new CoreLaunch05(), new CoreReady05());
                });
                startButton.onClick.AddListener(() =>
                {
                    // StartProcedure 进入已注册的首个流程，后续切换由流程状态决定。
                    if (startMode.value == 0) manager.StartProcedure<CoreLaunch05>();
                    else manager.StartProcedure(typeof(CoreLaunch05));
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            bool initialized = manager != null;
            bool running = initialized && manager.CurrentProcedure != null;
            initializeButton.interactable = !initialized;
            startButton.interactable = initialized && !running;
            startMode.interactable = !running;
            stateText.text = !initialized ? "尚未初始化" : !running ? "状态已注册，尚未启动"
                : $"当前：{manager.CurrentProcedure.GetType().Name}\n停留：{manager.CurrentProcedureTime:F1} 秒";
            if (initialized)
            {
                bool hasLaunch = manager.HasProcedure<CoreLaunch05>();
                bool hasReady = manager.HasProcedure(typeof(CoreReady05));
                var launch = hasLaunch ? manager.GetProcedure<CoreLaunch05>() : null;
                var ready = hasReady ? manager.GetProcedure(typeof(CoreReady05)) : null;
                queryText.text = $"CoreLaunch：{(hasLaunch ? "已注册" : "未注册")} / {launch?.GetType().Name ?? "无实例"}\n"
                    + $"CoreReady：{(hasReady ? "已注册" : "未注册")} / {ready?.GetType().Name ?? "无实例"}";
            }
        }
    }

    public sealed class CoreLaunch05 : ProcedureBase
    {
        protected override void OnUpdate(IFsm<IProcedureManager> owner, float elapsed, float realElapsed)
        {
            base.OnUpdate(owner, elapsed, realElapsed);
            if (owner.CurrentStateTime >= 1f) ChangeState<CoreReady05>(owner);
        }
    }
    public sealed class CoreReady05 : ProcedureBase { }
}
