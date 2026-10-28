# YooAsset 工作流

每个项目或 Sample 维护两份资源配置：

```text
UnityQGameFramework/Resource/
  ResourceSettings.asset      QGF 运行设置
  YooAssetCollector.asset     YooAsset 官方收集配置
```

ResourceComponent 只引用 ResourceSettings；默认包、每包唯一的 Play Mode、服务器、缓存与清理策略都保存在这份资产中。Collector 保存包、分组、标签、收集路径与寻址规则。不会另建 QGF 打包 Profile，也不覆写官方配置资产 Inspector。

## 编辑器调试

1. 给 ResourceComponent 指定设置。新项目可在组件 Inspector 点击 **Use or Create Default Settings**。
2. 在 ResourceSettings 指定 Collector，选择默认包和 **EditorSimulateMode**。
3. 点击 **Open Collector** 或 `QGF > Resource > Packages`，选择自己的 Collector 配置包与地址。
4. 修改规则后使用窗口原生 **Save**。路径引用失效时才点 **Fix**，正常导入示例无需两者。

普通 Play 自动生成模拟资源。Collector 可以双击切换，不依赖一份写死的 Sample 名称列表。Builder 的包列表来自当前 Collector，删除包后同步减少。

## 运行模式

| Play Mode | 读取来源 |
| --- | --- |
| EditorSimulateMode | 编辑器直接读取模拟清单中的项目资产 |
| OfflinePlayMode | StreamingAssets 中的内置资源 |
| HostPlayMode | 内置首包、磁盘缓存和远端资源 |
| WebPlayMode | YooAsset Web 文件系统 |
| CustomPlayMode | IResourcePackageInitializer 返回官方初始化参数 |

每个包只有一个 Play Mode，编辑器与 Player 使用同一个值。框架不会自动切换；选择 Offline、Host 或 Web 后，编辑器也需要真实构建产物。Play Mode 决定读取方式，Builder 的 Build Pipeline 决定资源如何构建。

## 构建资源，再构建应用

1. 停止 Play，明确选择 OfflinePlayMode 或 HostPlayMode。
2. 从组件点击 **Open Builder**，选择目标包、平台、版本、管线和压缩。AssetBundle 使用 SBP 或 Legacy，原生文件使用 RawFile。
3. 离线应用选择完整首包复制，输出到 `Assets/StreamingAssets/yoo/<Package>`；Host 按首包策略复制，将其余产物部署到对应服务器。
4. 用 Unity 原生 Build Profiles（2022.3 为 Build Settings）构建启动场景。

资源输出在 `Bundles/<BuildTarget>/<Package>/<Version>`，模拟输出为 Simulate。Builder 的本机构建偏好由 YooAsset 按项目和包保存在 EditorPrefs；换电脑时收集规则随 Git 同步，构建偏好需重新选择。

窗口中的 Batch Build 可勾选当前 Collector 的多个包，沿用各包原生构建选项并顺序构建；失败停止剩余任务。QGF 仅集成窗口入口和多包操作，不修改 YooAsset 官方源码。

模块 `12_Resource/05_Update` 需先在场景 UpdateRemoteResources 脚本 Inspector 点击 **Build Remote Resources Example**。它准备本地 HTTP 服务的测试资源；其他普通资源示例可直接 Play。

## 自定义初始化

需要自定义加密或文件系统时，实现 IResourcePackageInitializer，在包中选择 CustomPlayMode 并填写完整类型名；返回 YooAsset 官方 InitializePackageOptions。仅通过字符串引用的类型用 Preserve 或 link.xml 防止裁剪。

也可在首次初始化前调用 `InitializePackageAsync(packageName, options, version)` 为本次启动提供参数，不改写设置资产。后续仍使用相同的[加载与归还接口](resource.md)。

默认配置位于 `Assets/UnityQGameFramework/Resource`，Sample 使用自己的同名相对目录。整体移动时保留 `.meta`，引用随资产移动；导入示例不会替换项目全局资源配置。
