# StarForce

在 Package Manager 导入 `02 · StarForce`，打开 `StarForce Launcher.unity`。Game 视图选 3:4，点击“开始”后按住鼠标左键移动并开火。

| 菜单 | 战斗 |
| --- | --- |
| ![StarForce 菜单](Images/starforce-menu.png) | ![玩家飞机与子弹](Images/starforce-battle.png) |

## 启动

`ProcedureLaunch` 读取内置设置，恢复语言和音量。`ProcedurePreload` 加载 TOML 参数、Luban 表、语言字典和 UI 字体；全部完成后，把菜单场景 Id 传给 `ProcedureChangeScene`。

`ProcedureInitResources` 调用 `ResourceComponent.InitializePackageAsync`。QGF 在此操作中完成 YooAsset 包初始化、版本请求和清单加载，StarForce 不重复调用这些底层步骤。`ProcedureUpdateResources` 创建下载器；有待更新文件时先显示数量与大小，确认后再下载。下载完成后进入 `ProcedureClearUnusedCache`，清除当前清单不再引用的 Bundle 缓存，再开始 TOML、Luban 与字典预载入。没有待下载文件时直接进入预载入。

`ProcedurePreload` 用 `LoadDataTablesAsync` 等待 Luban 表集、用 `LoadAssetAsync` 加载主字体；取消预载入会取消对应等待。Config 和 Localization 保持 UGF 的 `ReadData`、事件与 `UserData` 用法，展示兼容的回调式接口。

下载使用 `ResourceComponent.DownloadResourcesAsync`，失败转为异常，取消流程时终止本次下载。进度从下载器读取，流程不自行轮询任务是否完成。启动和预载失败提示使用 Launcher 携带的内置对话框，不依赖尚未下载成功的 UI 资源。

`ProcedureChangeScene` 用 Id 查询 `DRScene`，加载 `Menu.unity`，成功后进入 `ProcedureMenu`。点击开始时，菜单流程写入主场景 Id 和玩法类型，再经过同一个切场景流程进入 `Main.unity` 与 `ProcedureMain`。

## 战斗

```text
ProcedureMain
  → GameBase / SurvivalGame
  → EntityExtension 查询实体表并显示 MyAircraft
  → MyAircraft 更新移动与攻击
  → Weapon 创建 Bullet
  → 碰撞后归还 Bullet
```

表记录的 Id 标识实体类型，`entityId` 标识场上的单个实例。`SurvivalGame` 从小行星表的实际记录中随机选择类型，因此表 Id 可以不连续。`HPBarComponent` 使用 ObjectPool 复用血条，结束显示后归还。

## 表与资源

配置工作区位于示例的 `UnityQGameFramework/Config`：`Editor/LubanProfile.asset` 决定生成参数，`Source/Datas` 保存 Excel，`Generated` 和 `Data` 保存产物。在 `QGF > Settings` 选择 StarForce Profile，修改 Excel 后执行 `QGF > Generate Tables`。

Launcher 的 ResourceComponent 引用 StarForce 自己的 ResourceSettings，编辑器通过对应的 YooAsset Profile 运行模拟包。构建 Windows Player 时运行 `QGF > Examples > 02 StarForce > Build Windows Player`；构建产物会包含所需资源包。

源码位于仓库的 `Assets/Examples/02_StarForce`，UPM 样本位于 `Samples~/02_StarForce`。资源来源和许可见 [THIRD-PARTY.md](../Samples~/02_StarForce/THIRD-PARTY.md)。

启动更新的职责划分参考 [YooAsset 3.0.6 的 Space Shooter](https://github.com/tuyoogame/YooAsset/tree/3.0.6/Assets/YooAsset/Samples~/Space%20Shooter)、[UGF 教程](https://share.note.youdao.com/ynoteshare/index.html?id=67c0d8a6b20893d82e3fe990cca802c2&type=note)和 [GameFramework-Next](https://github.com/Alex-Rachel/GameFramework-Next)。StarForce 继续使用 QGF 的 Procedure、Resource 和 UI 入口，异步等待使用 UniTask。
