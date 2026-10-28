# UGF 到 QGF

QGF 保留 UGF 的模块、组件、Helper、流程、事件和池机制，在现有接口中接入现代工具。

| 部分 | 当前实现 |
| --- | --- |
| Core | 基础类型与模块接口使用 `UnityQGameFramework.Core`；模块及 Helper 通知集中在 `Core.Events`，避免与 Runtime 游戏事件同名冲突 |
| 基础类型 | 保留上游 `GameFrameworkEntry`、`GameFrameworkLinkedList` 等名称 |
| Unity 入口 | 场景组件仍由 GameEntry 登记，常用业务使用 `QGF.Setting` 等快捷入口 |
| DataTable | Luban Excel schema、生成代码、bytes 和 Catalog，移除旧 TXT 生产流程 |
| Config / Localization | Tomlyn 解析标准 TOML，按层级映射查询键 |
| Resource | YooAsset 负责包、清单、下载与句柄，UniTask 提供业务异步入口 |
| 输入 | Unity Input System |
| 示例 | 三个可独立导入的 Sample，uGUI、URP，不替宿主修改项目设置 |

`GameFrameworkEntry.GetModule<T>()` 来自 UGF。QFramework 的 Architecture、Model、System、Utility 分层及 BindableProperty 尚未迁入，不应把现有入口当作其替代实现。

Core、Runtime、Editor 分别承担引擎无关模块、Unity 适配、编辑器工具，保留这三个程序集边界。第三方运行库使用各自程序集；每个 Sample 只有一个自有程序集。

Resource 的旧回调仍供 Entity、UI、Config 等模块接线，进入同一 YooAsset 后端；旧 UGF 版本清单和补丁接口不用于新 YooAsset 工作流。新业务参照[资源模块](Modules/resource.md)。

## 查看逐文件差异

在完整仓库执行：

```sh
python Tools/Review/compare_ugf.py --refresh
```

在 VS Code 打开 `Artifacts~/UgfComparison/UGF-QGF.code-workspace`，点击 Source Control 中的文件：左侧是固定提交的官方 UGF 原文，右侧是当前 QGF。对应文件按当前路径排列，改名与移动仍显示为 M；真正新增或删除的文件才显示 U 或 D。

`file-map.json` 记录上游提交、原始路径和哈希。生成工具只操作独立对照副本，不改主仓库索引；刷新前将旧副本备份到 History。
