# StarForce

从 Package Manager 导入 **02 · StarForce**，在 URP 项目打开 [StarForce Launcher](../Samples~/02_StarForce/StarForce%20Launcher.unity)。宿主启用 Input System，等待编译完成后运行；Game 视图可选 3:4，点击开始后按住鼠标左键移动并开火。

![StarForce 战斗](Images/starforce-battle.png)

## 按运行顺序读代码

| 步骤 | 入口 | 关注的调用 |
| --- | --- | --- |
| 启动 | [ProcedureLaunch](../Samples~/02_StarForce/GameMain/Scripts/Procedure/ProcedureLaunch.cs) | 初始化设置、构建信息与默认字典 |
| 预加载 | [ProcedurePreload](../Samples~/02_StarForce/GameMain/Scripts/Procedure/ProcedurePreload.cs) | Config、Luban 表和必要资源准备完成后继续 |
| 菜单 | [ProcedureMenu](../Samples~/02_StarForce/GameMain/Scripts/Procedure/ProcedureMenu.cs) | 打开菜单 UI，收到开始请求后切换流程 |
| 切场景 | [ProcedureChangeScene](../Samples~/02_StarForce/GameMain/Scripts/Procedure/ProcedureChangeScene.cs) | 加载场景，按成功或失败结果继续 |
| 战斗 | [ProcedureMain](../Samples~/02_StarForce/GameMain/Scripts/Procedure/ProcedureMain.cs) | 创建并更新本局游戏 |

实体逻辑位于 `GameMain/Scripts/Entity`，界面位于 `GameMain/Scripts/UI`，业务数据位于 `GameMain/Scripts/Data`。显示与隐藏由 Entity 管理；窗口由 UI 管理；Luban 提供静态参数。

## 修改与构建

Excel 位于 Sample 的 `UnityQGameFramework/Config/Source`。在 DataTable Inspector 选择该目录的 LubanProfile 并生成；生成结果已经随示例提供，首次运行不用重新生成。运行参数与翻译位于 `GameMain/Configs/*.toml`。

资源使用 Sample 自己的 ResourceSettings 和 YooAssetCollector，普通 Play 自动生成 Editor Simulation。打包时改为 OfflinePlayMode，先用 YooAsset Builder 构建并复制完整首包，再用 Unity 构建 Launcher 场景，步骤见[资源构建](Modules/resource-build.md)。

来源与授权见 Sample 的 [第三方声明](../Samples~/02_StarForce/THIRD-PARTY.md)。
