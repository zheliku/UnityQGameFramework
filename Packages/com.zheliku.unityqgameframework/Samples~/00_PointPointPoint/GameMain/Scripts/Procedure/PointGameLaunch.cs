using System.IO;
using UnityEngine.SceneManagement;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.PointPointPoint
{
    public sealed class PointGameLaunch : ProcedureBase
    {
        private bool loaded;
        private object request;

        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            loaded = false;
            object token = request = new object();
            // 从启动场景定位 GameMain，移动整个示例目录后仍可找到资产。
            string root = Path.GetDirectoryName(Path.GetDirectoryName(SceneManager.GetActiveScene().path)).Replace('\\', '/');
            var game = new PointGameData { FormAsset = root + "/UI/UIForms/PointGameView.prefab" };
            // 状态机持有 VarObject，后续流程从同一份 Game 数据继续运行。
            var context = ReferencePool.Acquire<VarObject>();
            context.Value = game;
            owner.SetData("Game", context);
            // 窗口组决定深度与覆盖关系；窗口打开到这个已注册的组中。
            QGF.UI.AddUIGroup("Game", 0);
            // 先订阅再读取，回调通过 UserData 识别本流程的请求。
            QGF.Event.Subscribe(LoadConfigSuccessEventArgs.EventId, OnLoaded);
            QGF.Event.Subscribe(LoadConfigFailureEventArgs.EventId, OnFailed);
            // 编辑器直接读资产；Player 先初始化包内清单，再通过同一资产名读取 TOML。
            string configAsset = root + "/Configs/Game.toml";
            if (QGF.Base.EditorResourceMode) QGF.Config.ReadData(configAsset, token);
            else QGF.Resource.InitResources(() =>
            {
                if (ReferenceEquals(request, token)) QGF.Config.ReadData(configAsset, token);
            });
        }

        private void OnLoaded(object sender, GameEventArgs args)
        {
            if (ReferenceEquals(((LoadConfigSuccessEventArgs)args).UserData, request)) loaded = true;
        }

        private void OnFailed(object sender, GameEventArgs args)
        {
            var failure = (LoadConfigFailureEventArgs)args;
            if (ReferenceEquals(failure.UserData, request)) Log.Error(failure.ErrorMessage);
        }

        protected override void OnUpdate(IFsm<IProcedureManager> owner, float elapsed, float realElapsed)
        {
            base.OnUpdate(owner, elapsed, realElapsed);
            if (!loaded) return;
            var game = (PointGameData)owner.GetData<VarObject>("Game").Value;
            // Config 已加载完成，读取规则后才创建这一局的初始状态。
            game.Duration = QGF.Config.GetFloat("Game.Duration");
            game.InitialLives = QGF.Config.GetInt("Game.Lives");
            game.TargetCount = QGF.Config.GetInt("Game.TargetCount");
            game.HitScore = QGF.Config.GetInt("Score.Hit");
            game.MissPenalty = QGF.Config.GetInt("Score.Miss");
            // 读取玩家保存的值；缺键时使用默认值，和只读 Config 规则分开管理。
            game.BestScore = QGF.Setting.GetInt("BestScore", 0);
            ChangeState<PointGameMenu>(owner);
        }

        protected override void OnLeave(IFsm<IProcedureManager> owner, bool shutdown)
        {
            request = null;
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(LoadConfigSuccessEventArgs.EventId, OnLoaded)) events.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnLoaded);
                if (events.Check(LoadConfigFailureEventArgs.EventId, OnFailed)) events.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnFailed);
            }
            base.OnLeave(owner, shutdown);
        }
    }
}
