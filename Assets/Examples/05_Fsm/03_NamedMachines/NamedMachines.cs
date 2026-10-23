using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class NamedMachines : MonoBehaviour
    {
        [SerializeField] private Text leftText, rightText, machinesText, resultText;
        [SerializeField] private Button destroyButton, recreateButton;
        [SerializeField] private Dropdown destroyMode, queryMode;
        private IFsm<NamedMachines> left, right;
        private readonly List<FsmBase> machines = new List<FsmBase>();
        private bool ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                destroyMode.ClearOptions();
                destroyMode.AddOptions(new List<string> { "按泛型与名称", "按 Type 与名称", "按 IFsm 引用", "按 FsmBase 引用" });
                queryMode.ClearOptions();
                queryMode.AddOptions(new List<string> { "GetAllFsms 返回数组", "GetAllFsms 写入 List" });
                // 名称相同但 owner 类型不同也可共存；本示例的两台使用同一类型和不同名称。
                left = QGF.Fsm.CreateFsm("Left", this, new Idle());
                right = QGF.Fsm.CreateFsm("Right", this, new List<FsmState<NamedMachines>> { new Idle() });
                left.Start<Idle>();
                right.Start<Idle>();
                destroyButton.onClick.AddListener(() =>
                {
                    var machine = (FsmBase)left;
                    bool removed = false;
                    switch (destroyMode.value)
                    {
                        // 销毁状态机会离开当前状态并调用各状态的 OnDestroy。
                        case 0: removed = QGF.Fsm.DestroyFsm<NamedMachines>("Left"); break;
                        // Type 重载可直接使用查询结果中的 owner 类型与名称。
                        case 1: removed = QGF.Fsm.DestroyFsm(machine.OwnerType, machine.Name); break;
                        case 2: removed = QGF.Fsm.DestroyFsm(left); break;
                        case 3: removed = QGF.Fsm.DestroyFsm(machine); break;
                    }
                    // 销毁返回后立即查看标记，随后清空引用；状态机已回池，之后可能被复用。
                    resultText.text = removed ? "Left：已销毁；Right：继续运行" : "Left：未找到；Right：继续运行";
                    left = null;
                });
                recreateButton.onClick.AddListener(() =>
                {
                    left = QGF.Fsm.CreateFsm("Left", this, new Idle());
                    left.Start<Idle>();
                    resultText.text = "新的 Left 从零开始计时";
                });
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!ready) return;
            // 同一 owner 类型还可按名称区分多个状态机。
            bool hasLeft = QGF.Fsm.HasFsm(typeof(NamedMachines), "Left");
            bool hasRight = QGF.Fsm.HasFsm<NamedMachines>("Right");
            var leftFsm = QGF.Fsm.GetFsm<NamedMachines>("Left");
            var rightFsm = QGF.Fsm.GetFsm(typeof(NamedMachines), "Right");
            leftText.text = hasLeft ? $"Left：运行 {leftFsm.CurrentStateTime:F1} 秒" : "Left：已销毁";
            rightText.text = hasRight ? $"Right：运行 {rightFsm.CurrentStateTime:F1} 秒" : "Right：已销毁";
            machines.Clear();
            if (queryMode.value == 0) machines.AddRange(QGF.Fsm.GetAllFsms());
            else QGF.Fsm.GetAllFsms(machines);
            machinesText.text = $"管理器当前有 {QGF.Fsm.Count} 台：";
            // GetAllFsms 返回非泛型基类，可统一查看不同 owner 类型的状态机。
            foreach (var machine in machines)
                machinesText.text += $"\n{machine.FullName}\n状态：{machine.CurrentStateName}    注册数：{machine.FsmStateCount}    "
                    + $"运行：{(machine.IsRunning ? "运行中" : "已停止")}";
            destroyButton.interactable = hasLeft;
            recreateButton.interactable = !hasLeft;
        }

        private sealed class Idle : FsmState<NamedMachines> { }

        private void OnDestroy()
        {
            var manager = GameEntry.GetComponent<FsmComponent>();
            if (manager == null) return;
            if (left != null && !left.IsDestroyed) manager.DestroyFsm(left);
            if (right != null && !right.IsDestroyed) manager.DestroyFsm(right);
        }
    }
}
