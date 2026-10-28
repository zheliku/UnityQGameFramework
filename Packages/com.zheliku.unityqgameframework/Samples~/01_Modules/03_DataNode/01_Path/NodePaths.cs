using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示路径与相对节点。
    /// </summary>
    public sealed class NodePaths : MonoBehaviour
    {
        [SerializeField] private Button relativeButton, absoluteButton;
        [SerializeField] private Text treeText;
        private const string ScorePath = "Example/Battle/Score";

        private void Start()
        {
            // SetData 会补齐中间节点；数值由节点持有，不需要自行归还引用池。
            QGF.DataNode.SetData<VarInt32>(ScorePath, 0);
            relativeButton.onClick.AddListener(() =>
            {
                int score = QGF.DataNode.GetData<VarInt32>(ScorePath).Value;
                QGF.DataNode.SetData<VarInt32>(ScorePath, score + 10);
            });
            absoluteButton.onClick.AddListener(() => QGF.DataNode.SetData<VarInt32>(ScorePath, 0));
        }

        private void Update()
        {
            treeText.text = $"Example / Battle / Score\nScore: {QGF.DataNode.GetData<VarInt32>(ScorePath).Value}";
        }
    }
}
