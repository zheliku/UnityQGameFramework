using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示管理子节点。
    /// </summary>
    public sealed class ManageNodeChildren : MonoBehaviour
    {
        [SerializeField]
        private Button addButton;
        [SerializeField]
        private Button removeNameButton;
        [SerializeField]
        private Button removeIndexButton;
        [SerializeField]
        private Button clearNodeButton;
        [SerializeField]
        private Button clearAllButton;
        [SerializeField]
        private Text countText;
        [SerializeField]
        private Text childrenText;
        private bool viewReady;
        private readonly List<IDataNode> children = new List<IDataNode>();
        private int nextId;
        private void Start()
        {
            // 路径不存在时补齐中间节点，返回目标节点。
            QGF.DataNode.GetOrAddNode("Example/Inventory");
            addButton.onClick.AddListener(() =>
            {
                // 全树清空后重新创建 Inventory，再添加子节点并交给它管理数据。
                var inventory = QGF.DataNode.GetOrAddNode("Inventory", QGF.DataNode.GetOrAddNode("Example"));
                var item = inventory.GetOrAddChild("Item" + ++nextId);
                item.SetData((Variable)(VarString)"Potion");
            });
            removeNameButton.onClick.AddListener(() =>
            {
                // HasChild(0) 检查第一个子节点，再按它的名称移除。
                var inventory = QGF.DataNode.GetNode("Example/Inventory");
                if (inventory != null && inventory.HasChild(0))
                {
                    inventory.RemoveChild(inventory.GetChild(0).Name);
                }
            });
            removeIndexButton.onClick.AddListener(() =>
            {
                var inventory = QGF.DataNode.GetNode("Example/Inventory");
                if (inventory != null && inventory.HasChild(0))
                {
                    inventory.RemoveChild(0);
                }
            });
            // Clear 归还当前值和全部子节点，但保留 Inventory 节点本身。
            clearNodeButton.onClick.AddListener(() => QGF.DataNode.GetNode("Example/Inventory")?.Clear());
            // 这份场景独占 DataNode；共享游戏中的业务清理应使用上一个示例的 RemoveNode。
            clearAllButton.onClick.AddListener(() => QGF.DataNode.Clear());
            viewReady = true;
        }

        private void Update()
        {
            if (!viewReady)
            {
                return;
            }

            var inventory = QGF.DataNode.GetNode("Example/Inventory");
            string text = "Root branches: " + QGF.DataNode.Root.ChildCount;
            children.Clear();
            // GetAllChild 支持写入 List 或返回数组，下面对照两种结果。
            inventory?.GetAllChildNonAlloc(children);
            // 节点可以继续添加；界面只预览前四个，避免长列表挤出面板。
            for (int i = 0; i < Mathf.Min(children.Count, 4); i++)
            {
                var child = children[i];
                text += $"\n{child.Name} = {child.GetData()}  [{child.FullName}]";
            }

            if (children.Count > 4)
            {
                text += $"\nAdditional {children.Count - 4} nodes";
            }

            childrenText.text = text;
            countText.text = inventory == null ? "Inventory does not exist. Adding an item will create it again." : $"Children: {inventory.ChildCount}; array result: {inventory.GetAllChild().Length}\n" + $"Item1：{(inventory.HasChild("Item1") ? inventory.GetChild("Item1").ToDataString() : "Missing")}";
            removeNameButton.interactable = children.Count > 0;
            removeIndexButton.interactable = children.Count > 0;
            clearNodeButton.interactable = inventory != null;
            clearAllButton.interactable = QGF.DataNode.Root.ChildCount > 0;
        }
    }
}
