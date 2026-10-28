# Scene

通过资源模块加载与卸载场景。

```csharp
QGF.Scene.LoadScene("Room");
// 等 LoadSceneSuccessEventArgs 后访问场景内容。
QGF.Scene.UnloadScene("Room");
```

地址来自 YooAsset Collector。Scene 以叠加方式加载，启动场景保留；切关需明确卸载旧场景。先订阅 Runtime 的成功、失败、进度事件，再请求，按 SceneAssetName 或 UserData 区分；加载完成前不要依赖场景对象。句柄由 Scene 模块维护。

[运行示例与完整脚本](../../Samples~/01_Modules/16_Scene/)。

源码：[组件](../../Runtime/Unity/Scene/SceneComponent.cs)。
