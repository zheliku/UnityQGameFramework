using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class NodePaths : MonoBehaviour
    {
        [SerializeField] private Text treeText;
        [SerializeField] private Button relativeButton;
        [SerializeField] private Button absoluteButton;
        [SerializeField] private Button relativeTypedButton;
        [SerializeField] private Text pathText;
        [SerializeField] private Text compareText;
        private bool viewReady;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // SetData 自动补齐中间节点，VarInt32 由节点持有和回收。
                QGF.DataNode.SetData<VarInt32>("Example/Battle/Score", 100);
                relativeButton.onClick.AddListener(() =>
                {
                    // 不传起点时从根查找；传入 battle 后，Score 相对于该节点。
                    var battle = QGF.DataNode.GetNode("Example/Battle");
                    QGF.DataNode.SetData("Score", (Variable)(VarInt32)250, battle);
                });
                // 从根路径和从 Battle 相对写入，修改的是同一个 Score。
                absoluteButton.onClick.AddListener(() => QGF.DataNode.SetData<VarInt32>("Example/Battle/Score", 100));
                relativeTypedButton.onClick.AddListener(() =>
                    QGF.DataNode.SetData<VarInt32>("Score", 500, QGF.DataNode.GetNode("Example/Battle")));
                viewReady = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!viewReady) return;
            // GetData 返回节点持有的 Variable，这里只读取，不负责 Release。
            var battle = QGF.DataNode.GetNode("Example/Battle");
            var score = QGF.DataNode.GetNode("Score", battle);
            treeText.text = "Example\n    Battle\n        Score = " + QGF.DataNode.GetData<VarInt32>("Score", battle).Value;
            pathText.text = $"完整路径：{score.FullName}\n父节点：{score.Parent.Name}\n节点直接读取：{score.GetData<VarInt32>().Value}";
            compareText.text = "从根读取：" + QGF.DataNode.GetData("Example/Battle/Score")
                + "\n从 Battle 读取：" + QGF.DataNode.GetData("Score", battle);
        }
    }
}
