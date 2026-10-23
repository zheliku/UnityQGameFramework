# ReferencePool 与 Variable：复用短生命周期的 C# 对象

ReferencePool 按类型缓存实现 `IReference` 的普通 C# 对象。事件参数、FSM 数据包装和内部任务都用它复用临时实例。

[ReferencePool 示例](../../Samples~/07_ReferencePool/) 对照实例复用、`Clear` 回调、容器接管、容量调整和严格检查。

`QGF.ReferencePool.EnableStrictCheck` 转发到 Core 的 `ReferencePool.EnableStrictCheck`；组件负责按 Inspector 中的策略设置初始值。独立的强制检查示例直接使用 Core 静态入口，不启动框架，避免清理正在被其他模块使用的引用。

## 自己定义一个可回收的参数

以下是完整类型，可以放进 `RewardContext.cs`：

```csharp
using UnityQGameFramework.Core;

namespace QgfDocs
{
    public sealed class RewardContext : IReference
    {
        public int PlayerId { get; private set; }
        public int Amount { get; private set; }

        public static RewardContext Create(int playerId, int amount)
        {
            var value = ReferencePool.Acquire<RewardContext>();
            value.PlayerId = playerId;
            value.Amount = amount;
            return value;
        }

        public void Clear()
        {
            PlayerId = 0;
            Amount = 0;
        }
    }
}
```

在自己的方法中使用：

```csharp
// 代码片段：调用方没有把所有权交给其他模块，所以由调用方归还。
var context = QgfDocs.RewardContext.Create(7, 20);
try
{
    UnityQGameFramework.Runtime.Log.Info(context.Amount);
}
finally
{
    UnityQGameFramework.Core.ReferencePool.Release(context);
}
```

Clear 在归还时清空值、集合和外部引用。下一次 Acquire 可能返回相同实例，每次使用的状态放在 Create 或初始化方法中设置。

## 管理权转移后不能再次归还

Event.Fire 接收的参数由事件池释放；FSM.SetData、DataNode.SetData 接收的 Variable 由对应容器释放；ObjectPool 中的 ObjectBase 由对象池管理。把对象交给这些拥有者以后，调用方不要再 Release。

反过来，普通 `new` 出来的对象也不会因为实现了 IReference 就自动进入池。项目应保持 Create/Acquire 与 Release 的约定，避免有人归还、有人继续持有。

开发时可以启用 `ReferencePool.EnableStrictCheck` 辅助检查错误归还；它不替代所有权设计。ReferencePoolComponent 主要用于配置和展示池状态，调用 Acquire/Release 用 Core 的静态 ReferencePool。

五个示例中的预热与裁剪示例同时显示空闲、借出和累计计数。`Remove`、`RemoveAll` 只处理空闲引用。`ClearAll` 会删除全部类型集合，不能在正常游戏中用它回收仍被其他模块持有的对象；严格检查示例用不启动其他模块的独立场景演示这一操作。

## Variable 是什么

Variable 是实现 IReference 的可装箱数据基类，`Variable<T>` 提供 Value。Unity 层的 VarInt32、VarString、VarBoolean、VarVector3 等是具体包装类型。

```csharp
// 代码片段：独立使用的包装对象由自己释放。
UnityQGameFramework.Runtime.VarInt32 count = 3;
int value = count.Value;
UnityQGameFramework.Core.ReferencePool.Release(count);
```

Variable 的主要用途是让同一个数据容器保存不同类型的值。它没有数据绑定、变更订阅或自动保存功能。不要将 `VarInt32` 当成 QFramework 的 `BindableProperty<int>`。

## 与 ObjectPool 怎么选

| 需求                                           | 选择          |
| ---------------------------------------------- | ------------- |
| 重复分配短期事件参数、任务参数                 | ReferencePool |
| 按名称取对象、记录使用次数、控制容量和过期释放 | ObjectPool    |
| 飞机、子弹等游戏实例的加载与复用               | Entity        |
| 窗口的加载、复用和显示次序                     | UI            |

少量低频对象可以直接 new，不必为所有业务类建立池。池会让所有权更严格；只有确实需要复用的对象才值得承担这个约定。

源码：[ReferencePool](../../Runtime/Core/Base/ReferencePool/ReferencePool.cs)、[Variable 泛型基类](../../Runtime/Core/Base/Variable/GenericVariable.cs)、[VarInt32](../../Runtime/Unity/Variable/VarInt32.cs)。
