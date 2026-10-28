# 运行机制

游戏代码通常通过 `QGF.Config`、`QGF.Resource`、`QGF.UI` 等入口取得场景组件。组件把 Unity 资源和生命周期交给纯 C# 的 Core 模块。

```text
业务脚本 → QGF / GameEntry → Unity 组件 → Core 管理器
                                      → 平台 Helper / YooAsset
```

## 初始化

1. `GameFrameworkComponent.Awake` 向 `GameEntry` 登记组件。
2. 各组件在 Awake 取得 Core 管理器，在 Start 接好 Resource、Event 和 Helper。
3. ProcedureComponent 等待组件启动后，进入 Inspector 指定的入口流程。

跨模块加载放在入口流程或组件启动后的业务 `Start`，不要在业务 Awake 中提前读表、加载资源或打开界面。运行时才实例化 QGF prefab 时，也要等待其 Start 完成。

`QGF` 是常用组件快捷入口，不是 QFramework 的 Architecture。扩展组件可通过 `GameEntry.GetComponent<T>()` 获取。纯 C# 模块使用：

```csharp
using UnityQGameFramework.Core;

IEventManager events = GameFrameworkEntry.GetModule<IEventManager>();
```

`GetModule<T>()` 只接受管理器接口，第一次创建实现，之后返回同一实例。Unity 组件已完成模块接线，普通业务优先使用组件入口。

## 更新与关闭

`BaseComponent.Update` 调用 `GameFrameworkEntry.Update`，按模块优先级更新状态机、流程、事件和任务。业务无需再次驱动 Core。游戏时间倍率影响逻辑时间，真实流逝时间仍单独传入。

Base 销毁时关闭 Core 模块、清空组件登记、引用池和缓存。业务拥有的订阅、资源借用与任务应在自己的生命周期结束时先清理，见[代码约定](03-conventions.md)。

## 异步结果

| 操作 | 何时可使用结果 |
| --- | --- |
| `LoadDataTables<Tables>()` | 同步返回 true 后 |
| `LoadDataTablesAsync<Tables>(address)` | await 完成后 |
| `LoadAssetAsync<T>(address)` | await 完成后；使用结束需归还 |
| Config / Localization 的 `ReadData` | 对应成功事件到达后 |
| Entity / UI / Scene | 成功事件或各自逻辑的显示、打开回调中 |

Core 模块与 Helper 的通知参数放在 `UnityQGameFramework.Core.Events`，由组件转换为 `UnityQGameFramework.Runtime` 的游戏事件。订阅 `QGF.Event` 的业务只需引入 Core 和 Runtime，不需要逐个事件写别名；`GameEventArgs` 等基础类型仍在 Core。直接订阅纯 C# 管理器通知时才使用 Core.Events。

源码：[GameFrameworkEntry](../Runtime/Core/Base/GameFrameworkEntry.cs)、[BaseComponent](../Runtime/Unity/Base/BaseComponent.cs)、[QGF](../Runtime/Unity/Base/QGF.cs)。
