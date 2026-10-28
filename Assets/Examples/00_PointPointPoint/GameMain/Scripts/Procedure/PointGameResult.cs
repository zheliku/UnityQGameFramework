using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.PointPointPoint
{
    /// <summary>
    /// 保存结算结果并等待返回菜单。
    /// </summary>
    public sealed class PointGameResult : ProcedureBase
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
            game.Phase = "Result";
            if (game.Hits >= game.TargetCount)
            {
                game.Result = "Complete!";
            }
            else if (game.Lives <= 0)
            {
                game.Result = "No lives left";
            }
            else
            {
                game.Result = "Time is up";
            }

            game.MenuRequested = false;
            // 返回本次打开的序号；窗口可能异步加载，userData 传给 OnOpen。
            formId = QGF.UI.OpenUIForm(game.FormAsset, "Game", game);
            if (game.Score > game.BestScore)
            {
                game.BestScore = game.Score;
                // SetInt 更新内存，Save 写入磁盘。
                QGF.Setting.SetInt("BestScore", game.BestScore);
                QGF.Setting.Save();
            }
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
            if (game.MenuRequested)
            {
                ChangeState<PointGameMenu>(owner);
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
