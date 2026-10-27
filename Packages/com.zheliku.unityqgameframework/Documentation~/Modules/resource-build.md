# YooAsset 资源构建

QGF 使用 YooAsset 3.0.6 的原生 Collector 和 Builder 窗口。每个 Sample 只需要两份资源配置：

```text
UnityQGameFramework/Resource/
  ResourceSettings.asset
  YooAssetCollector.asset
```

`ResourceSettings` 是 QGF 唯一的资源运行配置，保存默认包、每个包唯一的 `Play Mode`、服务器地址和缓存策略。ResourceComponent 只保存这份资产的引用；在组件 Inspector 展开`Edit Resource Settings`或直接选中 `ResourceSettings.asset` 都会编辑同一份数据。它在编辑器中引用 Collector，Player 不包含该引用。

每个包只有一个 `Play Mode`，编辑器与 Player 都使用同一个值，初始化参数沿用 YooAsset 官方模式：

`Play Mode` 决定读取方式，Builder 的 `Build Pipeline` 决定资源构建方式。同一平台、同一版本的构建产物可以用于 Offline 或 Host 加载。

| 模式 | 读取方式 | 用途 |
| --- | --- | --- |
| `EditorSimulateMode` | YooAsset `EditorSimulateModeOptions`，直接读取 Collector 生成的模拟文件 | 日常 Play，改表或改资源后快速验证 |
| `OfflinePlayMode` | YooAsset `OfflinePlayModeOptions`，读取 StreamingAssets 内置资源 | 不联网的 Player |
| `HostPlayMode` | YooAsset `HostPlayModeOptions`，可组合内置首包、缓存和远端服务器 | 热更新 Player |
| `WebPlayMode` | YooAsset `WebPlayModeOptions` | WebGL 文件系统 |
| `CustomPlayMode` | QGF `IResourcePackageInitializer` 返回 YooAsset 官方选项 | 自定义加密、文件系统或平台策略 |

选择 Offline、Host 或 Web 时，编辑器也会读取对应的真实资源包，需要先构建并部署资源。`EditorSimulateMode` 直接读取项目资产；构建 Player 前需明确改为 Offline、Host 或 Web 等适用模式，框架不会自动切换。自定义模式只负责返回 YooAsset 的 `InitializePackageOptions`，不修改插件源码。

`YooAssetCollector.asset` 使用 YooAsset 官方的 `BundleCollectorSetting` 类型，保存包、分组、收集目录和寻址规则。包数量增加时，在同一个 Collector 中添加 Package，无需新增构建 SO。

## 收集和构建

打开 `QGF > Resource > Packages`，在窗口顶部指定 Collector。也可以双击 Collector 资产，或在 Resource 组件 Inspector 点击`Open Collector`、`Open Builder`。这些入口打开的是同一组官方窗口。

在 Collector 中设置 Package、Group、Collector 和寻址规则，修改后用原生 `Save` 保存。原生 Builder 的包列表来自当前 Collector；压缩、管线、首包复制、缓存和加密等构建选项统一在 Builder 设置。AssetBundle 使用 SBP 或 Legacy，原生文件使用 RawFile 管线。

Builder 的构建选项由 YooAsset 按项目和包保存在本机 `EditorPrefs`；QGF 同样记住窗口中的构建版本，不再另外创建 Profile。换电脑时，收集规则随 Git 同步，本机构建偏好需要重新选择。Raw 示例第一次使用时根据官方 Raw 收集规则选择 RawFile 管线，之后遵循窗口选择。

普通示例在编辑器里自动生成 Editor Simulation，无需先构建 Bundle。模拟包输出到 `Bundles/<BuildTarget>/<PackageName>/Simulate`；发布资源输出到 `Bundles/<BuildTarget>/<PackageName>/<Version>`。

## 多包构建

原生 Builder 顶部的 `Batch Build` 展示当前 Collector 的全部 Package。勾选多个包，或点击 `Select All`，再点击 `Build Selected`。每个包仍读取其原生 Builder 保存的管线、压缩、加密和首包复制选项，按顺序执行；不需要再创建 QGF 构建 Profile。选择首包复制时，与原生单包构建一样输出到 `Assets/StreamingAssets/yoo/<Package>`。失败会显示具体错误并停止剩余构建，取消保留已完成产物。

