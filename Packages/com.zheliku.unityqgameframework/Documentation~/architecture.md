# 基础版架构

本版采用三层结构：Core 负责引擎无关的模块及契约，Runtime 负责 Unity 生命周期和默认实现，Editor 提供配置与资源工具。程序集之间的依赖方向固定，Core 不引用 UnityEngine，运行时不引用编辑器或示例。

## 初始化、轮询与关闭

`FrameworkEntry.GetModule<T>()` 只接受核心模块接口，通过接口对应的实现创建模块。模块实例按接口/实现类型缓存；有序链表仍决定更新和释放次序，缓存不参与排序。

Unity 的 `FrameworkComponent.Awake` 向 `GameEntry` 注册组件。BaseComponent 安装文本、版本、日志、压缩及 JSON 辅助器，应用帧率、时间缩放和运行配置。其他组件在 Awake 获取核心模块，在 Start 注入资源、对象池等依赖及具体辅助器。ProcedureComponent 等待下一帧，再启动入口流程，避免依赖渲染帧而阻塞无图形运行。

每帧由 BaseComponent 传入 `Time.deltaTime` 和 `Time.unscaledDeltaTime`，模块按优先级从高到低更新。相同优先级保持创建先后顺序。关闭按反向顺序运行，再清理模块缓存、引用池和原生内存缓存。BaseComponent 销毁时也清理 Unity 组件注册表和低内存事件订阅，以便再次启动。

## 模块与依赖

| 模块 | 核心职责 | 主要模块依赖 | 更新优先级 |
| --- | --- | --- | --- |
| Event | 排队及同步事件、订阅管理 | 引用池 | 7 |
| ObjectPool | 单次/多次借用、容量、过期与优先级释放 | 引用池 | 6 |
| Download | 下载任务队列、代理、重试及进度回调 | 任务池、下载辅助器 | 5 |
| FileSystem | 虚拟文件系统、只读/读写存储 | 文件流辅助器 | 4 |
| Resource | 资源版本、校验、更新、依赖加载与池化 | Download、FileSystem、ObjectPool | 3 |
| Scene | 场景加载/卸载状态及回调 | Resource | 2 |
| Fsm | 具名状态机、状态转换与状态数据 | 引用池 | 1 |
| Config | 标量配置解析与查询 | Resource、解析辅助器 | 0 |
| DataNode | 树状数据结点与变量 | 引用池 | 0 |
| DataTable | 表、行和表解析器 | Resource、读表辅助器 | 0 |
| Entity | 实体分组、显示、隐藏、挂接和回收 | Resource、ObjectPool | 0 |
| Localization | 语言与字典解析 | Resource、字典辅助器 | 0 |
| Network | 频道、连接、收发、心跳及包辅助器 | Socket、引用池 | 0 |
| Setting | 本地配置与持久化契约 | 存储辅助器 | 0 |
| Sound | 声音分组、代理和播放回调 | Resource、声音辅助器 | 0 |
| UI | 界面组、覆盖/暂停、开关界面和回收 | Resource、ObjectPool | 0 |
| WebRequest | HTTP 任务与代理 | 任务池、请求辅助器 | 0 |
| Debugger | 调试窗口树与更新 | 调试窗口契约 | -1 |
| Procedure | 建立游戏流程状态机 | Fsm | -2 |

ReferencePool、TaskPool、可复用集合、序列化器、变量和 Utility 是公共基础设施，不是额外的模块单例。业务代码通过接口或 Unity 组件使用能力，模块通过 Set*Manager/Set*Helper 显式建立依赖。

## 资源链路

资源模式继续区分打包资源、可更新资源以及边玩边更新。ResourceLoader 使用任务池限制并发，通过辅助器读取 AssetBundle/文件系统资源，维护资产与资源包的依赖计数和对象池；释放资产需要经过资源模块。

编辑器资源模式由 EditorResourceComponent 使用 AssetDatabase、编辑器场景及异步回调模拟加载，方便直接调试 Assets 内容。场景仍需按示例说明配置活动构建场景列表。它不等同于 Player 的 AssetBundle 路径，因此验证同时覆盖编辑器示例与实际资源包/Player。

版本列表、CRC/哈希、压缩、资源包签名及已有文件命名在基础版保持兼容；品牌重命名不重写资源内容。压缩 DLL 保持现有版本与许可，本阶段不替换算法或压缩格式。

## 事件与对象所有权

Fire 是可跨线程排队的事件入口，在主循环派发；FireNow 为同步入口，不承诺线程安全。事件参数由引用池回收。实体、UI 和资源的卸载会经过对应回收队列及对象池，不能把对象池借用状态当作 Unity 对象是否仍存在。

FSM 轮询使用临时快照以允许状态机集合在回调中变化。状态转换按原有 Leave/Enter 顺序执行。网络发送、下载与资源更新的队列和回调模型不因 API 更新改变。

## Unity API 与工具边界

Unity 版本相关内容集中于 Runtime/Unity、Editor 和可选示例。对象查找使用 FindFirstObjectByType/FindObjectsByType；刷新率使用 refreshRateRatio.value；宏定义使用 NamedBuildTarget；网络辅助器统一使用 UnityWebRequest。Android JNI 使用有符号字节 API，按原始字节拷贝至现有 byte[] 接口。

编辑器使用 TypeCache 和 CompilationPipeline 发现派生类型，支持业务 asmdef，并过滤编辑器专属程序集。核心反射工具读取当前已加载程序集，保留成功的类型缓存，避免首次初始化快照漏掉后加载的程序集；类型加载异常时仍返回可加载类型。

本阶段没有引入第二套事件系统、依赖注入容器或配置生成器。后续架构融合应明确对象所有权、初始化、更新和销毁边界，再接入现有模块。
