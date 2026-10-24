# 从 UGF 到当前 QGF，改了什么

当前代码为 `0.1.0-exp.8`。本页说明保留和替换的 UGF 接口，以及使用 QGF 的方式。

## 1. 哪些机制仍然来自 UGF

Core Manager、Unity Component、Helper 的分工，统一 Update/Shutdown，Procedure 建立在 Fsm 之上，以及资源、实体、UI、事件、对象池、引用池的协作方式，都延续自 UGF。

`FrameworkEntry.GetModule<T>()` 是 UGF 模块入口的改名与维护版本，当前增加了缓存；它不是 QFramework 的 GetSystem/GetModel。`Runtime.GameEntry.GetComponent<T>()` 则取得场景组件，不能把两者当成一套任意业务对象容器。

当前工程来源记录在 [第三方声明](../Third%20Party%20Notices.md)：

| 来源 | 导入基线 |
| --- | --- |
| UnityGameFramework | `e7eb3ef11393df0ac504204c480cdb839752b05f` |
| GameFramework | `d0c010b05167c58e92350449d04864a91ca13fd2` |

使用上游文档时，对照这些导入基线以及本包的公开接口与生成代码。

## 2. 包与命名空间

| 原来的组织 | 当前的组织 |
| --- | --- |
| Core 与 Unity 层按上游项目分发 | 同一 UPM 包中的 Runtime/Core、Runtime/Unity |
| GameFramework 命名空间 | UnityQGameFramework.Core |
| UnityGameFramework.Runtime | UnityQGameFramework.Runtime |
| 上游编辑器菜单 | QGF 菜单 |
| 按模块维护、分散在开发工程的示例 | 点点点、模块示例、StarForce 三个 UPM Sample |

Core、Runtime、Editor、Luban 有独立程序集。业务代码写在自己的 Assets 目录，通过引用使用包；不要修改 Library/PackageCache 内的代码作为项目定制方式。

`Assets/Examples` 和包内 `Samples~` 使用相同的 `00_PointPointPoint`、`01_Modules`、`02_StarForce` 三个目录。示例从 Assets 原样复制发布。普通使用者导入 Sample 后，Assets/Samples 下的内容是自己的副本，包更新不会自动覆盖它。

## 3. DataTable 已替换数据生产与加载

旧链路是 TXT 数据、旧代码生成器、IDataRow 的逐行解析和逐表读取。当前链路是官方 Luban 编译器、Excel schema、生成 C#、bytes、Catalog 与整组 Tables：

```text
Excel schema + 数据 + luban.conf
           ↓ QGF > Generate Tables
Generated/*.cs + Data/*.bytes + Catalog.asset
           ↓ DataTableComponent.LoadDataTables
Luban 强类型表 + QGF 公共查询基类
```

保留 DataTableComponent 场景节点和条件、排序查询。所有生成表统一从 `GetDataTable<TbXxx>()` 取得，管理器直接登记表对象。已移除旧 TXT 解析流程、IDataRow 的 int Id 限制及额外表视图；字符串键、复合索引、list/one 表和跨表引用均通过同一入口访问。旧代码的泛型参数需要从行类型 DRXxx 改为表类型 TbXxx。

Profile 控制编辑器生成。Catalog 在运行时把文件名关联到 TextAsset，并携带结构 hash；运行不依赖 Excel 或编译器。批量发布前检查结构与数据，失败不应留下半组已注册表。完整操作见 [Luban 示例](02-data-tables.md) 和 [DataTable 原理](Modules/data-table.md)。

## 4. TOML 替换了哪些内容

| 内容 | 当前读取方式 | 仍然属于谁 |
| --- | --- | --- |
| 全局游戏参数 | DefaultConfigHelper + TomlConfigParser | Config |
| 语言文本 | DefaultLocalizationHelper + TomlLocalizationParser | Localization |
| 示例 BuildInfo、内置字典 | 示例的 TOML 读取逻辑 | StarForce |
| 构建场景、资源集合、资源编辑器、资源构建配置与报告 | EditorToml、对应资源工具 | Editor |

使用 Tomlyn 解析标准 TOML，没有自写 TOML 语法解析器。Config 将嵌套标量键展开；Localization 先选当前语言，再展开字符串键。它们各自通过资源接口读取，**Config 没有依赖 DataTable 才能工作**。

