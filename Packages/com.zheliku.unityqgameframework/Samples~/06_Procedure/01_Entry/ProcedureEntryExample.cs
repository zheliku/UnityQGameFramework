using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ProcedureEntryExample : MonoBehaviour
    {
        [SerializeField] private Text currentText, elapsedText, enteredText;
        internal int Entered;

        private void Update()
        {
            // Procedure 组件完成初始化前，CurrentProcedure 为 null。
            var procedure = QGF.Procedure.CurrentProcedure;
            currentText.text = procedure == null ? "等待框架启动" : "入口：" + procedure.GetType().Name;
            elapsedText.text = $"当前流程停留：{QGF.Procedure.CurrentProcedureTime:F1} 秒";
            enteredText.text = $"OnEnter 已执行：{Entered} 次";
        }
    }

    public sealed class ExampleLaunch01 : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            // Procedure 组件按 Inspector 中的入口配置启动。
            Object.FindFirstObjectByType<ProcedureEntryExample>().Entered++;
        }
    }
}
