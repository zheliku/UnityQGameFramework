# 在项目里怎样使用这些模块

流程、窗口与实体按各自的生命周期绑定数据和事件，退出时清理自己创建的对象。下面约定代码位置、调用时机和释放责任。

## 1. 先定好代码和资源放在哪里

```text
Assets/MyGame/
  Launcher.unity
  Scripts/
    Entry/          业务入口与组件缓存
    Procedures/     启动、登录、菜单、战斗等流程
    Entities/       EntityLogic 与实体业务数据
    UI/             UIFormLogic 与界面行为
    Services/       普通 C# 业务服务
    Events/         自己的 GameEventArgs
  Prefabs/
  Scenes/
  Audio/
Assets/UnityQGameFramework/Config/
  Source/           Excel 与 luban.conf
  Generated/        生成代码
  Data/             bytes 与 Catalog
  Extensions/       手写的表扩展
  Editor/           LubanProfile.asset
```

业务代码与资源放在 Assets，目录名可以按项目调整。更换配置目录时更新 Profile 和相关引用；Packages 提供框架代码与示例模板。

生成文件只由生成器改写；手写扩展放进 Extensions。多个 Profile 可以输出不同业务配置，但不要在同一工程生成两组相同命名空间、相同类型名的代码。

## 2. 入口只做组件获取，业务初始化放进流程

自己的 GameEntry 可以像 StarForce 一样缓存组件。缓存组件与加载游戏配置是两步：前者取得引用，后者必须等框架初始化完成。

| 位置                                     | 适合做什么                                                   |
| ---------------------------------------- | ------------------------------------------------------------ |
| MonoBehaviour.Awake                      | 获取自己的 Transform、序列化引用；不调用尚未就绪的跨模块业务 |
| 入口 Procedure.OnEnter                   | 启动配置加载、连接业务依赖、进入后续流程                     |
| Procedure.OnUpdate                       | 根据完成标记推进阶段                                         |
| EntityLogic.OnShow / UIFormLogic.OnOpen  | 绑定本次显示的数据和事件                                     |
| EntityLogic.OnHide / UIFormLogic.OnClose | 取消本次显示的订阅，释放本次显示的业务引用                   |

一个流程负责一个业务阶段，例如加载登录界面。商店价格、敌人 AI 和按钮行为放进各自的业务对象或脚本，由流程安排它们的启动与退出。

## 3. 异步操作：请求号、结果和归属要分开

常用组件通过 `QGF.Config`、`QGF.Fsm`、`QGF.Event` 等入口取得，调用时遵守上表的初始化时机。可选依赖使用 `GameEntry.GetComponent<T>()` 检查是否存在；纯 C# 宿主按 Core 接口取得模块。

`OpenUIForm`、`PlaySound`、`AddDownload`、`AddWebRequest` 等方法返回的整数通常是请求或播放序列号。拿到号码并不表示任务已经完成。

`ShowEntity` 的实体编号由调用者指定；资源表中的 Entity Id 标识配置记录。多个运行时实体可以引用同一条资源配置记录。

异步事件通常携带 `UserData`。给自己的请求传一个归属对象，并在回调中检查它，可以避免同时加载其他内容时误处理结果。

```csharp
// 代码片段：放在事件回调里。
var args = (UnityQGameFramework.Runtime.LoadSceneSuccessEventArgs)e;
if (!ReferenceEquals(args.UserData, this)) return;
```

异步完成通知可能在流程离开后到达。支持取消的模块在离开时取消请求；其他模块按其接口标记请求失效或延后隐藏。回调继续流程前，先确认请求仍属于当前流程。

## 4. 成对调用，按所有权释放

| 创建或取得                                   | 对应结束操作                   | 结束后不要做什么                 |
| -------------------------------------------- | ------------------------------ | -------------------------------- |
| Event.Subscribe                              | Event.Unsubscribe              | 保留关闭界面的订阅               |
| ReferencePool.Acquire                        | ReferencePool.Release          | 再读已释放对象的字段             |
| ObjectPool.Spawn                             | ObjectPool.Unspawn             | 用 Release 代替 Unspawn          |
| Resource.LoadAsset                           | Resource.UnloadAsset           | 卸载以后继续创建依赖该资源的实例 |
| Entity.ShowEntity                            | Entity.HideEntity              | 直接 Destroy 受管理的实例        |
| UI.OpenUIForm                                | UI.CloseUIForm                 | 直接销毁或仅 SetActive(false)    |
| Scene.LoadScene                              | Scene.UnloadScene              | 只清业务状态而留下场景           |
| Sound.PlaySound                              | Sound.StopSound 或自然播放结束 | 把播放序列号当成音效配置编号     |
| Fsm.CreateFsm                                | Fsm.DestroyFsm                 | 让离开场景的状态机继续更新       |
| FileSystem.LoadFileSystem / CreateFileSystem | FileSystem.DestroyFileSystem   | 用完后只丢弃接口引用             |

