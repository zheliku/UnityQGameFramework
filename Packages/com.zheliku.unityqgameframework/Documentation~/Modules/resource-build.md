# YooAsset 资源构建

QGF 的资源构建由 YooAsset 3.0.6 完成。每个示例维护自己的 `ResourceSettings` 和 `ResourceBuildProfile`，构建期间只在内存中替换 YooAsset 收集器，不保存或覆盖宿主项目的全局收集器。

## 配置位置

```text
Assets/Examples/02_StarForce/UnityQGameFramework/Resource/
  ResourceSettings.asset
  Editor/ResourceBuildProfile.asset
```

`ResourceSettings` 是运行时配置，包含默认包、编辑器模拟开关、运行模式、服务器地址和版本。`ResourceBuildProfile` 是编辑器收集配置，包含包名、构建版本、AssetBundle／Raw 类型和收集目录。目录使用 GUID 保存，移动示例后仍能定位资产。

未配置的 ResourceComponent 可在 Inspector 中点击 `Create Resource Settings`。QGF 会在 `Assets/UnityQGameFramework/Resource` 创建运行配置和默认包的构建 Profile，并只将配置赋给当前组件；不会改动 ProjectSettings 或其他 Sample。资源配置属于 Assets，代码和构建菜单属于 Package。

## 构建

选中 `ResourceBuildProfile.asset`，使用以下菜单之一：

- `QGF > Resource > Build Editor Simulation`：生成 EditorSimulate 清单。
- `QGF > Resource > Build Player Package`：生成 AssetBundle 或 Raw 包。
- `QGF > Resource > Build Selected Profile`：根据 Profile 的设置选择构建方式。

输出位于 `QgfBuild~/<Profile GUID>/S`（模拟）或 `P`（实际包），首包副本位于 `QgfBuild~/<Profile GUID>/B/<PackageName>`，由 Profile 的 GUID 隔离，不写入 `Assets/StreamingAssets`。实际 Player 构建会把首包目录部署到 Player 的 `StreamingAssets/yoo/<PackageName>`，其中包含 `BuiltinCatalog.bytes` 和内置 Bundle。示例使用 `PackByGuid` 将每个资产单独打包，目录移动不改变包名，也减少 Windows 构建缓存的路径长度。

## 运行时

```csharp
await resource.InitializePackageAsync();
GameObject prefab = await resource.LoadAssetAsync<GameObject>(location);
// 使用实例；所有实例销毁后归还这次借用。
resource.UnloadAsset(prefab);
```

跨包加载时把包名作为参数传入。需要精确所有权时使用 `LoadAssetHandleAsync<T>`，完成使用后调用句柄的 `Release()`。`LoadBinaryAsync` 返回独立的字节副本，归还资源句柄不会影响业务缓存。

Host 模式使用 `InitializePackageAsync(packageName, options)` 初始化资源包，`CreateResourceDownloader` 创建标签下载器，再由 `DownloadResourcesAsync` 开始并等待下载。下载失败转为异常，取消下载会终止该下载器；取消初始化等待不会影响其他调用者共享的包初始化。

## 资源清理

`UnloadUnusedAssetsAsync` 只清理内存中的未使用对象；`ClearCacheAsync` 清理磁盘缓存。包有活跃句柄、场景或下载操作时，QGF 会拒绝销毁包或清理缓存。

取消这两个方法的等待不会中止已启动的 YooAsset 操作；后端关闭会等这些操作完成后再销毁资源包。

场景使用 QGF 的 `SceneComponent`，内部按 Additive 加载并保存 YooAsset 场景句柄。取消场景加载时，完成后会补偿卸载。

## 示例

模块示例的资源场景分别展示：

- `01_LoadPrefab`：回调加载、一个资产创建多个实例和归还时机。
- `02_Package`：包初始化、显式资产句柄和销毁保护。
- `03_Binary`：Raw 包和独立字节片段。
- `04_Update`：Host 包、标签下载、取消、404 失败和缓存重试。

旧的 UGF ResourceCollection、ResourceBuilder、ResourcePack 和版本列表文件不再参与构建或运行。
