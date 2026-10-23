# Scene：加载场景与等待完成

Scene 通过资源模块加载和卸载游戏场景，并记录各场景的当前状态。业务使用完整场景资产名，例如 `Assets/GameMain/Scenes/Battle.unity`。

[Scene 示例](../../Samples~/16_Scene/) 带有独立房间，可操作加载与卸载、切换房间和调整多场景顺序。下图是切换到橙色房间后的结果。

![Scene 示例的实际运行结果](../Images/example-scene.png)

## 准备一个目标场景

创建并保存 `Assets/MyGame/Scenes/Room.unity`，放一个能辨认的物体。Launcher 里保留框架根节点，Base 开启编辑器资源模式。

下面完整流程加载 Room，并在成功时打印路径：

```csharp
using UnityEngine;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace QgfDocs
{
    public sealed class SceneGuideProcedure : ProcedureBase
    {
        private const string Room = "Assets/MyGame/Scenes/Room.unity";
        private EventComponent events;
        private SceneComponent scenes;

        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            scenes = QGF.Scene;
            events = QGF.Event;
            events.Subscribe(LoadSceneSuccessEventArgs.EventId, OnLoaded);
            events.Subscribe(LoadSceneFailureEventArgs.EventId, OnFailed);
            scenes.LoadScene(Room, this);
        }

        private void OnLoaded(object sender, GameEventArgs e)
        {
            var args = (LoadSceneSuccessEventArgs)e;
            if (ReferenceEquals(args.UserData, this)) Log.Info("场景已加载：" + args.SceneAssetName);
        }

        private void OnFailed(object sender, GameEventArgs e)
        {
            var args = (LoadSceneFailureEventArgs)e;
            if (ReferenceEquals(args.UserData, this)) Log.Error(args.ErrorMessage);
        }

        protected override void OnLeave(IFsm<IProcedureManager> owner, bool isShutdown)
        {
            events.Unsubscribe(LoadSceneSuccessEventArgs.EventId, OnLoaded);
            events.Unsubscribe(LoadSceneFailureEventArgs.EventId, OnFailed);
            if (!isShutdown && scenes.SceneIsLoaded(Room)) scenes.UnloadScene(Room);
            base.OnLeave(owner, isShutdown);
        }
    }
}
```

在 Procedure 中选择这个入口并运行。Hierarchy 应同时存在 Launcher 和 Room。加载游戏场景并不会替你创建玩家，也不表示该场景里的全部业务初始化已经完成。

## 编辑器与 Player 的准备不同

当前 EditorResourceComponent 在编辑器资源模式中按资产路径使用编辑器场景加载 API，不要求把示例场景写入宿主 Build Profiles。

发行模式通过资源管理器加载已构建的场景资源。启动 Launcher 如何进入 Player、游戏场景如何进入资源构建，仍由项目决定。不要直接从 Room 点击 Play 来绕过 Launcher 的初始化。

## 查询状态，避免重复请求

| 方法 | 含义 |
| --- | --- |
| HasScene | 资源层面是否有这个场景 |
| SceneIsLoading | 这条场景加载是否正在进行 |
| SceneIsLoaded | 是否已经在 Scene 模块的已加载记录中 |
| SceneIsUnloading | 是否正在卸载 |
| GetLoadedSceneAssetNames | 取得当前模块记录的全部已加载游戏场景 |

已加载、正在加载或正在卸载同一个场景时再次 LoadScene，会被状态检查拒绝。卸载后立即重载，也要等 UnloadSceneSuccess 再请求。

## 激活场景与主相机

`SetSceneOrder` 选择最大顺序对应的场景作为 Unity 激活场景。加载中可以先登记顺序，加载完成时应用。激活场景决定未指定归属的新物体进入哪个场景，不会自动隐藏其他场景、移动已有物体或改变加载优先级。

`ActiveSceneChangedEventArgs` 带回上一个和当前激活场景。卸载后会选择剩余场景中的最大顺序；没有游戏场景时回到框架场景。第三个示例同时展示两个房间，点击创建可验证物体归属。

![多场景激活顺序与主相机](../Images/example-scene-order.png)

## 取消、切换和摄像机

此模块没有通用的“立刻取消场景加载”入口。还在加载时不能直接 UnloadScene。需要在加载中退出业务时，让一个仍存活的场景协调者接住完成结果，再卸载不再需要的场景。

三个示例共用 `SceneExampleCleanup`：控制器退出后，等待由 Scene 组件的生命周期约束，加载结束再卸载自己请求的房间。框架退出时取消等待。切换示例同时记录当前房间与待加载路径，其他业务加载的场景继续保留。

Scene.MainCamera 是组件维护的摄像机引用，自己的摄像机变化后可调用 RefreshMainCamera。摄像机叠加、URP Renderer 和画面布局仍由项目设置；Scene 不会自动把新项目改成某个渲染管线。

源码：[SceneComponent](../../Runtime/Unity/Scene/SceneComponent.cs)、[SceneManager](../../Runtime/Core/Scene/SceneManager.cs)、[编辑器资源实现](../../Runtime/Unity/Resource/EditorResourceComponent.cs)。
