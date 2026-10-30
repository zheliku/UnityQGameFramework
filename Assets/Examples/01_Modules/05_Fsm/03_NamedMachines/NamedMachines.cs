using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示用名称管理多台状态机。
    /// </summary>
    public sealed class NamedMachines : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text leftText, rightText, machinesText, resultText;
        [SerializeField]
        private Button destroyButton, recreateButton;
        private IFsm<NamedMachines> left, right;
        private void Start()
        {
            // 同一 owner 可以通过名称持有多台状态机。
            left = QGF.Fsm.CreateFsm("Left", this, new Idle());
            right = QGF.Fsm.CreateFsm("Right", this, new Idle());
            left.Start<Idle>();
            right.Start<Idle>();
            destroyButton.onClick.AddListener(() =>
            {
                bool removed = QGF.Fsm.DestroyFsm<NamedMachines>("Left");
                left = null;
                resultText.text = removed ? "Left destroyed. Right is still running." : "Left does not exist.";
            });
            recreateButton.onClick.AddListener(() =>
            {
                left = QGF.Fsm.CreateFsm("Left", this, new Idle());
                left.Start<Idle>();
                resultText.text = "A new Left was created.";
            });
        }

        private void Update()
        {
            bool hasLeft = QGF.Fsm.HasFsm<NamedMachines>("Left");
            bool hasRight = QGF.Fsm.HasFsm<NamedMachines>("Right");
            leftText.text = hasLeft ? $"Left：{QGF.Fsm.GetFsm<NamedMachines>("Left").CurrentStateTime:F1} s" : "Left: destroyed";
            rightText.text = hasRight ? $"Right：{QGF.Fsm.GetFsm<NamedMachines>("Right").CurrentStateTime:F1} s" : "Right: destroyed";
            machinesText.text = $"Manager contains {QGF.Fsm.Count} state machines.";
            destroyButton.interactable = hasLeft;
            recreateButton.interactable = !hasLeft;
        }

        /// <summary>
        /// 保持空闲的状态。
        /// </summary>
        private sealed class Idle : FsmState<NamedMachines>
        {
        }

        private void OnDestroy()
        {
            var manager = GameEntry.GetComponent<FsmComponent>();
            if (manager == null)
            {
                return;
            }

            if (left != null && !left.IsDestroyed)
            {
                manager.DestroyFsm(left);
            }

            if (right != null && !right.IsDestroyed)
            {
                manager.DestroyFsm(right);
            }
        }
    }
}
