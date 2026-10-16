# 基础 API

公共核心命名空间为 `UnityQGameFramework.Core`，Unity 组件位于 `UnityQGameFramework.Runtime`，编辑器工具位于 `UnityQGameFramework.Editor`。源码包含中文 XML 注释，可在 IDE 中查看各接口的完整成员与回调约定。

## Unity 工程入口

启动场景放置 `Runtime/Prefabs/UnityQGameFramework.prefab`。BaseComponent 在 Awake 中安装辅助器、注册组件；Start 中各组件建立模块依赖；流程组件等待其他组件的 Start 完成后进入入口流程。

```csharp
using UnityQGameFramework.Runtime;

var events = GameEntry.GetComponent<EventComponent>();
var resources = GameEntry.GetComponent<ResourceComponent>();
```

从业务 asmdef 使用这些类型时引用 `UnityQGameFramework.Runtime`，需要核心接口时同时引用 `UnityQGameFramework.Core`。不要从自己的 Update 再调用核心轮询：BaseComponent 已每帧驱动一次。

## 核心模块

```csharp
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Event;

IEventManager events = FrameworkEntry.GetModule<IEventManager>();
```

模块按接口获取、按需创建并复用同一实例。轮询遵循模块优先级，关闭按相反顺序进行，随后清理引用池与原生缓存。脱离 Unity 的宿主需要自行调用 Update/Shutdown；Unity 宿主由 BaseComponent 管理。

## 流程与辅助器

流程继承 `UnityQGameFramework.Core.Procedure.ProcedureBase`，在 ProcedureComponent 的 Inspector 中登记并选择入口流程。辅助器实现核心契约或继承对应 HelperBase，通过组件 Inspector 选择。自定义 runtime asmdef 中的具体类型会出现在类型选择列表中，编辑器专属类型不会作为运行时流程列出。

类型名字符串用于实例化辅助器、流程和序列化器。IL2CPP 游戏需在自己的 link.xml 中保留被反射创建的业务类型；包内 link.xml 只负责保留框架程序集。

## 事件与池

`Fire` 将事件排队，在模块 Update 中派发；`FireNow` 同步派发。事件参数实现 GameEventArgs，并在派发后归还引用池；不要在回调结束后继续保存这些参数实例。

ReferencePool 用于可清理的 C# 引用，ObjectPool 用于有 Spawn/Unspawn 生命周期的对象。实体、UI 和资源管理器使用对象池管理实例与资产的释放；不要绕过对应模块直接回收。

各模块间依赖及生命周期详见 [架构说明](architecture.md)。
