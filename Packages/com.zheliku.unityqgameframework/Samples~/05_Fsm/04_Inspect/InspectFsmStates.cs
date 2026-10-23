using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class InspectFsmStates : MonoBehaviour
    {
        [SerializeField] private Text stateText, statesText, lookupText;
        [SerializeField] private Dropdown startMode, listMode;
        [SerializeField] private Button startButton, walkButton, inspectButton;
        private IFsm<InspectFsmStates> fsm;
        private bool walkRequested;
        private readonly List<FsmState<InspectFsmStates>> states = new List<FsmState<InspectFsmStates>>();

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                startMode.ClearOptions();
                startMode.AddOptions(new List<string> { "Start<TState>", "Start(Type)" });
                listMode.ClearOptions();
                listMode.AddOptions(new List<string> { "GetAllStates 数组", "GetAllStates List" });
                // 创建时各状态执行 OnInit；Start 才进入首个状态。
                fsm = QGF.Fsm.CreateFsm(this, new Idle(), new Walk());
                startButton.onClick.AddListener(() =>
                {
                    if (startMode.value == 0) fsm.Start<Idle>();
                    else fsm.Start(typeof(Idle));
                });
                walkButton.onClick.AddListener(() => walkRequested = true);
                inspectButton.onClick.AddListener(() =>
                {
                    // GetState 返回已注册的状态实例。
                    bool idleExists = fsm.HasState<Idle>();
                    bool walkExists = fsm.HasState(typeof(Walk));
                    var idle = fsm.GetState<Idle>();
                    var walk = fsm.GetState(typeof(Walk));
                    states.Clear();
                    if (listMode.value == 0) states.AddRange(fsm.GetAllStates());
                    else fsm.GetAllStates(states);
                    statesText.text = "已注册的状态：";
                    foreach (var state in states) statesText.text += "\n" + state.GetType().Name;
                    lookupText.text = $"Idle：{(idleExists ? "已注册" : "未注册")} / {idle.GetType().Name}\n"
                        + $"Walk：{(walkExists ? "已注册" : "未注册")} / {walk.GetType().Name}\n"
                        + $"HasFsm 泛型/Type：{(QGF.Fsm.HasFsm<InspectFsmStates>() ? "存在" : "不存在")} / "
                        + $"{(QGF.Fsm.HasFsm(typeof(InspectFsmStates)) ? "存在" : "不存在")}\n"
                        + $"GetFsm 两个入口：{(ReferenceEquals(QGF.Fsm.GetFsm<InspectFsmStates>(), QGF.Fsm.GetFsm(typeof(InspectFsmStates))) ? "同一实例" : "不同实例")}";
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (fsm == null || fsm.IsDestroyed) return;
            stateText.text = $"{fsm.FullName}\n名称：{(fsm.Name.Length == 0 ? "未命名" : fsm.Name)}    "
                + $"Owner：{(ReferenceEquals(fsm.Owner, this) ? "当前组件" : "其他对象")}\n"
                + $"状态数：{fsm.FsmStateCount}    运行：{(fsm.IsRunning ? "运行中" : "已停止")}\n"
                + $"当前：{fsm.CurrentState?.GetType().Name ?? "尚未启动"}    停留 {fsm.CurrentStateTime:F1} 秒";
            startButton.interactable = !fsm.IsRunning;
            walkButton.interactable = fsm.CurrentState is Idle;
        }

        private sealed class Idle : FsmState<InspectFsmStates>
        {
            protected override void OnUpdate(IFsm<InspectFsmStates> owner, float elapsed, float realElapsed)
            {
                if (owner.Owner.walkRequested) ChangeState(owner, typeof(Walk));
            }
        }
        private sealed class Walk : FsmState<InspectFsmStates> { }

        private void OnDestroy()
        {
            var manager = GameEntry.GetComponent<FsmComponent>();
            if (manager != null && fsm != null && !fsm.IsDestroyed) manager.DestroyFsm(fsm);
        }
    }
}
