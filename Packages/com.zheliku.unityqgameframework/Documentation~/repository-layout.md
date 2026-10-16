# 目录、架构边界与发布规划

## 选择：Unity 工程根 + 一个 embedded UPM 包

仓库根就是现有 Unity 工程。框架源码放在 `Packages/com.zheliku.unityqgameframework`；`Assets` 用于本仓库的集成验证和示例宿主，不放另一份框架源码。保留 GitHub origin 与现有工程内容。

这样既可以直接用 Unity Hub 打开仓库根，也能通过 UPM 的 `?path=/Packages/com.zheliku.unityqgameframework` 安装同一份插件。不在仓库外建立父工程，不嵌套第二个 Unity 工程或 Git 仓库，不用 submodule 或符号链接作为安装前提。

依据：[Unity 6 包目录规则](https://docs.unity3d.com/6000.0/Documentation/Manual/cus-layout.html)、[embedded packages](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-embed.html)、[Git 子目录依赖](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-git.html#subfolder)。

## 当前程序集

```text
GameFramework                       纯 C# 核心，不引用 UnityEngine
      ↑
UnityGameFramework.Runtime           Unity 组件与资源/场景/实体等适配
      ↑
UnityGameFramework.Editor            编辑器工具，也直接引用 GameFramework
```

原核心 DLL 已替换为源码。保留上游程序集名、命名空间、预制体与脚本 GUID，建立可比较的导入基线。项目名与包名已改为 UnityQGameFramework；公共 API 的统一改名属于后续重构，需同时迁移反射字符串、序列化数据、link.xml 和预制体引用。

包依赖只声明上游所需 Unity 内置模块。现有宿主的 URP、Alchemy、Rainbow Folders、测试框架等留在宿主清单，避免外部安装者被绑定到开发者的工具组合或渲染管线。

## 后续目录落位（尚未实现）

| 目录 | 职责 |
| --- | --- |
| `Runtime/Architecture/` | 引擎无关的 Architecture、Model、System、Utility 与 BindableProperty；独立 asmdef，不依赖具体游戏或 Luban 生成表 |
| `Runtime/Integration/` | Architecture 与 GF 组件、流程、生命周期的桥接，显式引用核心/架构/Unity 适配程序集 |
| `Runtime/Configuration/` | 通用的配置加载契约与 Luban 接入；不绑定用户自己的 `cfg.Tables` 或业务表名 |
| `Editor/Configuration/` | 导表入口、项目配置与生成诊断；外部工具版本和输出路径显式配置 |
| `Tests/Editor/`、`Tests/Runtime/` | 包内可选测试，通过测试 asmdef 隔离，不让消费者运行时依赖 NUnit |
| `Samples~/` | 发行暂存包中的可选示例，由宿主 `Assets/Examples/StarForce` 生成；后续扩展 Architecture、BindableProperty 与 Luban 示例 |
| `Documentation~/` | 随包发布的说明、迁移指南与来源记录；`~` 目录不参与正常资产导入 |
| 根 `Configs/` | 测试宿主/示例的 Luban 源表与生成配置，属于项目数据 |
| 根 `Tools/` | 开发、导表、校验及打包脚本；不进入运行时依赖链 |
| 根 `Artifacts~/` | 被 Git 忽略的导出工程与发行产物 |

其余未实现模块暂不创建空目录或空程序集。等实际模块进入时再创建，并随功能补齐示例与测试。

## StarForce：开发源放 Assets，发行内容放 Samples~

已导入官方 StarForce `d4ce803353f89852c4d58df65b2482a3b08eb11f`，唯一可编辑源码放在 `Assets/Examples/StarForce/`，可以直接编译、开场景和调试。没有复制上游 UGF 子模块或旧 ProjectSettings。路径适配通过 GameMain 的目录 GUID 定位，因此也支持用户把发行样例导入到 `Assets/Samples/...`。

UPM 的 `Samples~` 不参与普通资产导入，消费者点击 Package Manager 的 Import 后才会复制到 Assets；这适合发行，但直接在该目录开发无法进入常规场景和脚本工作流。因此不在开发包中长期维护第二份镜像，也不要求 junction / symlink。

`python Tools/Packaging/prepare_upm.py` 会在 `Artifacts~/UPM/<版本>/` 下：

1. 复制当前框架包到 `Packages/com.zheliku.unityqgameframework/`。
2. 把 Assets 示例完整复制到生成包的 `Samples~/StarForce/`，包括 `.meta`、许可证及依赖。
3. 在生成包清单登记 `samples`，逐文件核对示例内容，并生成标准 `package/` 根结构的 `.tgz`。

开发分支的 Git URL 当前提供框架；未来完整发行标签（例如 `upm/v<版本>`）应指向上述生成的发布树，保留相同包路径，URL 只需追加该标签。该标签包含生成的 Samples~，因此 Git URL 安装后同样能显示可选示例。创建发行标签和 GitHub 发布仍是后续工作，本轮没有发布。

不能把只存在于本机暂存目录的 Samples~ 当作已上线功能；正式发布前还需在干净工程验证 `.tgz` 与发行标签的 Git URL 安装及样例导入。

## QFramework 与 Luban 的职责

- Architecture 负责模块装配、访问权限与生命周期；Model 持有状态，System 承载业务规则，Utility 提供基础能力，BindableProperty 支持状态订阅。
- GF 保留资源、实体、UI、流程、事件、对象池等基础设施能力；后续明确它与 Architecture 的事件语义、所有权和释放顺序，避免双重生命周期。
- Luban 替换配置表生成和读取工作流。源表、生成代码与生成数据由使用框架的游戏工程持有，运行时只引用通用加载契约；生成输出不能写进只读的 UPM Git 缓存。
- 推荐用户输出到 `Assets/<Game>/Generated/Config/` 与其资源目录；本仓库的示例源表和生成数据可以封装进 `Samples~`，由用户选择导入。
- 本地 GameZMXY 仅供核对实现思路；其 Godot 专属 API、C# 新语法、游戏数据和项目规则不直接套用到 Unity 框架。

本轮不导入完整 QFramework 工具集，也不移除上游 Config/DataTable，以便先看到未经行为修改的 Unity 6 导入结果。

## 发布约定

1. **开发**：直接编辑 embedded package；宿主无需安装自己的 Git URL，不再在 Assets 维护副本。
2. **UPM**：从 GitHub 的包子目录安装；有稳定版本后同步 package.json、CHANGELOG 与 Git tag。带样例的发行 tag 指向上述生成发布树。URL 格式为 `https://github.com/zheliku/UnityQGameFramework.git?path=/Packages/com.zheliku.unityqgameframework#<tag>`。
3. **unitypackage**：后续导出工具从同一源码创建独立临时导出工程，把内容映射至 `Assets/UnityQGameFramework/` 并保留 `.meta`；附带 README、LICENSE 与第三方许可。排除宿主 Assets、ProjectSettings、manifest、缓存和测试数据，发布前验证可导入干净工程。
4. **不同时安装两种发行形式**：同一工程选择 UPM 或 Assets 下的 unitypackage 安装，避免重复程序集与 GUID。原版 UGF 也不能与当前兼容命名的本包并存。
5. **验证后发布**：先完成 Unity 6 编译/启动、独立工程 UPM 安装、unitypackage 导入与包内依赖检查，再标记可用版本。

已实现 UPM 发布暂存与 `.tgz` 生成工具；GitHub Actions、发行标签、远程发布、unitypackage 导出与完整运行适配留待后续阶段。
