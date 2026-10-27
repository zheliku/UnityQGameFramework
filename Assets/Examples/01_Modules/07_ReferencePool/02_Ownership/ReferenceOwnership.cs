using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示谁负责归还变量。
    /// </summary>
    public sealed class ReferenceOwnership : MonoBehaviour
    {
        [SerializeField]
        private Text localText, nodeText;
        [SerializeField]
        private Button acquireButton, releaseButton, transferButton, removeButton;
        private VarInt32 local;
        private bool ready;
        private void Start()
        {
            // int 转 VarInt32 时会从引用池取得包装对象；目前由本地负责归还。
            acquireButton.onClick.AddListener(() => local = 7);
            releaseButton.onClick.AddListener(() =>
            {
                ReferencePool.Release(local);
                local = null;
            });
            transferButton.onClick.AddListener(() =>
            {
                // SetData 接管同一个包装对象。交接后本地不再负责 Release。
                QGF.DataNode.SetData<VarInt32>("Example/Owned", local);
                local = null;
            });
            // 移除整条分支及其值，其他分支保持不变。
            removeButton.onClick.AddListener(() => QGF.DataNode.RemoveNode("Example/Owned"));
            ready = true;
        }

        private void Update()
        {
            if (!ready)
            {
                return;
            }

            // GetNode 未找到时返回 null，不会创建节点。
            bool hasNode = QGF.DataNode.GetNode("Example/Owned") != null;
            localText.text = local == null ? "本地：没有持有变量" : $"本地持有：{local.Value}，本地负责归还";
            nodeText.text = hasNode ? $"节点持有：{QGF.DataNode.GetData<VarInt32>("Example/Owned").Value}，DataNode 负责归还" : "节点：没有持有变量";
            acquireButton.interactable = local == null;
            releaseButton.interactable = transferButton.interactable = local != null;
            removeButton.interactable = hasNode;
        }

        private void OnDestroy()
        {
            if (local != null)
            {
                ReferencePool.Release(local);
            }

            GameEntry.GetComponent<DataNodeComponent>()?.RemoveNode("Example/Owned");
        }
    }
}
