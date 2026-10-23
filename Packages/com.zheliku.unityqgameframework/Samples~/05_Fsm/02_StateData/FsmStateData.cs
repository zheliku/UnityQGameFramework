using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class FsmStateData : MonoBehaviour
    {
        [SerializeField] private InputField jobsInput;
        [SerializeField] private Dropdown writeMode;
        [SerializeField] private Button loadButton, runButton, clearButton;
        [SerializeField] private Text stateText, jobsText, batchText;
        private IFsm<FsmStateData> fsm;
        private bool runRequested;
        private int lastBatch;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                writeMode.ClearOptions();
                writeMode.AddOptions(new System.Collections.Generic.List<string> { "SetData<T>", "SetData(name, Variable)" });
                // 创建时各状态执行 OnInit；Start 才进入 Prepare。
                fsm = QGF.Fsm.CreateFsm(this, new Prepare(), new Execute());
                fsm.Start<Prepare>();
                loadButton.onClick.AddListener(() =>
                {
                    int.TryParse(jobsInput.text, out int count);
                    count = Mathf.Clamp(count, 1, 10);
                    jobsInput.SetTextWithoutNotify(count.ToString());
                    // SetData 接管包装对象；替换或移除时，状态机会归还引用池。
                    if (writeMode.value == 0) fsm.SetData<VarInt32>("Jobs", count);
                    else fsm.SetData("Jobs", (VarInt32)count);
                });
                runButton.onClick.AddListener(() => runRequested = true);
                clearButton.onClick.AddListener(() => fsm.RemoveData("Jobs"));
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (fsm == null || fsm.IsDestroyed) return;
            bool preparing = fsm.CurrentState is Prepare;
            bool hasJobs = fsm.HasData("Jobs");
            stateText.text = preparing ? "准备阶段" : "执行阶段：每秒处理一件";
            jobsText.text = hasJobs ? $"Jobs = {fsm.GetData("Jobs")}" : "Jobs 已移除";
            batchText.text = $"上一批读取到：{lastBatch} 件";
            loadButton.interactable = clearButton.interactable = preparing;
            runButton.interactable = preparing && hasJobs;
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
                // GetData 返回状态机持有的包装对象；修改 Value 会直接更新共享数据。
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
