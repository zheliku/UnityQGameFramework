using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class FsmStateData : MonoBehaviour
    {
        [SerializeField] private InputField jobsInput;
        [SerializeField] private Button loadButton, runButton, clearButton;
        [SerializeField] private Text stateText, jobsText, batchText;
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
                    fsm.SetData<VarInt32>("Jobs", Mathf.Clamp(count, 1, 10));
            });
            runButton.onClick.AddListener(() => runRequested = true);
            clearButton.onClick.AddListener(() => fsm.RemoveData("Jobs"));
        }

        private void Update()
        {
            if (fsm == null || fsm.IsDestroyed) return;
            bool preparing = fsm.CurrentState is Prepare;
            stateText.text = preparing ? "准备阶段" : "执行阶段：每秒处理一件";
            jobsText.text = fsm.HasData("Jobs") ? $"Jobs = {fsm.GetData<VarInt32>("Jobs").Value}" : "Jobs 已移除";
            batchText.text = $"上一批读取到：{lastBatch} 件";
            loadButton.interactable = clearButton.interactable = preparing;
            runButton.interactable = preparing && fsm.HasData("Jobs");
        }

        private sealed class Prepare : FsmState<FsmStateData>
        {
            protected override void OnUpdate(IFsm<FsmStateData> owner, float elapsed, float realElapsed)
            {
                if (!owner.Owner.runRequested) return;
                owner.Owner.runRequested = false;
                if (owner.HasData("Jobs")) ChangeState<Execute>(owner);
            }
        }

        private sealed class Execute : FsmState<FsmStateData>
        {
            private float nextJob;
            protected override void OnEnter(IFsm<FsmStateData> owner)
            {
                owner.Owner.lastBatch = owner.GetData<VarInt32>("Jobs").Value;
                nextJob = 1f;
            }
            protected override void OnUpdate(IFsm<FsmStateData> owner, float elapsed, float realElapsed)
            {
                if (owner.CurrentStateTime < nextJob) return;
                nextJob += 1f;
                var jobs = owner.GetData<VarInt32>("Jobs");
                if (--jobs.Value > 0) return;
                owner.RemoveData("Jobs");
                ChangeState<Prepare>(owner);
            }
        }

        private void OnDestroy()
        {
            var manager = GameEntry.GetComponent<FsmComponent>();
            if (manager != null && fsm != null && !fsm.IsDestroyed) manager.DestroyFsm(fsm);
        }
    }
}
