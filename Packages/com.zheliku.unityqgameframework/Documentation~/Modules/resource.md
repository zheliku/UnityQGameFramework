# Resource

`ResourceComponent` 是 QGF 的资源入口，保留常用的 UGF 加载回调；运行时资源包、资产句柄和缓存由 YooAsset 3 管理。加载预制体只会取得资产，不会创建场景实例。Entity 和 UI 会在此基础上管理实例与复用。

## 配置资源包

每个 `ResourceComponent` 显式引用一个 `ResourceSettings` 资产。配置属于游戏项目的 `Assets`，不同 Sample 使用各自的配置；导入 QGF 或 Sample 不会修改 ProjectSettings，也不会切换全局配置。

未配置的组件会在 Inspector 中提示。可以在该组件上创建配置，文件默认放在 `Assets/UnityQGameFramework/Resource`，并同时创建默认包的 `ResourceBuildProfile`。创建配置只会赋给当前组件。默认框架 prefab 不带游戏专属配置，场景实例或项目 prefab 需要自行指定。

`ResourceSettings` 保存默认包名称、编辑器模拟开关和各包运行设置。每个包需要对应的 `ResourceBuildProfile`，其中维护 YooAsset 收集规则、版本和构建类型。收集目录以 Unity 资产引用保存，移动目录后仍可定位。

编辑器模拟使用该包的 Profile 构建 YooAsset 模拟包。Player 根据包设置使用 Offline 或 Host 模式。Profile 的 `IncludeAssetGUID` 启用时，QGF 可用资产 GUID 作为定位地址；Sample 会直接展示这种不依赖目录路径的调用。

Host 包默认使用 `StreamingAssets` 首包作为内置资源。全部资源都从服务器下载时，在该包设置中关闭 `Use Builtin Files`；此时不创建内置文件系统，也不要求 `BuiltinCatalog.bytes`。Offline 模式仍需要部署完整的内置包。

## 加载与归还

回调式 `LoadAsset` 适合延续 UGF 的事件写法。每次加载都会形成一次资产借用，使用结束后用 `UnloadAsset` 归还。创建的 GameObject 实例由业务负责销毁，归还预制体资产前应先销毁所有使用它的实例。

```csharp
resources.LoadAsset(prefabGuid, typeof(GameObject), new LoadAssetCallbacks(
    OnLoaded,
    OnFailed));
```

资源模块还提供 UniTask 接口：

- `LoadAssetAsync<T>` 返回资产并登记一次隐式借用，使用后调用 `UnloadAsset`。
- `LoadAssetHandleAsync<T>` 返回独立的 `AssetHandle`，调用者在使用结束后释放该句柄。
- `LoadBinaryAsync` 返回独立的字节数组，读取完成后自动归还内部句柄。

多包加载时传入 `packageName`。同一资源的多次借用各自持有句柄，归还其中一次不会释放其他使用者的借用。

## 包生命周期

`InitializePackageAsync` 初始化指定包并加载清单。取消标记只取消当前等待；并发初始化共享同一个包操作。Host 模式可传入 YooAsset 的初始化选项，再用 `CreateResourceDownloader` 获取标签下载器。

下载前可读取下载器的 `TotalDownloadCount` 和 `TotalDownloadBytes`，向玩家确认后调用 `DownloadResourcesAsync`。任务完成表示下载成功；失败抛出异常；取消标记会终止该下载器。进度可从下载器属性或 `DownloadProgressChanged` 事件取得。离开界面前，尚未开始的下载器也应调用 `CancelDownload`，释放其在途登记。

```csharp
var downloader = resources.CreateResourceDownloader(new ResourceDownloaderOptions(4, 2));
await resources.DownloadResourcesAsync(downloader, cancellationToken);
```

重试时重新创建下载器，只会包含当前清单中仍未完成的文件。不要重复启动同一个失败的下载器，也不要用旧 UGF 的版本清单或 `ResourcePack` 接口更新 YooAsset 包。

`UnloadUnusedAssetsAsync` 清理内存中未使用的资产；`ClearCacheAsync` 清理磁盘缓存；`DestroyPackageAsync` 注销资源包。三种操作含义不同，销毁包前必须归还资产、场景和下载器等活跃资源。

取消 `UnloadUnusedAssetsAsync` 或 `ClearCacheAsync` 只会取消当前等待，不会中止已启动的 YooAsset 清理操作。操作仍由资源包跟踪；销毁资源包或关闭后端时，QGF 会等待清理完成。

场景资源通过 `SceneComponent` 加载和卸载，由场景模块维护已加载场景及其 YooAsset 句柄。配置与 Luban 数据表仍通过各自模块读取，底层资产加载由同一个 Resource 后端完成。

## 示例

[Resource 示例](../../Samples~/01_Modules/12_Resource/) 包含预制体加载、资源包初始化、二进制读取和 Host 下载。每个示例的 `ResourceSettings` 和 `ResourceBuildProfile` 独立维护；运行前按 [资源构建说明](resource-build.md) 构建所需资源包。

源码：[ResourceComponent](../../Runtime/Unity/Resource/ResourceComponent.cs)、[YooAsset 接口](../../Runtime/Unity/Resource/ResourceComponent.YooAsset.cs)、[YooAsset 后端](../../Runtime/Unity/Resource/YooAssetResourceBackend.cs)、[ResourceManager](../../Runtime/Core/Resource/ResourceManager.cs)。
