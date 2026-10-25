# QGF 现代工具集成与 YooAsset 替换方案

日期：2026-10-24。本文记录实施前的审查和设计；当前实施进度见第 13、14 节。

## 1. 当前基线与验证范围

实施前框架提交：`e249a7efa97e6195383cc4dad34d7d0db47c9db1`，包版本 `0.1.0-exp.8`。本节记录迁移前的源码和验证范围。

开发源与发布目录保持同构，逐文件比较通过：

| 开发目录 | Package Sample | 文件数 |
| --- | --- | --- |
| Assets/Examples/00_PointPointPoint | Samples~/00_PointPointPoint | 52 |
| Assets/Examples/01_Modules | Samples~/01_Modules | 501 |
| Assets/Examples/02_StarForce | Samples~/02_StarForce | 564 |

验证证据保存在 `Artifacts~/ModernIntegration`；较早的完整模块报告在 `Artifacts~/ExamplesAlignment`。

- 6.3.24f1：59 个模块场景检查通过。未包含专门操作断言的场景只算启动和绑定检查。
- 2022.3.62f3 / URP 14.0.12、6.0.70f1 / URP 17.0.4：重新编译当前三个 Sample，检查 59 个模块场景、284 个控件引用，以及 66 个场景、31 个预制体的脚本和资源归属。
- 两个版本均重新操作四个数据表示例和对象池工厂示例；StarForce 菜单、Luban 表、TOML 配置正常；点点点流程和 UniTask 下一帧、非缩放延迟、销毁取消通过。
- 6.3：重新检查点点点三种结局、记录保存、UI 复用和原生鼠标输入；StarForce 两轮战斗、鼠标飞行、设置、语言重启通过。
- 6.3 新消费工程：实际从公开 Git URL 安装上述提交，PackageSource 为 Git，注册三个 Sample；当前示例源码与包内副本一致，四个表场景、工厂场景、StarForce 启动、点点点流程和 UniTask 检查通过。此检查没有把三个 Sample 的移动、卸载验收一起算入。
- 包结构、文档链接、UniTask 上游文件哈希、Excel 文件归档和 Sample 同步检查通过。
- 已修正六个维护脚本中遗留的点点点命名空间；它们应引用 `UnityQGameFramework.Examples.PointPointPoint`。

本轮跨版本检查主要是 Editor 和运行模式检查。不能据此宣称 Android、iOS、WebGL、IL2CPP Player 或全部资源热更新路径均已重新验证；YooAsset 接入后必须另设这些验收。

自动化环境记录：新建的 6.3 无界面消费工程缺少 `UserSettings/Search.index`，UnityEditor.Search 在启动索引时抛 ArgumentOutOfRangeException，首次运行因此失败。保留失败日志和报告，仅在隔离工程补入正常 Editor 的默认索引配置后复核；原有错误捕获保持不变，最终运行无该异常。没有为此修改 QGF、Sample 或用户主工程设置。

## 2. 参考来源及适用边界

