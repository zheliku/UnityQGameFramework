# 代码与生命周期

框架参考 UGF 的接口、命名和生命周期；示例参考 QFramework，直接展示用途明确的调用和结果。UGF 基础类型保留 `GameFramework` 前缀。Core 基础类型与模块接口使用 `UnityQGameFramework.Core`，模块及 Helper 通知使用 `UnityQGameFramework.Core.Events`；Unity 组件和游戏事件位于 `UnityQGameFramework.Runtime`，业务无需为同名通知编写事件别名。

## 调用与清理成对

| 取得或创建 | 对应清理 | 所有权 |
| --- | --- | --- |
| `Event.Subscribe` | `Unsubscribe` | 订阅者负责；流程在 OnEnter / OnLeave 配对 |
| `ReferencePool.Acquire<T>` | `Release` | 归还后不再持有；Fire 的事件参数由事件池回收 |
| `ObjectPool.Spawn` | `Unspawn` | 归还借用；Target 由登记条目的 Release 释放 |
| `Resource.LoadAssetAsync` | `UnloadAsset` | 一次成功加载对应一次归还；实例先销毁 |
| `LoadAssetHandleAsync` | `handle.Release()` | 调用者拥有此次句柄 |
| `Entity.ShowEntity` | `HideEntity` | 实例交给实体池，不自行 Destroy |
| `UI.OpenUIForm` | `CloseUIForm` | 实例交给 UI 池，关闭可继续复用 |
| `Scene.LoadScene` | `UnloadScene` | 场景模块管理资源句柄 |

EntityLogic 在 OnShow / OnHide 管理每次显示，UIFormLogic 在 OnOpen / OnClose 管理每次打开；OnInit 只执行实例首次初始化。缓存实例重新显示时不会重新执行 Awake。

异步操作传入业务生命周期的取消标记。离开界面后不再修改其 UI；取消等待与中止底层操作是否相同，以对应 API 的 XML 注释为准。事件参数会复用，需要长期保留时复制业务值。

## 集合查询

返回数组的方法创建结果容器；带 `NonAlloc` 的方法清空并填充调用方的 `List<T>`，不替换列表引用。无需 `out`。先给足容量并复用委托，才能避免扩容和闭包分配；结果中的对象仍是原对象。

数据表键使用 Luban 定义的实际类型，整数、字符串或复合键均可。生成表索引器适用于确定存在的键；允许缺键时用 `GetDataRow`。不要把所有键转换成字符串。

## 源码风格

四空格、独占行大括号；方法与访问器使用普通方法体，简单属性可保留自动属性。类型和公开成员使用简洁的中文 XML 注释，泛型参数、参数、返回值与签名一致。标识、界面文字与运行消息使用英文。

不修改第三方官方源码。YooAsset 使用官方收集与构建界面，QGF 适配放在自己的集成代码中。具体开发检查见仓库 `AGENTS.md`。
