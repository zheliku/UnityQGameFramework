using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示状态机共享数据。
    /// </summary>
    public sealed class FsmStateData : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField jobsInput;
        [SerializeField]
        private Button loadButton, runButton, clearButton;
        [SerializeField]
        private TMP_Text stateText, jobsText, batchText;
        private IFsm<FsmStateData> fsm;
        private bool runRequested;
        private int lastBatch;
        private void Start()
        {
            // 状态数据由状态机持有；这里直接使用泛型入口写入 VarInt32。
            fsm = QGF.Fsm.CreateFsm(this, new Prepare(), new Execute());
            fsm.Start<Prepare>();
            loadButton.onClick.AddListener(() =>
            {
                if (int.TryParse(jobsInput.text, out int count))
                {
                    fsm.SetData<VarInt32>("Jobs", Mathf.Clamp(count, 1, 10));
                }
            });
            runButton.onClick.AddListener(() => runRequested = true);
            clearButton.onClick.AddListener(() => fsm.RemoveData("Jobs"));
        }

        private void Update()
        {
            if (fsm == null || fsm.IsDestroyed)
            {
                return;
            }

            bool preparing = fsm.CurrentState is Prepare;
            stateText.text = preparing ? "Preparation" : "Processing: one item per second";
            jobsText.text = fsm.HasData("Jobs") ? $"Jobs = {fsm.GetData<VarInt32>("Jobs").Value}" : "Jobs removed";
            batchText.text = $"Previous batch read: {lastBatch} items";
            loadButton.interactable = clearButton.interactable = preparing;
            runButton.interactable = preparing && fsm.HasData("Jobs");
        }

        /// <summary>
        /// 准备共享数据的状态。
        /// </summary>
        private sealed class Prepare : FsmState<FsmStateData>
        {
            /// <summary>
            /// 根据状态条件处理请求或切换到下一状态。
            /// </summary>
            /// <param name="owner">当前有限状态机，可通过 Owner 访问示例对象。</param>
            /// <param name="elapsed">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapsed">真实流逝时间，以秒为单位。</param>
            protected override void OnUpdate(IFsm<FsmStateData> owner, float elapsed, float realElapsed)
            {
                if (!owner.Owner.runRequested)
                {
                    return;
                }

                owner.Owner.runRequested = false;
                if (owner.HasData("Jobs"))
                {
                    ChangeState<Execute>(owner);
                }
            }
        }

        /// <summary>
        /// 读取共享数据的状态。
        /// </summary>
        private sealed class Execute : FsmState<FsmStateData>
        {
            private float nextJob;

            /// <summary>
            /// 读取本批任务数量并重置执行计时。
            /// </summary>
            /// <param name="owner">当前有限状态机。</param>
            protected override void OnEnter(IFsm<FsmStateData> owner)
            {
                owner.Owner.lastBatch = owner.GetData<VarInt32>("Jobs").Value;
                nextJob = 1f;
            }

            /// <summary>
            /// 根据状态条件处理请求或切换到下一状态。
            /// </summary>
            /// <param name="owner">当前有限状态机，可通过 Owner 访问示例对象。</param>
            /// <param name="elapsed">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapsed">真实流逝时间，以秒为单位。</param>
            protected override void OnUpdate(IFsm<FsmStateData> owner, float elapsed, float realElapsed)
            {
                if (owner.CurrentStateTime < nextJob)
                {
                    return;
                }

                nextJob += 1f;
                var jobs = owner.GetData<VarInt32>("Jobs");
                if (--jobs.Value > 0)
                {
                    return;
                }

                owner.RemoveData("Jobs");
                ChangeState<Prepare>(owner);
            }
        }

        private void OnDestroy()
        {
            var manager = GameEntry.GetComponent<FsmComponent>();
            if (manager != null && fsm != null && !fsm.IsDestroyed)
            {
                manager.DestroyFsm(fsm);
            }
        }
    }
}
