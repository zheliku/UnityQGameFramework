# Unity 兼容性与验证

最低安装版本为 **Unity 2022.3**。框架只声明所需内置模块，使用该基线存在的 API。开发宿主使用 Unity 6000.3.24f1，独立消费者验证不携带宿主的 URP、Input System、CLI Pipeline 或其他工具插件。

## 当前验证记录

| Unity 版本 | 验证状态与范围 |
| --- | --- |
| 2022.3.62f3 | 通过独立消费者编译、19 模块创建与单例检查、事件/对象池回归、类型发现、预制体引用、AssetBundle 构建读取和 Windows Mono Player 无图形启动 |
| 6000.0.70f1 | 同上，全部通过 |
| 6000.3.24f1 | 同上，全部通过；另通过 StarForce 菜单进入战斗、原版鼠标移动/射击、21 个 URP 材质的实际帧验证；Console 无新增错误/警告 |

更新日期：2026-10-16。Android JNI 的字节转换已验证原始二进制值不变，但 Android/iOS/WebGL、IL2CPP 和远程热更新尚未做完整平台回归。表中 Windows Player 为基础框架启动，不等同于完整 StarForce Player 资源发行。

属性整理后，上述三个版本均重新通过独立工程的 9 项验证及 Player 启动，新增覆盖 null 元素查询和默认值类型键的哈希计算。开发宿主中 9795 条公共成员与 Unity 序列化字段记录与整理前一致；StarForce 鼠标移动、射击及 URP 检查已重新通过。

## Luban 接入验证（2026-10-17）

- Unity 2022.3.62f3、6000.3.24f1：独立 UPM `.tgz` 消费者编译和 Windows Mono Player 通过。11 张 StarForce 生成表经 DataTableComponent 查询与整组卸载；仅登记 Catalog 的 AssetBundle 构建、加载、解析及释放后读取也通过。Player 无运行错误；源 Excel、schema、Profile 未复制到消费者工程。
- Unity 6000.3.24f1：StarForce 155 项原始数据核对、原生／QGF 记录一致性、筛选排序、具名表、取消异步请求、失败时整批不发布、资源模式加载通过。菜单进入战斗、原版鼠标移动／射击与 21 个 URP 材质检查通过。
- 编辑器生成：默认工作区、缓存编译器安装入口、只读 UPM 包缓存中的模板、校验失败不覆盖输出、目录移动跟随、GUID 保留、用户文件保留和废弃生成文件清理均通过；重新指定输出目录时拒绝覆盖不属于该 Profile 的文件和 Catalog。
- 模板：真实 Luban 生成并编译的跨表引用、同 bean 多表、容器、字符串键、值类型行、单例及列表表通过；常规 `int Id` class map 表继续使用 QGF 查询，其他类型使用原生 Tables。

验证脚本在 `Tools/Validation`：`StarForceLubanValidation.cs`、`LubanEditorValidation.cs`、`luban_templates.py`、`prepare_luban_player.py`、`LubanPlayerValidation.cs` 与 `LubanRuntimeProbe.cs`。先用 `Tools/Packaging/prepare_upm.py --output <新的 Artifacts~ 子目录>` 暂存发行包，再创建消费者：

```text
python Tools/Validation/prepare_luban_player.py --package <暂存的 .tgz> --output <新的 Artifacts~ 子目录> --unity-version 6000.3.24f1
Unity.exe -batchmode -nographics -projectPath <消费者目录> -executeMethod Luban.Validation.LubanPlayerValidation.Run -logFile <日志>
```

包缓存生成入口为 `Luban.Validation.LubanPlayerValidation.ValidateCachedGenerator`，另传 `-qgf-luban-generator <本机 Luban.dll>`。结果为 `Results/editor-result.json`、`player-result.json` 或 `generator-cache-result.json`。本轮未重跑 Unity 6.0 的 Luban 路径，也未验证完整 StarForce Player 发行和远程更新。

## Excel schema 与 TOML 验证（2026-10-17）

