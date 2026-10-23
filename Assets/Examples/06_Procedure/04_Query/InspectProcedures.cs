using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class InspectProcedures : MonoBehaviour
    {
        [SerializeField] private Text currentText, resultText;
        [SerializeField] private Dropdown queryMode;
        [SerializeField] private Button queryButton;

        private void Start()
        {
            queryMode.ClearOptions();
            queryMode.AddOptions(new System.Collections.Generic.List<string> { "泛型：已注册", "Type：已注册", "泛型：未注册", "Type：未注册" });
            queryButton.onClick.AddListener(() =>
            {
                bool exists;
                ProcedureBase procedure;
                switch (queryMode.value)
                {
                    // 先检查注册情况；它不表示该流程当前正在运行。
                    case 0: exists = QGF.Procedure.HasProcedure<ExampleInspect04>(); procedure = QGF.Procedure.GetProcedure<ExampleInspect04>(); break;
                    case 1:
                        exists = QGF.Procedure.HasProcedure(typeof(ExampleInspectIdle04));
                        procedure = QGF.Procedure.GetProcedure(typeof(ExampleInspectIdle04));
                        break;
                    case 2:
                        exists = QGF.Procedure.HasProcedure<ExampleLaunch01>();
                        procedure = QGF.Procedure.GetProcedure<ExampleLaunch01>();
                        break;
                    default:
                        exists = QGF.Procedure.HasProcedure(typeof(ExampleLaunch01));
                        procedure = QGF.Procedure.GetProcedure(typeof(ExampleLaunch01));
                        break;
                }
                resultText.text = $"注册状态：{(exists ? "已注册" : "未注册")}\n取得实例：{procedure?.GetType().Name ?? "无实例"}\n"
                    + $"当前流程：{(procedure != null && ReferenceEquals(procedure, QGF.Procedure.CurrentProcedure) ? "是" : "否")}";
            });
        }

        private void Update()
        {
            // ProcedureComponent 驱动流程状态机；当前流程由它持有。
            currentText.text = $"当前：{QGF.Procedure.CurrentProcedure?.GetType().Name}\n"
                + $"停留 {QGF.Procedure.CurrentProcedureTime:F1} 秒，每两秒自动交替。";
        }
    }

    public sealed class ExampleInspect04 : ProcedureBase
    {
        protected override void OnUpdate(IFsm<IProcedureManager> owner, float elapsed, float realElapsed)
        {
            base.OnUpdate(owner, elapsed, realElapsed);
            if (owner.CurrentStateTime >= 2f) ChangeState<ExampleInspectIdle04>(owner);
        }
    }
    public sealed class ExampleInspectIdle04 : ProcedureBase
    {
        protected override void OnUpdate(IFsm<IProcedureManager> owner, float elapsed, float realElapsed)
        {
            base.OnUpdate(owner, elapsed, realElapsed);
            if (owner.CurrentStateTime >= 2f) ChangeState<ExampleInspect04>(owner);
        }
    }
}
