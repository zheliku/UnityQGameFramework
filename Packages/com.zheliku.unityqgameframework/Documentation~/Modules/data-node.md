# DataNode：按路径保存临时共享数据

DataNode 是一棵运行时内存树，节点可以保存 Variable 并继续拥有子节点。它适合在流程间传少量临时数据，不负责存盘，也不会自动刷新界面。

[DataNode 示例](../../Samples~/01_Modules/03_DataNode/) 保留路径读写和子节点管理两个场景。数据放在 `Example` 分支；需要替换或删除分支时可直接在同一管理器上调用对应 API。

## 保存本场分数

把下面完整流程放进 `DataNodeGuideProcedure.cs`，选择为入口流程：

```csharp
using UnityEngine;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace QgfDocs
{
    public sealed class DataNodeGuideProcedure : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            var nodes = QGF.DataNode;
            nodes.SetData<VarInt32>("Guide/Battle/Score", 100);
            int score = nodes.GetData<VarInt32>("Guide/Battle/Score").Value;
            Log.Info("本场分数：" + score);
            nodes.RemoveNode("Guide/Battle");
            Log.Info(nodes.GetNode("Guide/Battle") == null);
        }
    }
}
```

输出为 `本场分数：100` 和 `True`。SetData 会创建缺少的中间节点，不需要逐层添加 Guide、Battle 和 Score。

## 路径和节点

当前路径支持点、正斜线和反斜线作为分隔符。项目中建议统一用正斜线。

`GetNode(path)` 找不到时返回 null；`GetOrAddNode(path)` 会补齐路径。`GetData<T>(path)` 在节点不存在时会抛错，所以读取不确定存在的数据，先取得节点并判断，再读取其值。

带 `IDataNode node` 参数的重载从指定节点出发进行相对查找。它不是文件系统路径，没有 `..` 这种自动返回父目录的约定。

## 谁拥有 Variable

SetData 取得 Variable 的管理权，替换数据、移除节点或 Clear 时会归还原值。不要在 SetData 后再 Release，也不要把读出的 Variable 长期缓存。

`SetData<VarInt32>(path, 100)` 中，100 通过 VarInt32 的隐式转换从 ReferencePool 获取包装对象。读出的 `Value` 是自己的 int，可以保存；包装对象仍由节点管理。

不要把同一个 Variable 同时放到两个节点。也不要把 DataNode 当成 BindableProperty：改动 Value 不会自动触发事件或刷新 UI。需要通知时，业务显式发 Event，或直接调用自己的视图更新逻辑。

## 什么时候不需要 DataNode

对象内部的字段就放在对象里。某台 FSM 的状态间数据可以使用该 FSM 的 SetData。需要明确类型、多个行为和长期生命周期的背包、任务或战斗状态，普通 C# 业务对象通常更容易维护。

只清理自己负责的子树。`Clear()` 会清空整个数据树，不适合在一个局部界面关闭时调用。

源码：[DataNodeComponent](../../Runtime/Unity/DataNode/DataNodeComponent.cs)、[管理器](../../Runtime/Core/DataNode/DataNodeManager.cs)、[节点与值的回收](../../Runtime/Core/DataNode/DataNodeManager.DataNode.cs)。
