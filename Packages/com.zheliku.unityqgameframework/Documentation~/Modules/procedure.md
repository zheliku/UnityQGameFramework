# Procedure：把启动、菜单和战斗分成阶段

Procedure 管理启动、登录、菜单、战斗等游戏阶段。每个阶段在进入时准备所需数据和资源，离开时清理；加载完成后再切换到下一阶段。

[Procedure 示例](../../Samples~/01_Modules/06_Procedure/) 已配置流程类型和入口，可查看当前流程与停留时间，操作切换、传值和查询。完整的一局流程见 [点点点](../../Samples~/00_PointPointPoint/README.md)。

Procedure 本身是纯 C# 状态。ProcedureManager 使用 FsmManager 创建一个以 `IProcedureManager` 为持有者的状态机；当前流程的更新来自 Fsm。Unity 的 ProcedureComponent 负责根据 Inspector 里的类型名创建状态并启动入口流程。

## 做一个三秒后进入菜单的启动流程

创建 `GuideStartup.cs`，复制下面的完整代码：

```csharp
using UnityEngine;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace QgfDocs
{
    public sealed class GuideStartup : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            owner.SetData<VarString>("Welcome", "进入菜单");
            Log.Info("启动完成，等待三秒");
        }

        protected override void OnUpdate(IFsm<IProcedureManager> owner,
            float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(owner, elapseSeconds, realElapseSeconds);
            if (owner.CurrentStateTime >= 3f) ChangeState<GuideMenu>(owner);
        }
    }

    public sealed class GuideMenu : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            Log.Info(owner.GetData<VarString>("Welcome").Value);
        }
    }
}
```

场景中放入 `Runtime/Prefabs/UnityQGameFramework.prefab`，选中 Procedure 节点：

1. Available Procedures 勾选 `QgfDocs.GuideStartup` 和 `QgfDocs.GuideMenu`。
2. Entrance Procedure 选择 `QgfDocs.GuideStartup`。
3. 保存并运行。

Console 先输出“启动完成，等待三秒”，三秒后输出“进入菜单”。第二个流程没有再切换，所以它会一直作为当前流程。

## 生命周期怎么用

| 回调      | 什么时候执行     | 通常放什么                           |
| --------- | ---------------- | ------------------------------------ |
| OnInit    | 状态机初始化时   | 与本轮进入无关的初始状态             |
| OnEnter   | 每次进入流程     | 重置完成标记、订阅、发起加载         |
| OnUpdate  | 当前流程每次轮询 | 等待完成条件、推进阶段               |
| OnLeave   | 离开或关闭状态机 | 取消订阅，清理本阶段拥有的任务和引用 |
| OnDestroy | 状态机销毁时     | 状态对象最终清理                     |

流程通常会重复进入。不要把“本次预加载是否完成”只在字段初始化时设为 false；应在每次 OnEnter 重置。

`ChangeState<T>()` 是状态内部的受保护方法。外部按钮不要直接操作流程管理器的内部 FSM。可以像 StarForce 一样，调用当前业务流程公开的 `StartGame()` 设置意图，再由流程的 OnUpdate 判断并切换。

## 在流程之间传什么数据

`owner.SetData<VarInt32>("NextSceneId", sceneId)`、`GetData<T>`、`RemoveData` 用于这一个流程状态机的共享数据。它们不是全局 DataNode。流程离开不会自动清空全部状态机数据；换阶段后不再需要的数据应明确删除。

这些 Variable 由 FSM 接管，替换、删除或销毁 FSM 时归还引用池。交出去之后不要自己 Release，也不要把同一个 Variable 同时交给两个拥有者。

## 异步加载怎样衔接

StarForce 的做法值得沿用：OnEnter 订阅结果并发起请求，成功回调更新完成标记，OnUpdate 再判断是否切换，OnLeave 取消订阅。失败分支应记录错误并给出重试或返回方案，不能只把加载标记一直留为 false。

同时加载几份资源时，用各自的完成记录或计数管理，不要任何一个成功就进入下一阶段。结果中的 UserData 用来确认回调属于本流程。

## 几个边界

三个 [Procedure 示例](../../Samples~/01_Modules/06_Procedure/) 使用组件初始化，观察启动、切换和传值。一般业务不用手动重复组件已完成的启动步骤。

- Procedure 不是 MonoBehaviour，不能依靠 Inspector 给流程对象序列化普通字段；它由类型名和无参构造函数创建。
- 流程必须出现在 Available Procedures 中，才能参与这一个流程状态机。
- 入口流程开始之前，ProcedureComponent 会等待一帧，供其他框架组件完成 Start；自己额外启动的异步服务仍要自己等待。
- 同一时间只有一个当前 Procedure。敌人 AI、技能状态等可以各自创建独立 Fsm，不必占用游戏主流程。

继续阅读：[Fsm](fsm.md)、[Scene](scene.md)、[运行链路](../00-runtime.md)。

源码：[ProcedureComponent](../../Runtime/Unity/Procedure/ProcedureComponent.cs)、[ProcedureManager](../../Runtime/Core/Procedure/ProcedureManager.cs)、[ProcedureBase](../../Runtime/Core/Procedure/ProcedureBase.cs)。
