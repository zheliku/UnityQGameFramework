using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.PointPointPoint
{
    // QGF.UI 负责加载界面预制体和回收实例。
    /// <summary>
    /// 显示本局状态并上报点击事件。
    /// </summary>
    public sealed class PointGameView : UIFormLogic
    {
        [SerializeField] private GameObject menu, play, result;
        [SerializeField] private Text phase, rules, record, status, resultTitle, resultScore;
        [SerializeField] private Button startButton, targetButton, boardButton, menuButton;
        [SerializeField] private RectTransform board, target;
        internal PointGameData Game { get; private set; }

        /// <summary>
        /// 初始化实例。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            // OnInit 在实例首次创建时执行，用来注册按钮。
            startButton.onClick.AddListener(() => Game.StartRequested = true);
            menuButton.onClick.AddListener(() => Game.MenuRequested = true);
            // 棋盘按钮覆盖空白区域；目标按钮只处理命中。
            boardButton.onClick.AddListener(() => QGF.Event.Fire(this, PointGameClick.Create(false)));
            // Fire 将点击排入事件队列；流程收到后计分，事件参数由框架归还引用池。
            targetButton.onClick.AddListener(() => QGF.Event.Fire(this, PointGameClick.Create(true)));
        }

        /// <summary>
        /// 打开界面并接收业务数据。
        /// </summary>
        /// <param name="userData">打开界面时传入的游戏数据。</param>
        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            // OpenUIForm 的 userData 传到这里，界面和流程共享同一局数据。
            Game = (PointGameData)userData;
            Render();
        }

        /// <summary>
        /// 更新界面状态。
        /// </summary>
        /// <param name="elapsed">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapsed">真实流逝时间，以秒为单位。</param>
        protected override void OnUpdate(float elapsed, float realElapsed)
        {
            base.OnUpdate(elapsed, realElapsed);
            Render();
        }

        private void Render()
        {
            phase.text = "启动 → 菜单 → 游玩 → 结算    当前：" + Game.Phase;
            menu.SetActive(Game.Phase == "菜单");
            play.SetActive(Game.Phase == "游玩");
            result.SetActive(Game.Phase == "结算");
            rules.text = $"在 {Game.Duration:0} 秒内点中 {Game.TargetCount} 个目标。";
            record.text = $"点空扣分并失去一次机会。最高分：{Game.BestScore}";
            status.text = $"剩余 {Game.Remaining:F1} 秒    命中 {Game.Hits}/{Game.TargetCount}    机会 {Game.Lives}    分数 {Game.Score}";
            target.anchoredPosition = new Vector2(
                (board.rect.width - target.rect.width) * Game.TargetX / 100f,
                -(board.rect.height - target.rect.height) * Game.TargetY / 100f);
            resultTitle.text = Game.Result;
            resultScore.text = $"本局 {Game.Score} 分    最高 {Game.BestScore} 分";
        }
    }
}
