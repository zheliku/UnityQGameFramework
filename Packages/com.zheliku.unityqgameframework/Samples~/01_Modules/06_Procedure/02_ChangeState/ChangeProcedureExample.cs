using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示菜单与游玩。
    /// </summary>
    public sealed class ChangeProcedureExample : MonoBehaviour
    {
        [SerializeField]
        private Text currentText, enteredText;
        [SerializeField]
        private Button enterButton, backButton;
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
            currentText.text = $"Launch → Menu ⇄ Play\nCurrent: {current?.GetType().Name}\nElapsed: {QGF.Procedure.CurrentProcedureTime:F1} s";
            enteredText.text = $"Menu entries: {MenuVisits} times    Enter Play: {PlayVisits} times";
            enterButton.interactable = current is ExampleMenu02;
            backButton.interactable = current is ExamplePlay02;
        }
    }

    /// <summary>
    /// 切入菜单的启动流程。
    /// </summary>
    public sealed class ExampleLaunch02 : ProcedureBase
    {
        /// <summary>
        /// 更新当前流程。
        /// </summary>
        /// <param name="owner">所属流程状态机。</param>
        /// <param name="elapsed">逻辑时间间隔，以秒为单位。</param>
        /// <param name="realElapsed">真实时间间隔，以秒为单位。</param>
        protected override void OnUpdate(IFsm<IProcedureManager> owner, float elapsed, float realElapsed)
        {
            base.OnUpdate(owner, elapsed, realElapsed);
            if (owner.CurrentStateTime >= 0.5f)
            {
                ChangeState<ExampleMenu02>(owner);
            }
        }
    }

    /// <summary>
    /// 等待开始请求的菜单流程。
    /// </summary>
    public sealed class ExampleMenu02 : ProcedureBase
    {
        private ChangeProcedureExample view;

        /// <summary>
        /// 进入当前流程。
        /// </summary>
        /// <param name="owner">所属流程状态机。</param>
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            view = Object.FindFirstObjectByType<ChangeProcedureExample>();
            view.EnterRequested = false;
            view.MenuVisits++;
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
            // 按钮只提出请求，流程自己决定什么时候转换。
            if (view.EnterRequested)
            {
                ChangeState<ExamplePlay02>(owner);
            }
        }
    }

    /// <summary>
    /// 等待返回请求的游玩流程。
    /// </summary>
    public sealed class ExamplePlay02 : ProcedureBase
    {
        private ChangeProcedureExample view;

        /// <summary>
        /// 进入当前流程。
        /// </summary>
        /// <param name="owner">所属流程状态机。</param>
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            view = Object.FindFirstObjectByType<ChangeProcedureExample>();
            view.BackRequested = false;
            view.PlayVisits++;
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
            if (view.BackRequested)
            {
                ChangeState(owner, typeof(ExampleMenu02));
            }
        }
    }
}