在 ResourceSettings 指定 Collector 后，原生窗口立即使用同一配置。Collector 保存包增删后，Builder 的单包菜单、多选列表及 ResourceSettings 自动同步，不保留已移除的包。

## 构建应用

资源包构建与应用构建是两件事。YooAsset Builder 生成 Bundle、清单和内置资源；Unity Build Profiles（2022.3 为 Build Settings）生成最终应用。

离线应用的`Play Mode`选择 `OfflinePlayMode`。先在原生 Builder 选择完整首包复制，并将内置资源输出到项目 `Assets/StreamingAssets/yoo`，然后用 Unity 原生构建面板构建启动场景。联机应用可按首包规则复制必要资源，其余文件上传到配置的资源服务器。

框架不在 Resource Inspector 中提供 Windows 专用 Player 构建面板。仓库验收工具仍可调用 `ResourceBuildService.BuildPlayer` 构建独立测试应用，它不改变宿主的构建场景或 StreamingAssets。

## 运行时

```csharp
await resource.InitializePackageAsync();
GameObject prefab = await resource.LoadAssetAsync<GameObject>(location);
// 使用实例；所有实例销毁后归还这次借用。
resource.UnloadAsset(prefab);
```

跨包加载时把包名作为参数传入。需要精确所有权时使用 `LoadAssetHandleAsync<T>`，完成使用后调用句柄的 `Release()`。`LoadBinaryAsync` 返回独立的字节副本，归还资源句柄不会影响业务缓存。

Host 模式使用 `InitializePackageAsync(packageName, options)` 初始化资源包，`CreateResourceDownloader` 创建标签下载器，再由 `DownloadResourcesAsync` 开始并等待下载。下载失败转为异常，取消下载会终止该下载器；取消初始化等待不会影响其他调用者共享的包初始化。

## 自定义初始化

需要统一的自定义模式时，实现 `IResourcePackageInitializer`，在对应包中选择 `CustomPlayMode` 并填写完整类型名。初始化器应有无参数构造函数；只有字符串引用的类型需要使用 `[Preserve]` 或 link.xml 保留。

```csharp
using UnityEngine.Scripting;
using UnityQGameFramework.Runtime;
using YooAsset;

namespace MyGame
{
    /// <summary>
    /// 配置游戏资源包的自定义文件系统。
    /// </summary>
    [Preserve]
    public sealed class PackageInitializer : IResourcePackageInitializer
    {
        /// <summary>
        /// 创建使用内置文件系统的初始化选项。
        /// </summary>
        /// <param name="settings">资源包运行设置。</param>
        /// <param name="builtinRoot">内置资源目录。</param>
        /// <param name="cacheRoot">下载缓存目录。</param>
        /// <returns>自定义资源包初始化选项。</returns>
        public InitializePackageOptions CreateOptions(ResourcePackageSettings settings, string builtinRoot, string cacheRoot)
        {
            var options = new CustomPlayModeOptions();
            // 可在这里替换为带解密服务或自定义实现的官方文件系统参数。
            options.FileSystemParameterList.Add(FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(builtinRoot));
            return options;
        }
    }
}
```

业务仍调用 `await QGF.Resource.InitializePackageAsync()`，之后使用相同的 `LoadAssetAsync`、`LoadBinaryAsync`、场景和下载接口。也可以直接调用已有的 `InitializePackageAsync(packageName, options, version)` 重载，为本次启动提供官方参数；它不会改写 ResourceSettings 资产，必须在该包首次初始化前调用。

## 保存位置与原 UGF 选项

