# ReferencePool

复用轻量 C# 临时对象。

```csharp
var cargo = ReferencePool.Acquire<Cargo>();
// 使用 cargo；Cargo 实现 IReference.Clear。
ReferencePool.Release(cargo);
cargo = null;
```

Acquire 优先取空闲对象，池空才创建。Release 调用 Clear 并放回池中，归还后不再访问旧引用。它不管理 Unity 资源的释放；复用 GameObject 或带释放策略的对象使用 ObjectPool。

[运行示例与完整脚本](../../Samples~/01_Modules/07_ReferencePool/)。

源码：[ReferencePool](../../Runtime/Core/Base/ReferencePool/ReferencePool.cs)。
