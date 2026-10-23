using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class FsmLifecycle : MonoBehaviour
    {
        [SerializeField] private Text stateText, callbacksText, resultText;
        [SerializeField] private Dropdown createMode, destroyMode;
        [SerializeField] private Button createButton, destroyButton;
        private IFsm<FsmLifecycle> fsm;
        private int initialized, entered, left, destroyed;
        private bool ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                createMode.ClearOptions();
                createMode.AddOptions(new List<string> { "CreateFsm(owner, params)", "CreateFsm(owner, List)" });
                destroyMode.ClearOptions();
                destroyMode.AddOptions(new List<string> { "DestroyFsm<T>", "DestroyFsm(Type)", "DestroyFsm(IFsm)", "DestroyFsm(FsmBase)" });
                createButton.onClick.AddListener(() =>
                {
                    // 创建时 Idle 执行 OnInit；Start 再执行 OnEnter。
                    if (createMode.value == 0) fsm = QGF.Fsm.CreateFsm(this, new Idle());
                    else fsm = QGF.Fsm.CreateFsm(this, new List<FsmState<FsmLifecycle>> { new Idle() });
                    fsm.Start<Idle>();
                    resultText.text = "创建后已执行 OnInit 与 OnEnter";
                });
                destroyButton.onClick.AddListener(() =>
                {
                    bool removed = false;
                    switch (destroyMode.value)
                    {
                        // 销毁状态机会离开当前状态并调用各状态的 OnDestroy。
                        case 0: removed = QGF.Fsm.DestroyFsm<FsmLifecycle>(); break;
                        case 1: removed = QGF.Fsm.DestroyFsm(typeof(FsmLifecycle)); break;
                        case 2: removed = QGF.Fsm.DestroyFsm(fsm); break;
                        case 3: removed = QGF.Fsm.DestroyFsm((FsmBase)fsm); break;
                    }
                    resultText.text = $"销毁请求：{(removed ? "成功" : "未找到状态机")}；"
                        + $"IsDestroyed：{(fsm.IsDestroyed ? "已销毁" : "仍存在")}";
                    // 销毁后清空句柄，状态机和数据已归还引用池。
                    fsm = null;
                });
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!ready) return;
            // 查询这一 owner 类型下的未命名状态机。
            bool exists = QGF.Fsm.HasFsm<FsmLifecycle>();
            stateText.text = exists ? "状态机运行中" : "状态机未创建或已销毁";
            callbacksText.text = $"OnInit：{initialized}\nOnEnter：{entered}\nOnLeave：{left}\nOnDestroy：{destroyed}";
            createButton.interactable = !exists;
            destroyButton.interactable = exists;
        }

        private sealed class Idle : FsmState<FsmLifecycle>
        {
            protected override void OnInit(IFsm<FsmLifecycle> owner) => owner.Owner.initialized++;
            protected override void OnEnter(IFsm<FsmLifecycle> owner) => owner.Owner.entered++;
            protected override void OnLeave(IFsm<FsmLifecycle> owner, bool shutdown) => owner.Owner.left++;
            protected override void OnDestroy(IFsm<FsmLifecycle> owner) => owner.Owner.destroyed++;
        }

        private void OnDestroy()
        {
            var manager = GameEntry.GetComponent<FsmComponent>();
            if (manager != null && fsm != null && !fsm.IsDestroyed) manager.DestroyFsm(fsm);
        }
    }
}
