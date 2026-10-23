using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class RemoveNodeBranch : MonoBehaviour
    {
        [SerializeField] private Text treeText;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button relativeButton;
        [SerializeField] private Button restoreButton;
        private bool viewReady;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // 节点接管 Variable；替换或移除节点时由模块归还旧值。
                QGF.DataNode.SetData<VarInt32>("Example/Battle/Score", 100);
                QGF.DataNode.SetData<VarString>("Example/Player/Name", "Pilot");
                // 移除 Battle 会连同 Score 子节点一起清理，Player 分支仍保留。
                removeButton.onClick.AddListener(() => QGF.DataNode.RemoveNode("Example/Battle"));
                relativeButton.onClick.AddListener(() =>
                    QGF.DataNode.RemoveNode("Battle", QGF.DataNode.GetNode("Example")));
                restoreButton.onClick.AddListener(() => QGF.DataNode.SetData<VarInt32>("Example/Battle/Score", 100));
                viewReady = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!viewReady) return;
            // GetNode 未找到时返回 null；GetData 要在确认节点存在后调用。
            bool hasBattle = QGF.DataNode.GetNode("Example/Battle") != null;
            treeText.text = "Example\n"
                + (hasBattle ? $"    Battle\n        Score = {QGF.DataNode.GetData<VarInt32>("Example/Battle/Score").Value}\n" : "    （Battle 已移除）\n")
                + "    Player\n        Name = " + QGF.DataNode.GetData<VarString>("Example/Player/Name").Value;
            removeButton.interactable = hasBattle;
            relativeButton.interactable = hasBattle;
            restoreButton.interactable = !hasBattle;
        }
    }
}
