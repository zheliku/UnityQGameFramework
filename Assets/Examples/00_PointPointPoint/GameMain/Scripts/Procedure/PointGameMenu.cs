using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.PointPointPoint
{
    /// <summary>
    /// 显示菜单并等待开始请求。
    /// </summary>
    public sealed class PointGameMenu : ProcedureBase
    {
        private PointGameData game;
        private int formId;

        /// <summary>
        /// 进入当前流程。
        /// </summary>
        /// <param name="owner">所属流程状态机。</param>
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            game = (PointGameData)owner.GetData<VarObject>("Game").Value;
            game.Phase = "Menu";
            game.StartRequested = false;
            // 打开预制体，并将本局数据作为 userData 交给 UIForm。
            formId = QGF.UI.OpenUIForm(game.FormAsset, "Game", game);
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
            // 按钮只提出请求。由流程决定何时离开菜单。
            if (game.StartRequested)
            {
                ChangeState<PointGamePlay>(owner);
            }
        }

        /// <summary>
        /// 离开当前流程并清理订阅。
        /// </summary>
        /// <param name="owner">所属流程状态机。</param>
        /// <param name="shutdown">是否因框架关闭而离开。</param>
        protected override void OnLeave(IFsm<IProcedureManager> owner, bool shutdown)
        {
            // 交回 UI 模块，下一阶段重新打开时会复用缓存实例。
            GameEntry.GetComponent<UIComponent>()?.CloseUIForm(formId);
            base.OnLeave(owner, shutdown);
        }
    }
}
