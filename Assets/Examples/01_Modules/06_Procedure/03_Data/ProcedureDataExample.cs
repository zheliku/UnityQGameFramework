using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示把关卡选择交给加载流程。
    /// </summary>
    public sealed class ProcedureDataExample : MonoBehaviour
    {
        [SerializeField]
        internal Dropdown levelChoice;
        [SerializeField]
        private Button confirmButton, finishButton;
        [SerializeField]
        private Text stateText, dataText;
        internal bool ConfirmRequested, FinishRequested;
        internal string LoadedLevel = "No level selected";
        internal bool HasLevelData;
        private void Start()
        {
            levelChoice.ClearOptions();
            levelChoice.AddOptions(new System.Collections.Generic.List<string> { "Forest", "Desert" });
            confirmButton.onClick.AddListener(() => ConfirmRequested = true);
            finishButton.onClick.AddListener(() => FinishRequested = true);
        }

        private void Update()
        {
            // ProcedureComponent 驱动流程状态机；当前流程由它持有。
            bool selecting = QGF.Procedure.CurrentProcedure is ExampleSelect03;
            stateText.text = selecting ? "Select Level" : "Read the transferred level data";
            dataText.text = $"Transferred: {LoadedLevel}\nProcedure data Level: {(HasLevelData ? "Retained" : "Removed")}";
            confirmButton.interactable = selecting;
            finishButton.interactable = !selecting && QGF.Procedure.CurrentProcedure != null;
            levelChoice.interactable = selecting;
        }
    }

    /// <summary>
    /// 保存所选关卡信息的流程。
    /// </summary>
    public sealed class ExampleSelect03 : ProcedureBase
    {
        private ProcedureDataExample view;

        /// <summary>
        /// 进入当前流程。
        /// </summary>
        /// <param name="owner">所属流程状态机。</param>
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            view = Object.FindFirstObjectByType<ProcedureDataExample>();
            view.ConfirmRequested = false;
            view.HasLevelData = owner.HasData("Level");
        }

        /// <summary>
        /// 更新当前流程。
        /// </summary>
        /// <param name="owner">所属流程状态机。</param>
        /// <param name="elapsed">逻辑时间间隔，以秒为单位。</param>
        /// <param name="realElapsed">真实时间间隔，以秒为单位。</param>
        protected override void OnUpdate(IFsm<IProcedureManager> owner, float elapsed, float realElapsed)
        {
            base.OnUpdate(owner, elapsed, realElapsed);
            if (!view.ConfirmRequested)
            {
                return;
            }

            // 数据存在流程状态机里，换流程后仍然可读。
            owner.SetData<VarString>("Level", view.levelChoice.options[view.levelChoice.value].text);
            ChangeState<ExampleLoad03>(owner);
        }
    }

    /// <summary>
    /// 读取所选关卡信息的加载流程。
    /// </summary>
    public sealed class ExampleLoad03 : ProcedureBase
    {
        private ProcedureDataExample view;

        /// <summary>
        /// 进入当前流程。
        /// </summary>
        /// <param name="owner">所属流程状态机。</param>
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            view = Object.FindFirstObjectByType<ProcedureDataExample>();
            view.FinishRequested = false;
            // 复制普通字符串；移除 Level 后不再持有它的 VarString 包装对象。
            view.LoadedLevel = owner.GetData<VarString>("Level").Value;
            view.HasLevelData = owner.HasData("Level");
        }

        /// <summary>
        /// 更新当前流程。
        /// </summary>
        /// <param name="owner">所属流程状态机。</param>
        /// <param name="elapsed">逻辑时间间隔，以秒为单位。</param>
        /// <param name="realElapsed">真实时间间隔，以秒为单位。</param>
        protected override void OnUpdate(IFsm<IProcedureManager> owner, float elapsed, float realElapsed)
        {
            base.OnUpdate(owner, elapsed, realElapsed);
            if (!view.FinishRequested)
            {
                return;
            }

            // 临时参数用完即移除；状态机会归还 VarString，不要再 Release。
            owner.RemoveData("Level");
            ChangeState<ExampleSelect03>(owner);
        }
    }
}
