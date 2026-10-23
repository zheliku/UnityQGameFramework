using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ChangeProcedureExample : MonoBehaviour
    {
        [SerializeField] private Text currentText, enteredText;
        [SerializeField] private Button enterButton, backButton;
        internal bool EnterRequested, BackRequested;
        internal int MenuVisits, PlayVisits;

        private void Start()
        {
            enterButton.onClick.AddListener(() => EnterRequested = true);
            backButton.onClick.AddListener(() => BackRequested = true);
        }

        private void Update()
        {
            // ProcedureComponent 驱动流程状态机；当前流程由它持有。
            var current = QGF.Procedure.CurrentProcedure;
            currentText.text = $"启动 → 菜单 ⇄ 游玩\n当前：{current?.GetType().Name}\n停留：{QGF.Procedure.CurrentProcedureTime:F1} 秒";
            enteredText.text = $"进入菜单：{MenuVisits} 次    进入游玩：{PlayVisits} 次";
            enterButton.interactable = current is ExampleMenu02;
            backButton.interactable = current is ExamplePlay02;
        }
    }

    public sealed class ExampleLaunch02 : ProcedureBase
    {
        protected override void OnUpdate(IFsm<IProcedureManager> owner, float elapsed, float realElapsed)
        {
            base.OnUpdate(owner, elapsed, realElapsed);
            if (owner.CurrentStateTime >= 0.5f) ChangeState<ExampleMenu02>(owner);
        }
    }

    public sealed class ExampleMenu02 : ProcedureBase
    {
        private ChangeProcedureExample view;
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            view = Object.FindFirstObjectByType<ChangeProcedureExample>();
            view.EnterRequested = false;
            view.MenuVisits++;
        }
        protected override void OnUpdate(IFsm<IProcedureManager> owner, float elapsed, float realElapsed)
        {
            base.OnUpdate(owner, elapsed, realElapsed);
            // 按钮只提出请求，流程自己决定什么时候转换。
            if (view.EnterRequested) ChangeState<ExamplePlay02>(owner);
        }
    }

    public sealed class ExamplePlay02 : ProcedureBase
    {
        private ChangeProcedureExample view;
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            view = Object.FindFirstObjectByType<ChangeProcedureExample>();
            view.BackRequested = false;
            view.PlayVisits++;
        }
        protected override void OnUpdate(IFsm<IProcedureManager> owner, float elapsed, float realElapsed)
        {
            base.OnUpdate(owner, elapsed, realElapsed);
            if (view.BackRequested) ChangeState(owner, typeof(ExampleMenu02));
        }
    }
}
