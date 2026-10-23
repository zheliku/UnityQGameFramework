# Fsm：让一个对象在等待和工作之间切换

Procedure 管理游戏主阶段；Fsm 适合管理敌人、技能和独立任务的状态。每台状态机持有自己的状态实例。

[Fsm 示例](../../Samples~/05_Fsm/) 展示状态切换、状态数据、命名实例、查询以及创建和销毁。相近重载在同一场景中切换查看。

命名实例示例通过 `FsmBase` 统一显示完整名称、当前状态、注册状态数和运行标记；Type 销毁重载使用查询结果中的 `OwnerType` 与 `Name`。状态查询示例通过 `IFsm<T>.Name` 显示未命名实例的名称状态。

## 一个完整的等待状态机

创建 `WorkerExample.cs`，挂到空物体。场景需要框架预制体中的 Fsm 和 Base。

```csharp
using UnityEngine;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Runtime;

namespace QgfDocs
{
    public sealed class WorkerExample : MonoBehaviour
    {
        private FsmComponent manager;
        private IFsm<WorkerExample> fsm;

        private void Start()
        {
            manager = QGF.Fsm;
            fsm = manager.CreateFsm(GetInstanceID().ToString(), this,
                new WaitingState(), new WorkingState());
            fsm.Start<WaitingState>();
        }

        private void OnDestroy()
        {
            if (manager != null && fsm != null && !fsm.IsDestroyed)
                manager.DestroyFsm(fsm);
        }

        private sealed class WaitingState : FsmState<WorkerExample>
        {
            protected override void OnEnter(IFsm<WorkerExample> owner)
            {
                Log.Info(owner.Owner.name + "：等待");
            }

            protected override void OnUpdate(IFsm<WorkerExample> owner,
                float elapseSeconds, float realElapseSeconds)
            {
                if (owner.CurrentStateTime >= 1f) ChangeState<WorkingState>(owner);
            }
        }

        private sealed class WorkingState : FsmState<WorkerExample>
        {
            protected override void OnEnter(IFsm<WorkerExample> owner)
            {
                Log.Info(owner.Owner.name + "：开始工作");
            }
        }
    }
}
```

运行一秒后，状态从 WaitingState 切换为 WorkingState。在场景中放两份这个脚本，也会创建两台独立状态机，因为它们使用不同名称。

## 状态机怎样被找到

FSM 的身份由持有者类型和名称共同确定。没有传名称的重载使用空名称，同一种持有者类型不能随意重复创建同名状态机。多个同类敌人要使用各自名称，例如实体 Id。

`owner.Owner` 是业务持有者；`owner.CurrentState` 是当前状态；`CurrentStateTime` 是当前状态持续的逻辑时间。`CreateFsm` 只创建，`Start<TState>()` 才进入第一个状态。

## 状态的复用和数据

初始化后，FSM 保存这些状态实例，切换时调用旧状态 OnLeave、新状态 OnEnter。不要在状态中保存“这一次攻击”的旧目标而不重置。一个状态实例也不要同时传给两台状态机使用。

`SetData<T>`、`GetData<T>`、`HasData`、`RemoveData` 可以存储该 FSM 的 Variable 数据。状态之间也可以通过持有者的普通属性共享数据；不是每个字段都必须包装为 Variable。

FSM 本身由管理器统一更新，不要再在 MonoBehaviour.Update 中手动调用它的内部更新。销毁业务对象时调用 `DestroyFsm`，否则它还会持有业务对象并继续轮询。

## 时间和切换

CurrentStateTime 使用逻辑时间，timeScale 为 0 时不会按实际秒数增长。需要真实计时的状态，在 OnUpdate 中累计 realElapseSeconds。

ChangeState 是同步的，会在当前调用里执行离开和进入逻辑。切换以后不要继续按旧状态的假设执行剩余代码；通常切换后就 return。目标状态必须在创建这台 FSM 时提供。

源码：[FsmComponent](../../Runtime/Unity/Fsm/FsmComponent.cs)、[FsmState](../../Runtime/Core/Fsm/FsmState.cs)、[IFsm](../../Runtime/Core/Fsm/IFsm.cs)、[Fsm 实现](../../Runtime/Core/Fsm/Fsm.cs)。
