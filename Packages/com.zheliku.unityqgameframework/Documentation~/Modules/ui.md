# UI：打开、关闭和复用一个窗口

UI 管理窗口的加载、缓存、分组和显示次序。业务窗口继承 `UIFormLogic`，在生命周期回调里显示数据、注册按钮并清理订阅。

[UI 示例](../../Samples~/14_UI/) 使用 uGUI 场景和预制体，可操作窗口打开与取消、关闭后复用、覆盖与置顶，以及多窗口和分组查询。

UIComponent → UIManager → Resource / ObjectPool → UIFormHelper → UIFormLogic，是一次界面打开的主要链路。和 Entity 不同，UI 的逻辑脚本挂在预制体上，OpenUIForm 不传逻辑类型。

## 创建一个最小窗口

创建 Canvas，设为 Screen Space - Overlay。添加一个 Text 作为标题、一个 Button 作为关闭按钮。把根节点保存为 `Assets/MyGame/UI/GuideWindow.prefab`，删除场景中的原实例。

在预制体根节点挂 `GuideWindow`，并把关闭按钮的 OnClick 绑定到它的 Close 方法：

```csharp
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace QgfDocs
{
    public sealed class GuideWindow : UIFormLogic
    {
        private Canvas canvas;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            Log.Info("打开窗口：" + userData);
        }

        protected override void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
        {
            base.OnDepthChanged(uiGroupDepth, depthInUIGroup);
            canvas.sortingOrder = uiGroupDepth * 1000 + depthInUIGroup;
        }

        public void Close() => QGF.UI.CloseUIForm(UIForm);
    }
}
```

这是单个根 Canvas 的示例。复杂界面还需要处理子 Canvas 的排序、CanvasScaler 和安全区域。StarForce 在 UGuiForm、UGuiGroupHelper 中实现了自己的规则，这些类属于示例，不是框架的强制 UI 基类。

创建以下入口流程 `UiGuideProcedure.cs`：

```csharp
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace QgfDocs
{
    public sealed class UiGuideProcedure : ProcedureBase
    {
        private UIComponent ui;
        private int serialId;

        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            ui = QGF.UI;
            if (!ui.HasUIGroup("Guide")) ui.AddUIGroup("Guide", 0);
            serialId = ui.OpenUIForm("Assets/MyGame/UI/GuideWindow.prefab", "Guide", "第一次打开");
        }

        protected override void OnLeave(IFsm<IProcedureManager> owner, bool isShutdown)
        {
            if (ui.HasUIForm(serialId) || ui.IsLoadingUIForm(serialId)) ui.CloseUIForm(serialId);
            base.OnLeave(owner, isShutdown);
        }
    }
}
```

场景需要框架预制体、摄像机和 EventSystem。EventSystem 的输入模块要与项目输入后端匹配：旧输入使用 StandaloneInputModule，Input System 使用其对应 UI 输入模块。开启编辑器资源模式后运行，看到窗口并可通过按钮关闭。

## 序列号与资源名

OpenUIForm 返回这一次打开的 serialId。相同预制体可以打开多个实例，因此需要关闭某次请求时优先按 serialId 操作，不要仅凭资源名取到“任意一个窗口”。

结果通过 `OpenUIFormSuccessEventArgs` / `OpenUIFormFailureEventArgs` 通知。UserData 用来区分调用者；取到序列号不代表 UIForm 已存在。仍在加载时 CloseUIForm(serialId) 会登记关闭意图。

UI 组件的五种事件通知都有独立开关。成功、失败、关闭完成默认开启；进度与依赖默认关闭。若逻辑依赖成功事件继续执行，保留成功通知；详见[事件开关](event.md)。

四个示例在控制脚本销毁时按自己的序号关闭窗口，也取消尚未完成的打开请求。复用示例的等待随脚本取消，退出后不会再次打开。`CloseAllLoadedUIForms` 用于明确需要关闭全部窗口的操作，单个控制器退出时用自己保存的序号。

## 暂停、覆盖和恢复

UIGroup 维护该组的界面次序。`pauseCoveredUIForm` 控制新界面是否暂停被覆盖的界面；被覆盖与被暂停不是完全一样的状态。

- OnCover / OnReveal：覆盖关系发生变化。
- OnPause / OnResume：暂停状态变化；默认逻辑会改变可见性。
- OnDepthChanged：界面在组中的排序变化，业务 UI 基类据此更新 Canvas。
- OnRefocus：由管理器请求已有界面重新聚焦时处理传入数据。

`IUIGroup.Helper` 返回组辅助器，组件按 Inspector 中的 Helper 类型或实例创建它。默认 UIGroupHelper 的 SetDepth 留作扩展；示例窗口在 `OnDepthChanged` 中把组深度和组内深度写入 Canvas.sortingOrder。

分组查询同时显示每个组的实际辅助器类型，可用来确认 Inspector 中选择的实现。读取 Helper 不会新建窗口或改变显示次序；业务调整组深度仍通过 `group.Depth`，由框架通知辅助器和窗口。

第三个示例在背包上显示 OnUpdate 累计时间。覆盖但不暂停时计时继续；暂停后默认逻辑隐藏背包，计时停止。关闭上层后恢复原实例。Refocus 把已有背包移到同组顶层，并触发 OnRefocus，不会创建新序号。计数包含首次显示时的 OnResume 与 OnReveal。

第四个示例将相同预制体分别打开到 Hud 和 Popup。组间不会通过 pauseCoveredUIForm 相互暂停；UIGroup.Pause 作用于整个组。改变组深度后，OnDepthChanged 将组深度与组内深度组合到 Canvas.sortingOrder。数组和 List 查询覆盖全部窗口、同资源窗口和指定组；按资源名取首个窗口不能代替保存业务所需的打开序号。

## 关闭与销毁是两步

关闭执行 OnClose，实例进入内部缓存，之后可能再用。OnInit 用于首次初始化；每次打开的数据、订阅和临时引用在 OnOpen 绑定，在 OnClose 解除。自行覆盖回调时记得调用 base。

不要直接 Destroy 窗口、仅将它 SetActive(false)，或把 Visible=false 当成关闭。这样管理器仍可能认为窗口已打开。正常关闭用 CloseUIForm；最终实例释放由内部池和 Helper 完成。

第二个示例把空闲过期设为一秒，分别观察立即重开、等待过期和锁定缓存。关闭后的实例在框架更新时回池，因此例子在下一帧末再重新打开。锁定防止过期释放，不会阻止 Close 结束本次序号。SetUIFormInstancePriority 影响实例释放排序，与 OpenUIForm 的资源加载优先级分工不同。

复用面板同时列出 OnInit、OnOpen、OnClose、OnRecycle 计数。缓存实例重新打开时，OnInit 不重复；每次打开和关闭的数据绑定分别放在 OnOpen、OnClose。

源码：[UIComponent](../../Runtime/Unity/UI/UIComponent.cs)、[UIFormLogic](../../Runtime/Unity/UI/UIFormLogic.cs)、[UIManager](../../Runtime/Core/UI/UIManager.cs)、[默认分组辅助器](../../Runtime/Unity/UI/DefaultUIGroupHelper.cs)。
