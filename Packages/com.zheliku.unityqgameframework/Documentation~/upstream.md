# 上游源码导入记录

核验日期：2026-10-16。使用 `git clone --depth 1` 获取指定上游的默认分支，并通过 `git ls-remote --symref <url> HEAD refs/heads/master` 核对远端 HEAD。以下是当时的最新默认分支提交，而非按标签选取的旧版；两个上游默认分支的最后提交均在 2021 年。

| 上游 | 分支 | 完整提交号 | 提交时间（UTC+8） |
| --- | --- | --- | --- |
| [UnityGameFramework](https://github.com/EllanJiang/UnityGameFramework) | master | `e7eb3ef11393df0ac504204c480cdb839752b05f` | 2021-10-28 15:00:30 |
| [GameFramework](https://github.com/EllanJiang/GameFramework) | master | `d0c010b05167c58e92350449d04864a91ca13fd2` | 2021-09-28 20:05:45 |

上游 package.json 的版本为 `2021.05.31`；这与分支最后提交日期是两个不同概念。原始清单与 README 存于 `Upstream/`。

## 导入映射

| 来源 | 包内目标 | 处理 |
| --- | --- | --- |
| GameFramework: `GameFramework/**/*.cs` | `Runtime/GameFramework/` | 330 个 C# 文件，包含原 AssemblyInfo，逐字节保留 |
| UnityGameFramework: `Scripts/Runtime/` | `Runtime/UnityGameFramework/` | 217 个 C# 文件及原 `.meta`，C# 不改动 |
| UnityGameFramework: `Scripts/Editor/` | `Editor/` | 71 个 C# 文件及原 `.meta`，C# 不改动 |
| UnityGameFramework: `GameFramework.prefab` | `Runtime/Prefabs/GameFramework.prefab` | 预制体与 `.meta` 不改动 |
| UnityGameFramework: `Libraries/ICSharpCode.SharpZipLib.dll` | `Runtime/Plugins/` | 原 DLL 与 `.meta` 不改动 |
| UnityGameFramework: `Libraries/link.xml` | `Runtime/link.xml` | 保留内容与 GUID |
| 两个上游的许可证和 README | `LICENSE.md`、`Documentation~/` | 保留许可证与出处 |

## 仅为包集成所做的改动

1. 新建包清单，名称为 `com.zheliku.unityqgameframework`，显示名为 `UnityQGameFramework`，版本为 `0.1.0-exp.1`。
2. 新增 `GameFramework.asmdef`：程序集名保持 `GameFramework`，`noEngineReferences` 开启；上游转换工具含 unsafe 实现，因此仅对此程序集开启 `allowUnsafeCode`。
3. 原运行时、编辑器 asmdef 增加对源码核心 `GameFramework` 的显式引用；原程序集名保持不变。
4. 新增目录与源码的 `.meta`；移动的 Unity 脚本、原 asmdef、DLL、预制体和 link.xml 保留原 GUID。
5. 不导入 `GameFramework.dll` 与其 XML 文档，避免源码与 DLL 重复定义。原独立 .NET 3.5 的 `.csproj` / `.sln` 不进入 Unity 包。
6. 包内 `.gitattributes` 将保留的约 196 KiB DLL 作为普通 Git 二进制储存，覆盖宿主宽泛的 DLL LFS 规则，使包不依赖该文件的 LFS 下载步骤。

临时上游 clone 位于系统临时目录，包与本仓库中不引入嵌套 `.git` 或 submodule。

## 后续修改与当前验证边界

2026-10-16 首批 Unity 6 修复修改了 `Editor/ResourceBuilder/ResourceBuilderController.cs` 的废弃打包选项和 `Runtime/UnityGameFramework/Debugger/DebuggerComponent.cs` 的剪贴板实现。上面的逐字节保留描述记录初次导入时的状态；具体后续变化见 CHANGELOG。

另已导入官方 StarForce 到仓库的 `Assets/Examples/StarForce`；其来源和路径接入调整记录在示例 README，发行时由工具生成 Samples~。示例不是核心包的运行时依赖。

本轮执行源码/资源 SHA-256 对照、元数据与预制体脚本引用检查、JSON/asmdef 结构检查。实际结果在任务交付时报告。

已通过官方 Unity CLI `1.0.0-beta.13` 的 recompile 与 Console 实时状态，验证 Unity 6000.3.24f1 下框架及示例编译通过、当前编译失败标记为 false、Console 错误为 0。通过预览场景检查 StarForce 启动场景，没有丢失脚本。尚未执行完整 Play Mode 或 Player 构建，也未验证远程 UPM 安装或产出 unitypackage；不据编译结果宣称完整 Unity 6 兼容。
