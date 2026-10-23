using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ProcedureDataExample : MonoBehaviour
    {
        [SerializeField] internal Dropdown levelChoice;
        [SerializeField] private Button confirmButton, finishButton;
        [SerializeField] private Text stateText, dataText;
        internal bool ConfirmRequested, FinishRequested;
        internal string LoadedLevel = "尚未选择关卡";
        internal bool HasLevelData;

        private void Start()
        {
            levelChoice.ClearOptions();
            levelChoice.AddOptions(new System.Collections.Generic.List<string> { "森林", "沙漠" });
            confirmButton.onClick.AddListener(() => ConfirmRequested = true);
            finishButton.onClick.AddListener(() => FinishRequested = true);
        }

        private void Update()
        {
            // ProcedureComponent 驱动流程状态机；当前流程由它持有。
            bool selecting = QGF.Procedure.CurrentProcedure is ExampleSelect03;
            stateText.text = selecting ? "选择关卡" : "读取已交接的关卡数据";
            dataText.text = $"已交接：{LoadedLevel}\n流程数据 Level：{(HasLevelData ? "保留中" : "已移除")}";
            confirmButton.interactable = selecting;
            finishButton.interactable = !selecting && QGF.Procedure.CurrentProcedure != null;
            levelChoice.interactable = selecting;
        }
    }

    public sealed class ExampleSelect03 : ProcedureBase
    {
        private ProcedureDataExample view;
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            view = Object.FindFirstObjectByType<ProcedureDataExample>();
            view.ConfirmRequested = false;
            view.HasLevelData = owner.HasData("Level");
        }
        protected override void OnUpdate(IFsm<IProcedureManager> owner, float elapsed, float realElapsed)
        {
            base.OnUpdate(owner, elapsed, realElapsed);
            if (!view.ConfirmRequested) return;
            // 数据存在流程状态机里，换流程后仍然可读。
            owner.SetData<VarString>("Level", view.levelChoice.options[view.levelChoice.value].text);
            ChangeState<ExampleLoad03>(owner);
        }
    }

    public sealed class ExampleLoad03 : ProcedureBase
    {
        private ProcedureDataExample view;
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            view = Object.FindFirstObjectByType<ProcedureDataExample>();
            view.FinishRequested = false;
            // 复制普通字符串；移除 Level 后不再持有它的 VarString 包装对象。
            view.LoadedLevel = owner.GetData<VarString>("Level").Value;
            view.HasLevelData = owner.HasData("Level");
        }
        protected override void OnUpdate(IFsm<IProcedureManager> owner, float elapsed, float realElapsed)
        {
            base.OnUpdate(owner, elapsed, realElapsed);
            if (!view.FinishRequested) return;
            // 临时参数用完即移除；状态机会归还 VarString，不要再 Release。
            owner.RemoveData("Level");
            ChangeState<ExampleSelect03>(owner);
        }
    }
}
