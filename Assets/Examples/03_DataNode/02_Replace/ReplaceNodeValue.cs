using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ReplaceNodeValue : MonoBehaviour
    {
        [SerializeField] private Text valueText;
        [SerializeField] private Button addButton;
        [SerializeField] private Button textButton;
        [SerializeField] private Button resetButton;
        private bool viewReady;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // 节点接管 Variable；替换或移除节点时由模块归还旧值。
                QGF.DataNode.SetData<VarInt32>("Example/Score", 0);
                addButton.onClick.AddListener(() =>
                {
                    // 泛型指定 Variable 类型；读值前要保证节点存在且类型一致。
                    int score = QGF.DataNode.GetData<VarInt32>("Example/Score").Value;
                    // 只保留普通 int；再次 SetData 后，旧 Variable 已归还引用池。
                    QGF.DataNode.SetData<VarInt32>("Example/Score", score + 10);
                });
                // 同一个节点可替换为其他 Variable 类型；之后先重置成整数才能加分。
                textButton.onClick.AddListener(() => QGF.DataNode.SetData("Example/Score", (Variable)(VarString)"已结算"));
                resetButton.onClick.AddListener(() => QGF.DataNode.GetNode("Example/Score").SetData<VarInt32>(0));
                viewReady = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!viewReady) return;
            // 非泛型 GetData 返回 Variable，可检查运行时类型后决定允许的操作。
            var value = QGF.DataNode.GetData("Example/Score");
            valueText.text = $"Score = {value}\n当前类型：{value.GetType().Name}";
            addButton.interactable = value is VarInt32;
        }
    }
}