这张表需要结合各章阅读。例如 Config、Localization 的数据由其数据提供器读取和释放，不需要业务再手动卸载它们内部加载的 TextAsset；Entity 和 UI 的资源也由各自内部池管理。

## 5. 订阅与对象池的两条规则

事件处理函数使用具名方法；订阅和取消时传入同一个方法，保证取消目标与注册目标一致。

事件参数和许多框架数据由 ReferencePool 管理。处理函数在回调期间读取事件参数；跨回调保留时，复制所需值或写入自己拥有的数据对象。

EntityLogic.OnInit、UIFormLogic.OnInit 一般用于首次实例初始化；实例复用时不会再次执行。每次显示的数据、临时监听、计时和外部引用，应在 OnShow / OnOpen 中重新绑定，在 OnHide / OnClose 中解除。

## 6. 配置与玩家数据分开

Luban 表描述设计数据。扣除背包里的道具数量，应修改背包业务对象，不应改生成表记录。Config 描述已加载的全局参数；Setting 保存少量本地偏好。大存档可以使用自己的存档格式和文件读写方案。

Setting 默认保存在持久化目录的 `GameFrameworkSetting.dat`。需要加密或服务端校验时，在游戏的数据保存流程中处理。Network 和 WebRequest 提供传输接口，认证与重试策略由业务实现。

## 7. 时间和线程

业务 OnUpdate 按用途选择逻辑时间或实际时间。游戏暂停后仍需更新的 UI 动画、网络超时和下载进度使用实际时间。

大多数组件和 Unity API 都应从主线程调用。Event.Fire 的事件入队支持跨线程，不等于整个框架都可跨线程调用。工作线程不要直接 ShowEntity、OpenUIForm 或访问 GameObject；把结果带回主线程再处理。

Unity 工程的 Core 更新统一由 BaseComponent 驱动。

## 8. 命名与程序集

- 类、接口、公开方法与属性采用 PascalCase；接口以 I 开头。
- 事件参数类以 EventArgs 结尾，并提供明确的 EventId。
- 业务类型使用项目自己的 namespace。
- 不需要 Inspector、字段级序列化或附加逻辑时，可以使用自动属性；Inspector 字段保留 `[SerializeField]`。
- Editor 代码放进 Editor 程序集，Player 业务不引用 `UnityQGameFramework.Editor`。
- `#if UNITY_EDITOR` 用于隔离依赖 `UnityEditor` API 或编辑器专用辅助类的代码，不是每个脚本都要加。独立编辑器工具放入 Editor 文件夹或 Editor 程序集；整份 MonoBehaviour 被宏包住时，该类型不会编译进 Player。需要在 Player 使用的组件应保留在运行程序集，并提供 Player 可用的资源路径与加载实现。
- 自己的 asmdef 按使用的功能引用 Core、Runtime、Luban；可选插件由使用它的程序集引用。

资源位置由 YooAsset 的 ResourceBuildProfile 决定。启用包的 `EnableAddressable` 并使用 `AddressByFileName` 时，传文件名且不带扩展名；启用 `IncludeAssetGUID` 时也可以传 Unity 资产 GUID。不要把 `Assets/...` 文件路径、YooAsset 地址和网络 URL 混用。

## 9. 加入一个功能时如何验收

以背包界面为例，至少检查这些行为：

1. 没打开界面时，游戏不会等待它的回调才能继续。
2. 首次打开和对象池复用后的再次打开，数据一致。
3. 快速打开再关闭，不会在加载完成后意外重新显示旧界面。
4. 关闭后修改背包，不再调用已关闭界面的监听函数。
5. 资源加载失败时有具体资源路径和失败原因，流程不会永远停在“加载中”。

Editor Simulation 验证玩法和资源收集；Player 还要验证实际包构建、启动场景、平台与网络环境。两者不能互相替代。

## 10. 文档和示例的写法

每章从一件能完成的小事开始，写清创建哪些文件、挂到哪个节点、配置哪些字段、运行后应看到什么。完整类与需要放入现有类的方法片段要明确区分；提供资源路径时说明它是示例路径。

示例里的错误分支也要可读。实际结果与预期不符时，先看自己的请求是否发出、资源是否成功、回调是否属于自己、对象是否已离开生命周期，再去检查模块内部。
