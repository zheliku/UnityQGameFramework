# 点点点

宿主需启用 Input System：在 Player Settings 的 Active Input Handling 选择 Input System Package (New) 或 Both。示例不会替项目修改该设置。

打开 [PointPointPoint.unity](GameMain/Scenes/PointPointPoint.unity) 并运行。点击目标得分，点击棋盘空白会扣分并失去一次机会。完成目标、耗尽机会或倒计时结束后进入结算；最高分会保存。

导入 Sample 后等待编译完成即可运行，默认使用 YooAsset Editor Simulation，无需先点击 `Fix` 或 `Save`。构建应用时，将资源配置的`Play Mode`设为 `OfflinePlayMode`；在`Open Builder`打开 YooAsset Builder，选择对应包并使用完整首包复制选项，将内置文件输出到项目 `Assets/StreamingAssets/yoo`。再用 Unity 的 Build Profiles（2022.3 为 Build Settings）选择启动场景并构建。

按运行流程读代码：

1. [PointGameLaunch](GameMain/Scripts/Procedure/PointGameLaunch.cs) 读取 `Game.toml` 和最高分，进入菜单流程。
2. [PointGameMenu](GameMain/Scripts/Procedure/PointGameMenu.cs) 用 `QGF.UI.OpenUIForm` 打开界面，等待开始请求。
3. [PointGameView](GameMain/Scripts/UI/PointGameView.cs) 用 `QGF.Event.Fire` 上报命中或失误。
4. [PointGamePlay](GameMain/Scripts/Procedure/PointGamePlay.cs) 更新本局数据并判断何时结算。
5. [PointGameResult](GameMain/Scripts/Procedure/PointGameResult.cs) 保存新纪录，等待返回菜单。

`PointGameLaunch` 把 `PointGameData` 放进 Procedure 的 FSM 数据；菜单和游玩流程打开界面时，再把同一份数据作为 `userData` 传给 `PointGameView`。开始和返回菜单写入请求标记，由当前流程切换状态；命中和失误通过 `QGF.Event` 排队派发，只在游玩流程处理。Config 提供规则，Setting 保存最高分。

规则可在 `GameMain/Configs/Game.toml` 修改。