- 默认工作区与 StarForce 通过真实 Luban 重新生成：1 + 11 张表的 `.bytes` 与迁移前一致，既有输出与源表的 61 份保留 `.meta` 不变；旧 XML 定义及其 `.meta` 按迁移移除。
- XLSX Diff 1.6.4 实际读取器可识别全部 12 份规范化工作簿；本机读取补丁同时支持历史中的前缀格式。StarForce 11 个工作表的全部单元格与格式元数据经读取器比较相同。
- Unity 6000.3.24f1：StarForce TOML 预加载、菜单场景、11 张表及 155 项原数据比较通过；TOML 验证覆盖中文／BOM、不同区域设置、7 类失败的原子性、TextAsset 导入和原配置 GUID。
- Unity 2022.3.62f3、6000.3.24f1：新 `.tgz` 独立消费者成功构建和运行 Windows Mono Player，AssetBundle 内的 TOML TextAsset 经 byte[] 解析后保留原三个值，11 张表读取／整组卸载通过，运行错误为 0。未验证 IL2CPP 和完整 StarForce Player 发行。
- 只读 UPM 包缓存中的新 Excel schema 初始模板在 Unity 6000.3.24f1 成功创建工作区、生成一张 Item 表及 Catalog；不依赖仓库源码路径。

脚本新增 `ExcelSchemaMigrationValidation.cs`、`TomlConfigValidation.cs` 和 `normalize_xlsx_namespaces.py`。工作簿规范化会逐 XML 比较命名空间展开后的元素／属性／文本，并检查 ZIP CRC；不以文件字节完全一致替代单元格语义比较。

## 生产前复查（2026-10-18）

本轮补充了真实 UPM Git URL 消费者：远端现有 main 通过基础框架编译和 Player；待发布本地版本通过隔离 Git 快照安装，在 Unity 2022.3.62f3、6000.3.24f1 首次自动建工作区、下载编译器、菜单生成／strict 校验、Luban/TOML AssetBundle 和 Windows Mono Player 均通过。Unity 6000.3.24f1 额外通过默认 Item 工作区从首次生成到 Player 查询的验证。

新增文件写入失败回滚、Catalog 预检查和外部进程超时测试；查询 Inspector 不再按次分配整表数组及反射获取方法。GitHub 发布状态、脚本入口和未验证平台见 [生产前检查记录](production-readiness.md)。这不等同于 IL2CPP 或完整游戏发行的认证。

## 版本策略

- 不保留 Unity 2021 及更早版本的条件分支；平台、编辑器和输入系统的必要分支继续保留。
- 以 2022.3 公共 API 为共同基线。新 API 只有解决已确认的问题时才引入，并在实际版本边界加条件编译；不按未知未来版本编写猜测分支。
- 继续使用 EditorBuildSettings.scenes 访问活动场景列表，适配旧 Build Settings 与 Unity 6 Build Profiles 的共有入口。
- 核心与渲染管线、输入实现隔离。URP、Input System 和 CLI 的版本由消费者工程选择，升级这些包不要求替换核心。
- 本轮维护和回归范围为 Unity 2022.3、6.0 和 6.3；其他版本后续按需要验证。

这些策略为后续 Unity 6 更新提供可重复维护的基础，不将未运行过的版本描述为已验证。

## 可重复验证

仓库提供 `Tools/Validation/prepare_project.py`，在忽略的 Artifacts 目录创建独立消费者工程，复制当前框架唯一源码，使用基础内置模块，不导入开发宿主设置。

```powershell
python Tools/Validation/prepare_project.py --unity-version 2022.3.62f3 --output Artifacts~/Validation/Foundation/2022.3.62f3-new
```

使用对应 Editor 执行：

```text
Unity.exe -batchmode -nographics -projectPath <生成的工程> -executeMethod Foundation.Validation.FoundationValidation.Run -logFile <日志>
```

验证方法会自行退出 Editor，无需添加 -quit。Windows 验证要求该 Editor 包含 Windows Mono 构建支持，并能使用本机 Unity 许可证。结果为工程中的 Results/editor-result.json、Results/player-result.json 和日志；只有 success=true 且进程成功退出才算通过。

验证覆盖模块单例、拒绝具体类型调用、事件的同步/排队语义、对象池借用/归还、关闭后的重新创建、含 null 的链表范围查询、默认及有效值类型键的哈希计算、后加载程序集、自定义 runtime asmdef 流程发现、21 个序列化辅助器、AssetBundle 构建/加载/卸载，以及实际 Windows Player 的流程启动和组件访问。编译日志应另外检查 CS0618、编译错误和 Unity 迁移警告。

## 官方 API 依据

- [Unity 2022.3 对象查找](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Object.FindObjectsByType.html)
- [Unity 2022.3 刷新率](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Resolution-refreshRateRatio.html)
- [Unity 类型缓存](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/TypeCache.GetTypesDerivedFrom.html)
