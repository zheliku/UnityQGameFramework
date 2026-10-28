namespace UnityQGameFramework.Examples.PointPointPoint
{
    // 流程与界面共享的本局数据。
    /// <summary>
    /// 各流程共享的本局游戏数据。
    /// </summary>
    internal sealed class PointGameData
    {
        /// <summary>
        /// 游戏界面的资源地址。
        /// </summary>
        public string FormAsset;

        /// <summary>
        /// 是否请求开始游戏。
        /// </summary>
        public bool StartRequested;

        /// <summary>
        /// 是否请求返回菜单。
        /// </summary>
        public bool MenuRequested;

        /// <summary>
        /// 当前游戏阶段。
        /// </summary>
        public string Phase = "Launch";

        /// <summary>
        /// 每局持续时长，以秒为单位。
        /// </summary>
        public float Duration;

        /// <summary>
        /// 本局剩余时长，以秒为单位。
        /// </summary>
        public float Remaining;

        /// <summary>
        /// 每局初始生命值。
        /// </summary>
        public int InitialLives;

        /// <summary>
        /// 当前生命值。
        /// </summary>
        public int Lives;

        /// <summary>
        /// 场内目标数量。
        /// </summary>
        public int TargetCount;

        /// <summary>
        /// 本局命中次数。
        /// </summary>
        public int Hits;

        /// <summary>
        /// 本局得分。
        /// </summary>
        public int Score;

        /// <summary>
        /// 已保存的最高得分。
        /// </summary>
        public int BestScore;

        /// <summary>
        /// 每次命中增加的分数。
        /// </summary>
        public int HitScore;

        /// <summary>
        /// 每次未命中扣除的分数。
        /// </summary>
        public int MissPenalty;

        /// <summary>
        /// 目标生成范围的水平半宽。
        /// </summary>
        public float TargetX = 45;

        /// <summary>
        /// 目标生成范围的垂直半高。
        /// </summary>
        public float TargetY = 40;

        /// <summary>
        /// 本局结算提示。
        /// </summary>
        public string Result;
    }
}