| 内容 | 唯一保存位置 | 编辑入口 |
| --- | --- | --- |
| 默认资源包、每包唯一的 Play Mode、服务器、缓存和清理间隔 | `UnityQGameFramework/Resource/ResourceSettings.asset` | 资产 Inspector；组件展开的是同一份资产 |
| 收集目录、分组、标签、寻址规则 | 同目录 `YooAssetCollector.asset` | YooAsset 原生 Collector |
| 管线、压缩、首包复制等本机构建偏好 | YooAsset 按项目和包管理的 EditorPrefs | YooAsset 原生 Builder |
| 内置首包 | `Assets/StreamingAssets/yoo/<Package>` | 构建输出，不手工编辑 |
| 下载缓存 | `persistentDataPath/QGF/Resources/<Package>` | 运行时自动维护，可选临时缓存根目录 |

默认框架配置位于 `Assets/UnityQGameFramework/Resource`；每个 Sample 在自己的根目录使用相同的相对结构。用户可在 Project 窗口移动整个目录，组件和 Collector 引用按资产标识跟随，不依赖固定 Sample 安装路径。

`ReadWritePathType`、最短和最长卸载间隔来自 UGF。读写路径现改为 ResourceSettings 内“下载缓存位置”，默认持久化，并明确区别于游戏存档；两种卸载间隔仍实际驱动清理，放在“缓存与内存清理”下。旧 UGF 的加载代理、资源池参数与版本列表不再作为组件配置项，也不会创建旧加载代理。

旧版本列表和补丁接口属于尚未迁移的 Core 兼容 API；新业务使用包清单、下载器和缓存接口。旧解密回调不参与 YooAsset 初始化，请改用官方文件系统的解密服务或上述初始化扩展。

## 资源清理

`UnloadUnusedAssetsAsync` 只清理内存中的未使用对象；`ClearCacheAsync` 清理磁盘缓存。包有活跃句柄、场景或下载操作时，QGF 会拒绝销毁包或清理缓存。

取消这两个方法的等待不会中止已启动的 YooAsset 操作；后端关闭会等这些操作完成后再销毁资源包。

场景使用 QGF 的 `SceneComponent`，内部按 Additive 加载并保存 YooAsset 场景句柄。取消场景加载时，完成后会补偿卸载。

## 示例

模块示例的资源场景分别展示：

- `01_LoadPrefab`：按含扩展名的路径加载、一个资产创建多个实例和归还时机。
- `02_AddressRules`：用文件名、文件夹名_文件名和分组名_文件名三种 YooAsset 规则生成地址，再通过同一个 QGF 加载接口读取。
- `03_Package`：按地址加载、显式资产句柄和销毁保护。
- `04_Binary`：Raw 包和独立字节片段。
- `05_Update`：按收集规则生成的地址加载、Host 包、标签下载、取消、404 失败和缓存重试。远程包的 Collector 开启 Address，使用 `AddressByGroupAndFileName`，因此 Cube 的地址为 `Base_Cube`。资源构建按钮在 `UpdateRemoteResources` Inspector 中。

这些定位方式都传给同一组 `LoadAssetAsync` 接口。GUID 需要 `IncludeAssetGUID`，地址需要 `EnableAddressable` 和地址规则；完整路径不需要去扩展名别名。示例关闭 `SupportExtensionless`，避免同目录的 Cube.prefab 和 Cube.mat 争用相同别名。

旧的 UGF ResourceCollection、ResourceBuilder、ResourcePack 和版本列表文件不再参与构建或运行。

## 第三方工具的分发

Luban 生成器使用官方发布的编译产物，属于编辑器外的工具，不随游戏运行。运行时只需要生成的数据类型与少量 Luban 读取代码。YooAsset 则保留原版 Runtime 和 Editor 源码：它依赖 Unity 编译条件、平台和编辑器扩展，直接替换为自行编译的 DLL 会增加版本、调试和平台兼容维护。包里不需要携带其完整开发工程、测试和示例。

QGF 的多配置适配单独放在 `Editor/Resource`，官方插件目录保持原样。YooAsset 3.0.6 的原生窗口使用单份静态配置缓存，QGF 通过集中在 `ResourceCollectorIntegration` 中的反射切换缓存并刷新窗口；这是版本相关的适配点，升级插件时应复核它，无需给插件源码重新打补丁。
