using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.PointPointPoint
{
    public sealed class PointGameMenu : ProcedureBase
    {
        private PointGameData game;
        private int formId;

        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            game = (PointGameData)owner.GetData<VarObject>("Game").Value;
            game.Phase = "菜单";
            game.StartRequested = false;
            // 打开预制体，并将本局数据作为 userData 交给 UIForm。
            formId = QGF.UI.OpenUIForm(game.FormAsset, "Game", game);
        }

        protected override void OnUpdate(IFsm<IProcedureManager> owner, float elapsed, float realElapsed)
        {
            base.OnUpdate(owner, elapsed, realElapsed);
            // 按钮只提出请求。由流程决定何时离开菜单。
            if (game.StartRequested) ChangeState<PointGamePlay>(owner);
        }
        protected override void OnLeave(IFsm<IProcedureManager> owner, bool shutdown)
        {
            // 交回 UI 模块，下一阶段重新打开时会复用缓存实例。
            GameEntry.GetComponent<UIComponent>()?.CloseUIForm(formId);
            base.OnLeave(owner, shutdown);
        }
    }
}