| 来源 | 核实内容 |
| --- | --- |
| [GameFramework-Next](https://github.com/Alex-Rachel/GameFramework-Next/tree/4f2fbfa6fc0eb0bfa55c92fd51df295eae3de4b3) | 实际源码中的 ResourceManager、AssetObject、ConfigSystem、异步扩展和启动流程 |
| [UGF 教程](https://share.note.youdao.com/ynoteshare/index.html?id=67c0d8a6b20893d82e3fe990cca802c2&type=note) | Resource、Scene、DataTable、Config、Localization 的职责与替换建议；正文已有本地缓存 `Artifacts~/UgfReference/reference.txt` |
| [YooAsset 3.0.6](https://github.com/tuyoogame/YooAsset/tree/3.0.6) | 当前官方标签、package.json、初始化、句柄、下载器、原生文件和编辑器收集器 |
| [UniTask 2.5.11](https://github.com/Cysharp/UniTask/tree/2.5.11) | 当前内置版本；357 个上游文件通过打包器哈希检查 |
| [Luban 5.1.0](https://github.com/focus-creative-games/luban/tree/v5.1.0) | 当前工具链版本 |

GameFramework-Next 内置 YooAsset 2.1.1、UniTask 2.5.4。借鉴它的模块分工，不直接搬用其旧版 API、目录布局或整个异步扩展。

官方 YooAsset 3.0.6 的包声明最低 Unity 2019.4；这只是声明。3.0.6 作为首选验证版本，须先在 2022.3、6.0、6.3 实测后锁定。

## 3. Luban：保留已经完成的部分，补齐资源与异步衔接

现有流程：Excel schema / 数据 → Luban → 生成 C# 与 bytes → Catalog → DataTableComponent → 生成表对象与跨表引用 → 原子登记。

目前已经有：

- Assets 下的可移动工作区、Profile、全局选择和菜单生成。
- schema hash 校验、剩余字节检查、整批登记冲突检查。
- Catalog 同步读取、通过 Resource 异步读取、取消序号、迟到结果释放和整组卸载。
- 强类型表查询、筛选，以及 Luban 生成表的原生索引能力。

可借鉴 Next 的内容：使用资源模块读取生成数据、由生成的 Tables 完成表构造和引用解析。我们的 Profile 与 Catalog 已经比它依赖工程外 bat、同步单例懒加载的流程更适合插件发布，继续保留。

下一步调整：

1. 增加 `LoadDataTablesAsync<T>`，返回 `UniTask<T>`，支持 CancellationToken；同步入口与原事件入口保留。
2. 同一套请求记录完成成功、失败或取消。不能只等待事件，也不能把表名作为并发请求的唯一标识。
3. 第一阶段仍加载 Catalog 及其 bytes 依赖，减少迁移面。Catalog 是资源引用和版本元数据，不是替代生成 C# 的数据解析器。
4. 字节读取与解析完成后释放加载句柄；表对象不得继续依赖已释放的 Unity 资产或原生文件缓冲区。
5. 后续大项目可以按 Catalog / package 拆分表集；不在这一轮强加逐表懒加载。
6. 明确区分 schema 版本与资源内容版本。同 schema 的数据更新可以热更；新增字段、表类型或枚举涉及生成 C#，需要重新编译或代码热更新，不能只替换 bytes。

Config 和 Localization 仍使用 TOML。二者通过 Core 的 DataProvider 读取资源，受到 Resource 替换影响，并不依赖 DataTable。

## 4. UniTask：接到请求生命周期，而不是只替换等待写法

保留内置 UniTask 2.5.11。Runtime 程序集增加对 `UniTask` 的明确引用；Core 保持无 Unity 和 UniTask 引用。

优先提供资源、表集、场景的 UniTask 入口，再按需要补 UI / Entity。网络与下载模块独立审查，不在一次提交中全改。

- 每个请求独立完成源与取消登记，成功、失败、取消只完成一次。
- 使用已取消的 token 时不创建加载请求；缓存命中也检查 token。
- 取消等待不等于底层共享下载或加载已经终止。只取消本请求的借用，不影响其他调用者；迟到完成仍回收其句柄。
- 场景加载发出后若无法中止，取消应负责完成后的补偿卸载；加载期间不能直接丢弃 SceneHandle。
- 框架退出、包销毁和禁用 Domain Reload 时，清空待完成请求和登记，避免上一轮 Play 污染下一轮。
- 业务取消抛出 OperationCanceledException，加载失败提供明确异常；原回调仍按其契约报告失败。

Next 的通用 AwaitableExtensions 仍混有 TaskCompletionSource 和全局静态字典，部分成功处理被注释；不适合作为整体基础。它的资源取消分支存在直接返回 null、未在该分支释放新句柄的源码风险，不照搬。

YooAsset 3 原生支持 await，但 OperationAwaiter.GetResult 明确不因业务失败抛异常，等待后仍须检查 Status 和 LastError。官方 UniTask Sample 使用部分 UniTask 内部接口；QGF 优先使用公开 API 做最小适配，避免为此修改 UniTask 源码或导入整个官方示例。

## 5. Resource 的目标结构

保留 `QGF.Resource`、ResourceComponent 场景节点、IResourceManager 及常用回调风格。底层只由 YooAsset 管理资源包、Bundle 依赖、版本、下载和缓存。

```text
游戏代码 / UI / Entity / Sound / Config / Localization / DataTable / Scene
                                ↓
                       ResourceComponent
                                ↓
             Core ResourceManager / IResourceManager
                                ↓
              重写后的 IResourceHelper 边界
                                ↓
                   Runtime 的 YooAsset 实现
```

原因：Core.asmdef 的 `noEngineReferences` 为 true，FrameworkEntry 按约定创建 Core.Resource.ResourceManager，FrameworkModule 也是 internal。只在 Runtime 新建一个同接口类不会自动替换现有模块。

采用上述结构可以保留模块入口和 Core 的独立性，不需要为这次替换增加模块注册框架。复用资源 Helper 的现有职责边界，重写其契约；YooAsset 类型不进入 Core。

资产登记条目保存 Target 与不透明句柄，真正释放时交由 Runtime Helper 处理。借鉴 Next，只按需要保留一层 QGF 资产缓存；删除旧 Bundle 资源池，Bundle 生命周期交给 YooAsset。UI / Entity 自己的实例池继续保留，它们解决的是实例复用。

加载键至少包括 package、location、assetType。默认包的旧 `UnloadAsset(object)` 继续可用；多包入口须携带包或明确句柄，不能仅凭 object 反查猜测归属。

建议的开发者入口如下，均为待实施接口：

| 用途 | QGF 入口 |
| --- | --- |
| 包生命周期 | CreatePackage / InitializePackageAsync / DestroyPackageAsync |
| 常规加载 | LoadAsset 回调接口、LoadAssetAsync<T> / UnloadAsset |
| 复杂所有权 | 显式加载句柄入口；跨包和共享请求按独立借用释放 |
| 表集 | LoadDataTablesAsync<T> / GetDataTable<T> / UnloadDataTables |
| 场景 | QGF.Scene.LoadSceneAsync / UnloadSceneAsync，内部持有 SceneHandle |
| 更新 | RequestPackageVersionAsync / LoadPackageManifestAsync / 下载器 |
| 清理 | UnloadUnusedAssetsAsync / ClearCacheAsync，分别清内存与磁盘 |

普通业务仍从 QGF.Component 入口调用；需要下载器细节时可以使用 YooAsset 的原生操作类型，不再平行发明一整套下载状态机。具体重载先用一个真实示例验证，再确定签名，避免一次添加大量重复 API。

## 6. 保留、替换与删除范围

| 当前内容 | 处理 |
| --- | --- |
| ResourceComponent、QGF.Resource、资源回调与 UserData | 保留入口、脚本 GUID 和必要语义，内部重新实现 |
| LoadAsset / UnloadAsset | 对接 AssetHandle；提供 UniTask 泛型入口，明确借用与归还 |
| LoadScene / UnloadScene | 对接 SceneHandle；Scene 模块继续维护场景集合、事件和主摄像机状态 |
| LoadBinary | 适配原生文件读取或 TextAsset；返回拥有独立生命周期的 bytes |
| LoadBinaryFromFileSystem / segment 方法 | 审查 DataProvider 与示例调用后删除或迁移；不模拟不存在的 UGF 文件容器 |
| InitResources、资源模式和版本检查 | 改为包创建、初始化、版本请求、清单加载、下载、缓存清理 |
| Core 的 ResourceIniter / Checker / Updater / Verifier / ResourceLoader | 新链路验收后移除旧实现与只供其使用的类型 |
| 旧资源清单序列化器、ResourcePack、变体和异或格式 | 移除；迁移到 YooAsset 包、标签、版本、原生文件及解密服务 |
| ResourceCollection / ResourceBuilder 编辑器工具与 TOML 文件 | 替换为 YooAsset 收集与构建；不继续维护两套构建器 |
| EditorResourceComponent 与编辑器直读模式 | 已移除组件、Inspector、prefab / scene 引用和 EditorResourceMode；默认资源配置由组件 Inspector 显式创建 |
| Resource Inspector、Debugger 与生成菜单 | 改为包状态、请求、版本、下载进度、句柄与缓存信息 |
| Download | 保留通用下载模块；YooAsset 的 Bundle 下载由它自己负责 |
| FileSystem | 保留独立文件系统；只移除旧 Resource 对它的私有依赖 |
| Config / Localization / DataTable | 改资源读取和异步衔接；保留 TOML / Luban 数据格式与查询职责 |

不承诺所有旧 UGF Resource API 都能原样映射。旧清单、ResourcePack 和文件容器语义没有等价物时，直接更新框架、示例和文档，避免空实现或伪造成功。

## 7. YooAsset 3 API 与清理语义

首选使用原生 3.x API，不走 Compatibility 目录的弃用方法：

| 能力 | 官方 3.0.6 入口 |
| --- | --- |
| 创建包 | YooAssets.CreatePackage |
| 初始化包 | ResourcePackage.InitializePackageAsync + 对应 Options |
| 版本与清单 | RequestPackageVersionAsync / LoadPackageManifestAsync |
| 加载资产 | LoadAssetAsync / LoadAssetSync |
| 加载场景 | LoadSceneAsync；通过 SceneHandle.UnloadSceneAsync 卸载 |
| 原生文件内容 | LoadAssetAsync<RawFileObject>，GetBytes / GetText |
| 下载 | CreateResourceDownloader + ResourceDownloaderOptions / BundleDownloaderOptions |
| 内存清理 | 释放借用，再调用 UnloadUnusedAssetsAsync |
| 磁盘缓存清理 | ClearCacheAsync + ClearCacheOptions |
| 销毁包 | 等待 DestroyPackageAsync，再从 YooAssets 移除登记 |

3.x 的旧 GetRawFileData / GetRawFileText 兼容方法会抛 NotSupportedException。不能按 Next 的 2.1.1 接法直接搬过来。

资产卸载、内存清理、磁盘缓存删除、包销毁分别提供明确操作。不能用一个 Clear 同时清掉四者；包存在活跃加载、场景、UI 或实体借用时，先拒绝销毁或按明确的关闭顺序排空。

## 8. Assets、Package 与收集配置

框架包内放代码、编辑器、默认模板、第三方来源记录与三个 Samples~；用户资产和可编辑配置放 Assets。构建产物进入 QgfBuild~，工具缓存进入 Library，包缓存使用 persistentDataPath 下的独立包目录。

建议默认工作区：

```text
Assets/UnityQGameFramework/
  Config/                         # 现有 Luban 工作区
  Resource/
    ResourceSettings.asset        # 可用于 Player 的运行配置
    Editor/
      ResourceBuildProfile.asset  # 编辑器收集与构建配置
```

路径使用资产引用 / GUID，用户可以移动；不把开发机绝对路径或 Git 包缓存路径写进运行配置。ResourceComponent 可以选择本场景配置，也可以显式更新项目默认配置；导入 Sample 不切换全局选择。

Sample 自己带配置，包名分别使用 QgfExamples.Modules、QgfExamples.PointPointPoint、QgfExamples.StarForce，避免默认包互相覆盖。包名、资源 location 和磁盘缓存目录是不同概念。

**收集器单例限制必须先解决：**3.0.6 的 SettingLoader 会全项目 FindAssets，出现多份 BundleCollectorSetting 就抛异常；官方构建任务也读取这个静态全局设置。因此，不能给三个 Sample 各放一份该类型资产后声称互相独立。

建议 QGF 的 ResourceBuildProfile 保存官方收集规则的数据，以显式 Profile 构造本次构建所用的临时 collector。先做最小编辑器验证，再选择限定作用域的官方设置覆盖入口或可替换构建任务；不可反射修改私有静态字段，也不可临时改写并保存用户的 collector。

如果官方入口需要扩展，保留一处明确、可审计的编辑器补丁，以 try/finally 恢复原状态，并在第三方来源文件中记录差异。该入口验证前，不承诺多 Profile 编辑器协同已经完成。

## 9. 依赖与 Git URL 分发

YooAsset 替换完成后是 Resource 的必需实现。Alchemy、Rainbow Folders 仍为可选扩展，二者不应成为资源加载的依赖。

按现有用户偏好，默认分发继续采用 Plugins 内置源码和版本锁定，附 LICENSE、来源 commit / tag、哈希与必要补丁。候选 YooAsset 3.0.6；UniTask 2.5.11 不为这次迁移降级。

Unity 的 package.json 不能可靠地把任意 Git URL 当作普通传递依赖。仅写 `com.tuyoogame.yooasset: 3.0.6`，也不意味着默认 Unity Registry 就能解析它；不能把缺少包的安装过程交给用户猜测。

已安装官方 UniTask / YooAsset 的消费项目存在同名程序集风险。必须单独验证外部依赖模式；不能未经实测就宣称 versionDefines / defineConstraints 能自动解决所有重复程序集。默认内置包和外部依赖包的去重机制属于发布门槛，不能留到文档里让用户手工删除 PackageCache。

优先保持第三方源文件可与上游逐文件核对。若要支持外部依赖分发，可由同一源目录构建排除内置第三方的变体，或后续提供可解析的包仓库；不分别手写两套框架代码。

## 10. Sample 调整

仍然只有三个导入项，所有修改先落在 Assets/Examples，再原样同步到 Samples~。

| 示例 | 修改重点 |
| --- | --- |
| 01_Modules/11_DataTable | 保留同步、查询、筛选；异步示例用 UniTask，增加取消与失败重试；展示 Catalog 走资源包 |
| 01_Modules/12_Resource | 收敛为包初始化、资产加载归还、按标签下载和重试、多包、内存与磁盘清理几组互不重复的示例 |
| 01_Modules/16_Scene | 使用 YooAsset 场景句柄，演示切换、取消后补偿卸载及主摄像机恢复 |
| Config / Localization | 保持 TOML，验证 EditorSimulate 与 Offline / Host 下相同查询结果 |
| UI / Entity / Sound | 验证预制体、音频引用；销毁中取消、实例回池和资源归还 |
| 00_PointPointPoint | 最小完整链路：包初始化 → TOML → UI → 游戏流程 → 返回菜单与记录保存 |
| 02_StarForce | 完整流程：初始化包 → 版本与清单 → 可选下载 → Luban / TOML → 菜单与战斗 |

不把每个示例都写成下载器或万能加载器。示例直接调用 QGF，展示必要的句柄 / 资产归还和取消；不为两行调用再套业务包装方法。

## 11. 热更新预留

第一轮替换不同时安装 HybridCLR。资源流程预留以下顺序：初始化和更新资源 → AOT 元数据 → 热更 DLL 依赖顺序加载 → 调用热更入口 → 读取 / 实例化挂载热更脚本的 Scene / Prefab。

DLL / AOT 文件按原生文件进入专用包或标签，保持 QGF 与启动程序集在 AOT 侧。挂载热更脚本的资产必须打包；只有 DLL 放进包并不等于可以加载对应 prefab。具体元数据列表、裁剪、程序集引用和 IL2CPP 构建归入后续 HybridCLR 接入验收。

## 12. 实施顺序与通过条件

1. **独立技术验证**：在 Artifacts~ 验证 YooAsset 3.0.6、三个 Unity 版本、UniTask 桥接、collector 配置隔离和重复依赖处理。此阶段不替换正式 Resource。
2. **资源主链路**：重写 Manager / Helper，保留 ResourceComponent 与普通加载回调；完成包初始化、AssetHandle、SceneHandle、原生文件、取消和退出清理。
3. **上层模块与 Luban**：迁移 DataProvider、Config、Localization、DataTable、Scene、UI、Entity、Sound；补 UniTask 接口及现有示例。
4. **更新与构建**：版本、清单、标签下载、断网、重试、缓存校验与清理；接入构建 Profile、Inspector、Debugger 和打包菜单。
5. **移除旧代码并发布**：全项目无旧资源构建器与私有清单依赖，更新文档、CHANGELOG、包版本并生成分发包。

每个阶段应检查：

- 一份资源多次借用、类型或包不同但 location 相同、缓存命中、同步与异步交错。
- 请求发出前取消、加载中取消、取消后立即重试、迟到成功、失败及框架退出。
- 活跃 UI / Entity / Sound / Scene 不因其他请求归还而被误释放；销毁控制器不留下活跃借用。
- EditorSimulate、Offline、Host 的正常路径与 HTTP 404、断网、重试、旧版本回退、损坏缓存。
- 2022.3、6.0、6.3 编译和运行；Windows Player / IL2CPP；Android 本地包读取和 WebGL 异步路径。
- Git URL 新项目、三个 Sample 单独和同时导入、移动目录、卸载；不修改用户渲染管线、Input Handling、Build Settings 或全局 Profile。
- Assets 与 Samples~ 原样一致，资源构建和表生成幂等，PackageCache 无写入，第三方来源与许可证完整。

阶段一优先解决 collector 单例和依赖去重，再开展大规模代码替换。这样能先确定插件分发与多 Sample 独立性，不把安装问题拖到资源模块完成以后。

## 13. 本轮已落地

- YooAsset 3.0.6（commit `3b4cfb36cc2e81b9558ab74d65fee2cf2eac2804`）已内置到 `Runtime/Plugins/YooAsset`，上游 Samples 不进入包；SBP 2.1.4 作为 Unity 包依赖。源文件和 Unity 导入后可变的 `.meta` 字段均有锁定检查。
- 新增 `ResourceSettings`、`ResourcePackageSettings`、`ResourceBuildProfile`、`ResourceBuildService` 和 QGF Resource 菜单。构建期间只替换 YooAsset 收集器的内存对象，结束后恢复原设置，不保存或覆盖用户的 `BundleCollectorSetting`。
- 新增内部 `ResourceOperation`，统一 UniTask 等待、取消、失败和句柄释放；继续使用 ResourceComponent 作为公开入口，不另设平行的 YooAssetPackage 包装。
- 2022.3.62f3、6000.0.70f1、6000.3.24f1 均通过 13 项隔离探针：三个独立 Profile、目录移动、两个包同路径加载、句柄独立释放、RawFileObject、包销毁，以及取消等待和取消后句柄释放。
- 依赖探针确认同时安装官方 UniTask/YooAsset 与内置副本会产生重复程序集；本包默认采用内置副本，外部模式必须在打包时排除对应内置目录。
- 已接通 ResourceComponent → Core ResourceManager → YooAssetResourceBackend。三个 Sample 均有独立 ResourceSettings / ResourceBuildProfile，并已迁移启动与资源示例，Assets 和 Samples~ 内容一致。点点点、Modules 的 QGF prefab 直接引用设置；StarForce Launcher 通过 prefab override 引用 StarForce 设置。EditorResourceComponent、EditorResourceMode、专用 Inspector 和 prefab / scene 引用已移除；默认 QGF prefab 保持项目配置为空，ResourceComponent Inspector 可在 Assets 下显式创建 ResourceSettings 与 GUID 收集 Profile，不改 ProjectSettings 或其他 Sample。
- 资源入口增加 InitializePackageAsync、LoadAssetAsync、LoadAssetHandleAsync、LoadBinaryAsync、UnloadUnusedAssetsAsync 和 DestroyPackageAsync。隐式借用按包归还，显式借用由调用者释放句柄；包有请求、场景、资产或清理操作时拒绝销毁。
- 初始化使用 UniTaskCompletionSource 支持并发等待；取消一个等待者不取消包初始化。不能使用 Preserve 代替并发完成源，实际 QGF 探针已捕获该问题并修复。Unity 6.3 的场景句柄使用 YooAsset.SceneHandle，避免与 Unity 新类型重名。
- DataTable 增加 LoadDataTablesAsync，任务入口与原事件入口共用请求记录；取消及迟到回调不发布旧表，工厂内取消也在登记前检查。解析完成后先归还 Catalog，再完成异步任务；失败抛出明确异常，保留原事件通知。
- 资源 Inspector 可选择 ResourceSettings，显示新链路的目录和加载请求，避免访问旧资源池。DefaultConfigHelper 的文档与方法体按 UGF 规范补齐。
- 打包器支持独立排除内置 UniTask 或 YooAsset，生成 external-dependencies.json 和实际 tgz。2022.3 消费工程的最新 tgz 通过 33 项检查，包含实际包来源、官方 UniTask 2.5.11 / YooAsset 3.0.6 和后端源码指纹，不以旧包缓存代替当前产物。
- YooAsset 1280 个上游文件检查完整哈希；仅两份原本只有 GUID 的 meta 允许 Unity 写入已锁定哈希的默认 importer，不再忽略所有 meta 的 importer 内容。
- 最新 QGF 入口探针在 2022.3.62f3、6000.0.70f1、6000.3.24f1 各通过 29 项检查，覆盖 TOML、Luban、取消与重试、场景补偿卸载、在途关闭和同名包重建。报告位于 `Artifacts~/YooAssetMigration/qgf-backend-compatibility.json`。这些是 EditorSimulate 检查，不覆盖 Offline / Host 或 Player。

## 14. 继续实施

- IResourceBackend / IResourceBackendHost 是接线期间的过渡边界；迁移 Sample、移除旧 Loader 后归并到计划中的 Helper 契约，最终不保留双资源后端。
- 补齐 Offline / Host、下载、缓存、404、断网和重试验证；接入相应包操作、Inspector 和 Debugger。
- Sample 的独立 ResourceSettings / ResourceBuildProfile、启动迁移和资源示例迁移已完成，DataProvider 的旧文件容器分支已移除并同步 Samples~。模块中的资源脚本已移除 `#if UNITY_EDITOR` / AssetDatabase 路径转换，改用 YooAsset 地址或 GUID。Scene 的公开 UniTask 入口已实施；旧 Resource Loader 尚待迁移与删除。当前 2022.3、6.0、6.3 的隔离后端探针通过；旧 Consumer 工程仍含过期 Sample 源码，不能代替新 Git 消费工程验收。本次 StarForce 的真实 Host 与 Windows Offline Player 检查见第 15 节。
- 删除旧 Resource 加载、更新、清单、构建器及专用 EditorResource 实现；完成移动、导入、卸载、Player 和 Git 分发验收后再更新正式版本号。

## 15. StarForce 启动更新与实际运行验证（2026-10-25）

- 已阅读 YooAsset 3.0.6 的 Space Shooter（PatchManager、8 个状态节点与 PatchWindow）、UGF 有道教程资源章节及 GameFramework-Next 的流程实现。借鉴初始化、下载确认、进度、重试和下载后清理的职责划分，保留 QGF 的 Procedure / Resource / UI，不导入它们的状态机或事件框架。
- StarForce 采用初始化包 → 确认下载 → 下载 → 清理未使用缓存 → TOML / Luban / 字体预载 → 菜单。包初始化已包含版本请求与清单加载，示例不重复调用。无待下载文件时完成空下载器，再直接预载。
- ResourceComponent 增加 DownloadResourcesAsync。框架统一处理 UniTask 等待、失败异常和下载取消，StarForce 与 HTTP 模块示例共用入口。缓存清理拒绝在途下载和资源借用。
- 联机包可关闭 UseBuiltinFiles，支持没有 StreamingAssets 首包的纯远端包。重载 Launcher 时等待 QGF 上一实例完成包关闭，再创建同名包，避免语言切换后包占用冲突。
- 内置对话框可在关闭回调中立即打开下一提示；预载失败也使用内置界面，避免加载故障资源才能显示错误。四语言和连续对话框回归通过 62 项检查。
- 示例以资产 GUID 作为 Bundle 名，缩短按 Profile 隔离的构建路径，解决 Windows PathTooLongException。实际包同时生成官方首包 Catalog，Player 从首包目录部署；同一 Profile / 版本可重复构建，保留增量缓存。
- 6000.3.24f1 真实 Host 检查通过：81 个 Bundle，下载前确认、独立取消、404 失败后重试、同帧重新确认、缓存清理、Luban / TOML 预载、缓存命中后的再次启动。16 项检查通过，预期故障产生 4 条错误日志，故障恢复后无意外运行错误。报告：Artifacts~/ExamplesMigration/Consumer/Artifacts~/StarForceWorkflow/check.json。
- 6000.3.24f1 完整 Editor 回归通过：两局战斗、原生鼠标移动开火、血条复用、设置、关于、退出取消和语言重启。仍观察到关闭时 YooAsset 下载调度终止提示及场景卸载警告，未将其称为零警告运行。
- Windows Offline Player 两个独立进程均通过：共四局战斗、语言重启及跨进程设置保留，运行错误为 0。构建前后 ProjectSettings、场景与项目 StreamingAssets 的内存和磁盘检查均通过。报告：Artifacts~/ExamplesMigration/Consumer/Artifacts~/StarForcePlayer/audit.json。
- Assets 与 Samples~ 三个镜像逐文件一致；包检查通过 59 个示例、2281 项资产与 4147 个发布文件。上述结果不替代本次变更在 2022.3、6.0、IL2CPP、Android、WebGL 和新 Git 消费工程中的后续验收，也不表示第 14 节的旧资源代码清理已经完成。
- 后端关闭会取消已登记但尚未启动的 ResourceDownloaderOperation，并等待下载、缓存清理和未使用资产清理等 YooAsset 操作结束后再销毁资源包。新增探针在 2022.3.62f3、6000.0.70f1 和 6000.3.24f1 各通过 33 项检查；报告分别位于 `Artifacts~/YooAssetMigration/Bundled2022/Artifacts~/qgf-resource-backend.json`、`Artifacts~/YooAssetMigration/Bundled6000/Artifacts~/qgf-resource-backend.json` 和 `Artifacts~/YooAssetMigration/Bundled630/Artifacts~/qgf-resource-backend.json`。
