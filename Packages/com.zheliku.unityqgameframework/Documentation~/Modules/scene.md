# Scene

通过资源模块加载与卸载场景。

```csharp
await QGF.Scene.LoadSceneAsync("Room", cancellationToken);
// await 完成后可访问场景内容。
await QGF.Scene.UnloadSceneAsync("Room", cancellationToken);
```

地址来自 YooAsset Collector。Scene 以叠加方式加载，启动场景保留；切关需明确卸载旧场景。先订阅 Runtime 的成功、失败、进度事件，再请求，按 SceneAssetName 或 UserData 区分；加载完成前不要依赖场景对象。句柄由 Scene 模块维护。

加载取消会结束本次等待，底层场景完成后补偿卸载；卸载取消只结束等待，已发出的卸载仍会完成。SceneComponent 销毁时会取消尚未完成的异步等待。使用回调式 LoadScene / UnloadScene 时，仍通过 Runtime 场景事件接收结果。

[运行示例与完整脚本](../../Samples~/01_Modules/16_Scene/)。

源码：[组件](../../Runtime/Unity/Scene/SceneComponent.cs)。
