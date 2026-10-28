# 模块用法

先完成[开始使用](../01-getting-started.md)，再按需要选择模块。每页给出核心调用、清理责任和可运行示例。

| 模块 | 用途 |
| --- | --- |
| [Base / Utility](base-utility.md) | 控制游戏时间，使用日志、文本、转换与随机工具。 |
| [Config](config.md) | 读取少量只读游戏参数。TOML 由 Tomlyn 解析。 |
| [DataNode](data-node.md) | 用路径组织内存中的临时共享值。 |
| [Event](event.md) | 用事件通知多个接收者，发送方无需引用接收者。 |
| [Fsm](fsm.md) | 创建由框架统一更新的业务状态机。 |
| [Procedure](procedure.md) | 用启动、菜单、加载与战斗流程组织整个游戏。 |
| [ReferencePool](reference-pool.md) | 复用轻量 C# 临时对象。 |
| [ObjectPool](object-pool.md) | 管理可借用、归还和淘汰的实际对象。 |
| [Setting](setting.md) | 保存玩家可修改的本地偏好与进度。 |
| [Localization](localization.md) | 选择语言并读取、格式化 TOML 字典。 |
| [DataTable](data-table.md) | 使用 Luban 生成的强类型表，统一通过 QGF.DataTable 加载与查询。 |
| [Resource](resource.md) | 通过 YooAsset 加载资产、二进制和资源包 |
| [Entity](entity.md) | 加载、显示和隐藏带 EntityLogic 的游戏对象。 |
| [UI](ui.md) | 管理 uGUI 窗口的加载、分组与复用。 |
| [Sound](sound.md) | 按声音组管理音乐和音效。 |
| [Scene](scene.md) | 通过资源模块加载与卸载场景。 |
| [FileSystem](file-system.md) | 在一个容器文件内管理多份逻辑文件。 |
| [WebRequest](web-request.md) | 发起 HTTP 请求并读取响应。 |
| [Download](download.md) | 将网络内容下载到磁盘文件。 |
| [Network](network.md) | 建立 TCP 通道，自定义消息编解码和处理。 |
| [Debugger](debugger.md) | 查看运行状态并登记业务调试页。 |

资源制作与发布见 [YooAsset 工作流](resource-build.md)，全部场景见 [模块 Sample](../../Samples~/01_Modules/README.md)。
