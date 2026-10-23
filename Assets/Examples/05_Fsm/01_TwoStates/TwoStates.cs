using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class TwoStates : MonoBehaviour
    {
        [SerializeField] private Text stateText, timeText, completedText;
        [SerializeField] private Slider progress;
        [SerializeField] private Button workButton;
        [SerializeField] private Toggle automatic;
        private IFsm<TwoStates> fsm;
        private bool workRequested;
        private int completed;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // 创建时各状态执行 OnInit；同一 owner 类型和名称对应一台状态机。
                fsm = QGF.Fsm.CreateFsm(this, new Waiting(), new Working());
                // Start 执行 Waiting.OnEnter，并开始更新当前状态。
                fsm.Start<Waiting>();
                workButton.onClick.AddListener(() => workRequested = true);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (fsm == null || fsm.IsDestroyed) return;
            bool working = fsm.CurrentState is Working;
            stateText.text = working ? "当前状态：工作" : "当前状态：等待";
            timeText.text = $"停留 {fsm.CurrentStateTime:F1} 秒";
            progress.value = working ? Mathf.Clamp01(fsm.CurrentStateTime / 2f) * 100 : 0;
            completedText.text = $"已完成：{completed} 件";
            workButton.interactable = !working;
        }

        private sealed class Waiting : FsmState<TwoStates>
        {
            protected override void OnUpdate(IFsm<TwoStates> owner, float elapsed, float realElapsed)
            {
                if (!owner.Owner.workRequested && !owner.Owner.automatic.isOn) return;
                owner.Owner.workRequested = false;
                ChangeState<Working>(owner);
            }
        }

        private sealed class Working : FsmState<TwoStates>
        {
            protected override void OnUpdate(IFsm<TwoStates> owner, float elapsed, float realElapsed)
            {
                // 状态机由框架统一更新。两秒后计件，再回到等待。
                if (owner.CurrentStateTime < 2f) return;
                owner.Owner.completed++;
                ChangeState<Waiting>(owner);
            }
        }

        private void OnDestroy()
        {
            var manager = GameEntry.GetComponent<FsmComponent>();
            if (manager != null && fsm != null && !fsm.IsDestroyed) manager.DestroyFsm(fsm);
        }
    }
}