没有把所有文件都改成 TOML：Luban 的 luban.conf 是官方工具的 JSON 配置；Setting 默认保存 GameFrameworkSetting.dat；AssetBundle 和资源版本数据仍是资源管线自己的格式。文件格式按用途区分，不通过一个“万能配置组件”管理。

## 5. Unity 与示例适配

当前使用现代 Unity 对象查找 API、refreshRateRatio，以及 NamedBuildTarget 相关编辑器 API，处理旧接口的弃用警告。序列化字段与对外只读状态按实际用途整理；有 Unity 序列化要求的字段仍保留，不因改用自动属性而丢失资产配置。

StarForce 保留原版鼠标操作，按当前输入后端使用对应 API；不将 WASD/空格扩展当成框架的默认玩法。示例自身管理需要的层与资源路径，编辑器场景经完整路径加载，导入和试玩不自动替宿主修改 Layer、输入后端、URP 资产或构建场景列表。

材质使用 URP 的内置 Shader，移除示例自定义 Shader。宿主需要已经启用 URP，框架不替宿主创建一套渲染管线。血条初始化移到明确可用的阶段，模板或对象池缺失时给出诊断，避免 Update 反复空引用。

示例资源通过目录 GUID 定位，方便导入到不同版本的 Assets/Samples 路径；贴图和包依赖的实际二进制内容按普通 Git 数据发布，避免 Git URL 安装后只收到 LFS 指针文件。

兼容目标为 Unity 2022.3 LTS、6.0、6.3，文档截图来自 Unity 6000.3.24f1 的 URP 编辑器。项目验收同时检查编辑器运行、Player 资源构建与更新流程。

## 6. 编辑器工作流

Unity 层新增 `QGF` 静态入口：`QGF.Config`、`QGF.Fsm` 等属性复用 GameEntry 的组件缓存。它与 GGF 的静态组件入口用途相似，属于调用方式整理，没有替换 Core 的管理器机制。

- 安装后在 Assets 创建默认 Luban 工作区，包含官方 schema 工作簿和 Item 示例；通过 QGF Settings 选择全局 Profile。
- DataTable Inspector 可以选择 Profile，显式应用到全局并执行生成；运行 Catalog 与全局生成 Profile 分开管理。
- QGF > Open Folder 分别打开表源、生成代码、表数据和存档位置，不把不同含义的 Data 混成一个目录。
- 生成器管理代码、数据、Catalog 的输出，手写扩展放在独立目录；本机 Luban 编译器可使用缓存或本地路径。
- 卸载入口登记并备份相关项目文件，示例可单独移除。删除包之前仍要迁移自己的业务代码与依赖。
- Alchemy、Rainbow Folders 通过 QGF > Extensions 按需安装，核心程序集不引用它们。插件许可见 [第三方声明](../Third%20Party%20Notices.md)。

`Tools/Packaging` 中的同步与发布检查属于完整源码仓库的维护工具，普通 Git 包使用者不需要这些脚本来运行游戏。

## 7. QFramework 的哪些部分还没有迁入

Architecture、IModel、ISystem、IUtility、ICommand、BindableProperty 仍属后续迁移内容。当前 DataNode 的 Variable 用于包装值，变更通知由业务处理。

文档写法参考 [QFramework README](https://github.com/liangxiegame/QFramework) 和 [示例](https://github.com/liangxiegame/QFramework/blob/master/Doc.md)：先展示运行结果，再解释 QGF 的接口与生命周期。

后续业务分层会围绕状态、服务、命令和表现的依赖设计，与已有 Procedure、Event、UI、Entity 协作。

## 阅读旧示例时怎么判断

状态机、事件、资源引用与对象复用的概念仍有参考价值。遇到旧 namespace、TXT 表解析、XML 字典或旧菜单，先换成本包对应章节。遇到业务扩展方法，例如 StarForce 的 ShowMyAircraft、OpenUIForm(UIFormId)，先看示例中的 Extension；这些不是组件原生接口。

版本级记录见 [Changelog](../CHANGELOG.md)，模块之间的调用链见 [从 Play 到战斗](00-runtime.md)。

### IDataProvider 与 Luban

Config 和 Localization 仍使用 UGF 的 `IDataProvider<T>`，因为它们读取一份文本或字节资源后交给对应 Helper 解析。DataTable 使用独立的 Luban 发布流程：Catalog 组合多份 bytes，生成的 Tables 完成跨表引用和 schema 校验后一次性登记。没有添加表面兼容的 `LubanTableProvider`，避免把多文件强类型流程压缩成不适用的单资源 `ParseData` 接口。
