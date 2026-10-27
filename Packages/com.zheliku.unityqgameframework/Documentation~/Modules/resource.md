# Resource

`ResourceComponent` 是 QGF 的资源入口，业务加载接口返回 UniTask；运行时资源包、资产句柄和缓存由 YooAsset 3 管理。加载预制体只会取得资产，不会创建场景实例。Entity 和 UI 会在此基础上管理实例与复用。

## 配置资源包

每个 `ResourceComponent` 显式引用一个 `ResourceSettings` 资产。配置属于游戏项目的 `Assets`，不同 Sample 使用各自的配置；导入 QGF 或 Sample 不会修改 ProjectSettings，也不会切换全局配置。

未配置的组件可在 Inspector 点击 `Use or Create Default Settings`，在 `Assets/UnityQGameFramework/Resource` 创建一份运行配置和一份官方 Collector，只赋给当前组件。默认框架 prefab 不带游戏专属配置。

`ResourceSettings` 保存默认包、各包唯一的 `Play Mode`、缓存与清理策略，编辑器中直接引用 YooAsset Collector。收集规则、分组、寻址和目录都由官方 Collector 保存，增加 Package 不需要增加 SO。日常操作在 Resource 组件 Inspector 与 YooAsset 窗口完成。

每个包只有一个 `Play Mode`，直接使用 YooAsset 的 `EPlayMode`，编辑器与 Player 都按同一个值初始化。示例默认使用 `EditorSimulateMode`；构建离线应用前改为 `OfflinePlayMode`，联机应用选择 `HostPlayMode`。模拟模式只能在编辑器运行，框架不会根据运行环境自动改换模式。旧版双字段配置迁移时保留原 Player 模式，原编辑器覆盖值不再参与运行。

`YooAsset Collector` 是包名称的来源。拖入另一份 Collector 后，包列表和 Default Package 立即同步，已经打开的 Collector、Builder 也切换到该资产；在原生 Collector 保存包增删后，运行设置自动跟随。同名包的模式和服务器设置保留，不重复维护包名称。

Host 包默认使用 `StreamingAssets` 首包作为内置资源。全部资源都从服务器下载时，在该包设置中关闭 `Use Builtin Files`；此时不创建内置文件系统，也不要求 `BuiltinCatalog.bytes`。Offline 模式仍需要部署完整的内置包。

## 加载与归还

业务从 `await LoadAssetAsync<T>` 开始，成功返回可用资产，失败抛出异常；取消令牌用于结束当前加载。未初始化时自动初始化默认包，不需要在每次加载前手动初始化。

```csharp
// 字符串可以是 Collector 生成的地址，也可以是完整 Assets 路径。
GameObject prefab = await QGF.Resource.LoadAssetAsync<GameObject>("Resource_Cube",
    cancellationToken: destroyCancellationToken);
GameObject instance = Instantiate(prefab);
// 使用完成后先销毁实例；Destroy 在帧末生效。
Destroy(instance);
await UniTask.NextFrame(destroyCancellationToken);
QGF.Resource.UnloadAsset(prefab);
```

| API | 返回内容 | 释放责任 |
| --- | --- | --- |
| `LoadAssetAsync<T>` | 资产对象 | 每次加载对应一次 `UnloadAsset(asset)` |
| `LoadAssetHandleAsync<T>` | YooAsset `AssetHandle` | 调用方执行 `handle.Release()` |
| `LoadBinaryAsync` | 独立的 `byte[]` | 内部句柄自动释放，调用方持有字节数组 |

两种资产返回形式都使用同一资源后端，分别适合普通业务和需要精确句柄所有权的场合。不要对显式句柄再调用 `UnloadAsset`。加载预制体与创建实例是两个操作；资产借用应覆盖所有实例的使用期。
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

旧 UGF 回调和版本列表 API 保留在兼容层，并从常用代码提示中隐藏。它们供既有调用过渡；新业务使用本页的 UniTask、原生下载器及缓存接口。Entity、UI、Config 等模块的内部回调同样进入 YooAsset 后端，不维护第二份资源或配置。

`AssetCount` 是清单资产数，`LoadingCount` 是当前请求数，`PlayMode` 是默认包配置的唯一运行模式。显式传入 `InitializePackageOptions` 时，以本次初始化参数为准。需要完整清单时可查询 `InitializePackageAsync` 返回的 YooAsset `ResourcePackage`。
[Resource 示例](../../Samples~/01_Modules/12_Resource/) 包含路径加载、三种寻址规则、资源包初始化、二进制读取和 Host 下载。每个 Sample 的 `ResourceSettings` 和 YooAsset 原生 `BundleCollectorSetting` 独立维护；运行前按 [资源构建说明](resource-build.md) 构建所需资源包。

源码：[ResourceComponent](../../Runtime/Unity/Resource/ResourceComponent.cs)、[YooAsset 接口](../../Runtime/Unity/Resource/ResourceComponent.YooAsset.cs)、[YooAsset 后端](../../Runtime/Unity/Resource/YooAssetResourceBackend.cs)、[ResourceManager](../../Runtime/Core/Resource/ResourceManager.cs)。

## 设计依据

- [GodotGameFramework 资源系统](https://github.com/nuoyanruoshui/GodotGameFramework/blob/af1edfaf93602176bce8a3401125f2a41c6779fb/Godot/docs/ResourceSystem.md)：借鉴桥接引擎资源能力、缩小业务入口的职责划分。其 Godot 线程任务和 PCK 清单不用于 Unity 的 YooAsset。
- [Addressables 异步加载](https://github.com/needle-mirror/com.unity.addressables/blob/99d59c9b2a39024617b347953389ca631517bd2b/Documentation~/load-wait-asynchronous-async-await.md)：借鉴异步任务与资产生命周期分开处理。QGF 使用 UniTask 兼容 Unity 2022.3、6.0 和 6.3。
- [YooAsset 官方项目](https://github.com/tuyoogame/YooAsset)：包、寻址、依赖、构建和文件系统使用原生接口，QGF 只处理框架接线、取消和借用归还。
