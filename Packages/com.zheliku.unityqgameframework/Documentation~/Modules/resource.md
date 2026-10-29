# Resource

QGF.Resource 使用 YooAsset 管理资源包、资产句柄和缓存，业务异步接口返回 UniTask。先给场景 ResourceComponent 指定 ResourceSettings，收集地址与运行模式见[资源工作流](resource-build.md)。

## 加载与归还

```csharp
var resources = QGF.Resource;
GameObject prefab = await resources.LoadAssetAsync<GameObject>(
    "Cube", cancellationToken: cancellationToken);
GameObject instance = Instantiate(prefab);

Destroy(instance);
await UniTask.NextFrame(cancellationToken); // Destroy 在帧末生效，实例先结束使用。
resources.UnloadAsset(prefab);
```

加载会自动确保资源包已初始化；显式初始化使用 `await resources.InitializePackageAsync()`。传入包名即可访问其他包，归还时使用同一个包名。一次成功加载对应一次归还，多个 Instantiate 仍是一次资产借用；取消与异常分支同样要处理已经取得的资源。

需要独立控制句柄时，用 `LoadAssetHandleAsync<T>`，最后 `Release()`；不要再对该次借用调用 UnloadAsset。`LoadBinaryAsync` 返回独立 bytes，资源句柄已由框架归还。

## 资源更新与清理

```csharp
await resources.InitializePackageAsync();
var downloader = resources.CreateResourceDownloader(new ResourceDownloaderOptions(4, 2));
// 先读取 TotalDownloadCount、TotalDownloadBytes，业务确认后再开始。
await resources.DownloadResourcesAsync(downloader, cancellationToken);
```

Host 模式先取得目标清单，再创建下载器；失败重试时重新创建下载器。DownloadResourcesAsync 的取消会终止下载；取消 InitializePackageAsync 仅取消当前等待，共享初始化不受影响。尚未开始的下载器离开业务时也应 CancelDownload。

| 接口 | 作用 |
| --- | --- |
| UnloadUnusedAssetsAsync | 清理内存中未使用的资源 |
| ClearCacheAsync | 清理磁盘下载缓存 |
| DestroyPackageAsync | 注销包，必须先归还活跃资产、场景和下载器 |

清理接口的取消只结束等待，已启动的 YooAsset 清理仍由包跟踪。Scene、Entity、UI、Config 等模块通过同一个后端加载，不维护第二份配置。

## 清单查询

`HasAsset(address)` 返回 `HasAssetResult.NotReady`、`NotExist` 或 `Exists`。清单尚未加载时返回 NotReady；Exists 只表示地址存在于清单，不表示资源包已经下载。`LoadAssetAsync` 会确保包初始化，不需要先调用 HasAsset。`QGF.Scene.HasScene` 仅在清单就绪且包含场景地址时返回 true。

## 寻址示例

| Collector 官方规则 | 示例地址 |
| --- | --- |
| AddressByFileName | Cube |
| AddressByFolderAndFileName | Art_Cube |
| AddressByGroupAndFileName | Demo_Cube |

同一预制体由三个包使用不同规则收集，运行 [02_AddressRules](../../Samples~/01_Modules/12_Resource/02_AddressRules/LoadAddressRules.cs) 切换比较。业务传入字符串地址，无需 GUID。

[全部资源示例](../../Samples~/01_Modules/12_Resource/)：资产与实例、寻址规则、包初始化、二进制、Host 更新。

## 从旧 UGF 资源接口迁移

| 已移除的接口或配置 | 当前用法 |
| --- | --- |
| SetResourceMode、SetReadWritePath、资源池与加载代理配置 | 在 ResourceSettings 中配置资源包 Play Mode 和缓存位置 |
| InitResources | await InitializePackageAsync |
| CheckVersionList、UpdateVersionList、VerifyResources、ApplyResources、UpdateResources | YooAsset 原生版本、清单、下载与缓存流程；见 Host 更新示例 |
| GetBinaryPath、LoadBinaryFromFileSystem、LoadBinarySegmentFromFileSystem | LoadBinaryAsync 返回独立 bytes；片段由业务按需要读取 |
| UGF 资源组 | 使用 YooAsset 的资源包、收集标签和下载选项 |
| 加载代理、资源池统计 | LoadingCount 与 YooAsset 原生诊断 |

框架保留 Core 与 Unity 适配边界：ResourceManager 转发加载契约，YooAsset 后端统一持有句柄。Entity、UI、Sound、Scene、Config 与 Localization 的加载回调使用同一个后端，业务无需另装 Helper 或旧版本清单序列化器。

关闭框架时，对象池先释放 UI、Entity 等实例并归还资产，资源后端再结束在途请求和销毁资源包。重载启动场景时，新实例会等待上一个后端释放同名包；已关闭的 ResourceManager 不能重新使用。

源码：[业务接口](../../Runtime/Unity/Resource/ResourceComponent.YooAsset.cs)、[YooAsset 后端](../../Runtime/Unity/Resource/YooAssetResourceBackend.cs)。
