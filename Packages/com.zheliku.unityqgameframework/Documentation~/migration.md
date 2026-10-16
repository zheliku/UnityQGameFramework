# 基础维护版迁移

本版统一 Unity Q Game Framework 的产品入口、源码、命名空间和程序集。未保留旧命名空间的别名层；已有代码需要同步迁移。

| 旧标识 | 新标识 |
| --- | --- |
| `GameFramework` 命名空间及子命名空间 | `UnityQGameFramework.Core` 及对应子命名空间 |
| `UnityGameFramework.Runtime` | `UnityQGameFramework.Runtime` |
| `UnityGameFramework.Editor` | `UnityQGameFramework.Editor` |
| `GameFramework` 程序集 | `UnityQGameFramework.Core` |
| `UnityGameFramework.Runtime / Editor` 程序集 | `UnityQGameFramework.Runtime / Editor` |
| `GameFrameworkEntry / Module / Component / Exception` | `FrameworkEntry / Module / Component / Exception` |
| `GameFrameworkLinkedList / List / MultiDictionary / Serializer / Action / Func / EventArgs / Log` | 对应的 `Framework*` 类型 |
| `Version.GameFrameworkVersion` | `Version.FrameworkVersion` |
| `GameFramework.prefab` | `UnityQGameFramework.prefab` |
| `Game Framework` 菜单 | `Unity Q Game Framework` |

同时更新业务 asmdef 的程序集引用、使用程序集限定名的反射字符串和业务 link.xml。仓库中的配置 XML 根节点已同步更名为 UnityQGameFramework；自己的资源编辑器/打包 XML 也需同步迁移。源码、预制体和场景的原有 .meta 随重命名保留，框架默认辅助器的序列化类型字符串已更新。

最低 Unity 版本提升为 2022.3。旧于该版本的兼容分支及不再参与编译的 WWW 辅助器被移除，网络请求使用 UnityWebRequest。对外的模块职责、加载回调、资源与对象池语义保留。

## 保留的数据格式标识

`GameFrameworkSetting.dat`、`GameFrameworkVersion.dat`、`GameFrameworkList.dat` 及资源包工具扫描使用的 `GameFrameworkVersion.*.dat` 是已有持久化格式的文件名。本版保留这些标识，确保存档、版本列表及已构建的更新资源仍可读取；它们不再用作产品展示名称。资源序列化版本和二进制签名不因品牌改名改变。

日志宏名称请以 `Editor/Misc/LogScriptingDefineSymbols.cs` 为准，通过新菜单配置。迁移自外部工程时同步调整自行定义的宏。

## 示例服务地址

StarForce 的介绍、网站和帮助入口已指向本项目。旧演示服务器的版本检查与应用下载 URL 已清空；编辑器资源模式不依赖这些服务。若使用可更新资源模式，在 `GameMain/Configs/BuildInfo.txt` 配置自己的版本服务和下载地址，并为业务反射类型保留 linker 配置。

MIT 版权和第三方许可继续保留；来源项目的名称只出现在必要的来源、许可及本迁移映射中。
