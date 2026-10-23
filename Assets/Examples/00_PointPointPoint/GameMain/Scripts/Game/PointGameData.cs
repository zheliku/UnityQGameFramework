namespace UnityQGameFramework.Examples.PointPointPoint
{
    // 流程与界面共享的本局数据。
    internal sealed class PointGameData
    {
        public string FormAsset;
        public bool StartRequested;
        public bool MenuRequested;
        public string Phase = "启动";
        public float Duration;
        public float Remaining;
        public int InitialLives;
        public int Lives;
        public int TargetCount;
        public int Hits;
        public int Score;
        public int BestScore;
        public int HitScore;
        public int MissPenalty;
        public float TargetX = 45;
        public float TargetY = 40;
        public string Result;
    }
}
