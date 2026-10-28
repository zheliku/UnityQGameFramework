using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示从入口流程开始。
    /// </summary>
    public sealed class ProcedureEntryExample : MonoBehaviour
    {
        [SerializeField] private Text currentText, elapsedText, enteredText;
        internal int Entered;

        private void Update()
        {
            // Procedure 组件完成初始化前，CurrentProcedure 为 null。
            var procedure = QGF.Procedure.CurrentProcedure;
            currentText.text = procedure == null ? "Waiting for framework startup" : "Entry: " + procedure.GetType().Name;
            elapsedText.text = $"Time in current procedure: {QGF.Procedure.CurrentProcedureTime:F1} s";
            enteredText.text = $"OnEnter calls: {Entered} times";
        }
    }

    /// <summary>
    /// 示例启动流程。
    /// </summary>
    public sealed class ExampleLaunch01 : ProcedureBase
    {
        /// <summary>
        /// 进入当前流程。
        /// </summary>
        /// <param name="owner">所属流程状态机。</param>
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            // Procedure 组件按 Inspector 中的入口配置启动。
            Object.FindFirstObjectByType<ProcedureEntryExample>().Entered++;
        }
    }
}
