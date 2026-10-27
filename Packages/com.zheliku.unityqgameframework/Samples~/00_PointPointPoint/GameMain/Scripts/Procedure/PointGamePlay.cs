using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.PointPointPoint
{
    /// <summary>
    /// 处理命中事件并判断结束条件。
    /// </summary>
    public sealed class PointGamePlay : ProcedureBase
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
            game.Phase = "游玩";
            game.Remaining = game.Duration;
            game.Lives = game.InitialLives;
            game.Score = 0;
            game.Hits = 0;
            // 返回本次打开的序号；窗口可能异步加载，userData 传给 OnOpen。
            formId = QGF.UI.OpenUIForm(game.FormAsset, "Game", game);
            // 只在游玩期间接收点击，离开时取消订阅。
            QGF.Event.Subscribe(PointGameClick.EventId, OnClick);
        }

        private void OnClick(object sender, GameEventArgs args)
        {
            // 游戏界面只发点击事件；这里确认事件来自当前这局的窗口再计分。
            if (!(sender is PointGameView view) || !ReferenceEquals(view.Game, game) || game.Lives <= 0 ||
                game.Hits >= game.TargetCount || game.Remaining <= 0)
            {
                return;
            }

            if (((PointGameClick)args).Hit)
            {
                game.Hits++;
                game.Score += game.HitScore;
                game.TargetX = Random.Range(5, 80);
                game.TargetY = Random.Range(5, 70);
            }
            else
            {
                game.Lives--;
                game.Score -= game.MissPenalty;
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
            // elapsed 是框架的游戏时间，暂停或变速会影响这一局的计时。
            game.Remaining = Mathf.Max(0, game.Remaining - elapsed);
            if (game.Hits >= game.TargetCount || game.Lives <= 0 || game.Remaining <= 0)
            {
                ChangeState<PointGameResult>(owner);
            }
        }

        /// <summary>
        /// 离开当前流程并清理订阅。
        /// </summary>
        /// <param name="owner">所属流程状态机。</param>
        /// <param name="shutdown">是否因框架关闭而离开。</param>
        protected override void OnLeave(IFsm<IProcedureManager> owner, bool shutdown)
        {
            // 停止 Play 时组件销毁顺序不固定，先确认事件组件还在。
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(PointGameClick.EventId, OnClick))
            {
                events.Unsubscribe(PointGameClick.EventId, OnClick);
            }

            GameEntry.GetComponent<UIComponent>()?.CloseUIForm(formId);
            base.OnLeave(owner, shutdown);
        }
    }
}
