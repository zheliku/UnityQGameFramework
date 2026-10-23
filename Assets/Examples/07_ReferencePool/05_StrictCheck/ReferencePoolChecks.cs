using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples
{
    public sealed class ReferencePoolChecks : MonoBehaviour
    {
        [SerializeField] private Toggle strictToggle;
        [SerializeField] private Button checkButton, clearButton;
        [SerializeField] private Text resultText, poolsText;
        private bool originalStrict;

        private void Start()
        {
            originalStrict = ReferencePool.EnableStrictCheck;
            strictToggle.SetIsOnWithoutNotify(originalStrict);
            strictToggle.onValueChanged.AddListener(value => ReferencePool.EnableStrictCheck = value);
            checkButton.onClick.AddListener(() =>
            {
                var value = ReferencePool.Acquire<CheckedEntry>();
                ReferencePool.Release(value);
                // 故意演示错误，只能在严格检查开启时执行，避免污染池中的空闲队列。
                try { ReferencePool.Release(value); }
                catch (FrameworkException) { resultText.text = "第二次 Release 被严格检查拒绝，借出计数仍是零。"; }
            });
            clearButton.onClick.AddListener(() =>
            {
                // ClearAll 是全局操作。只在所有引用都已归还的独立实验中使用。
                foreach (var info in ReferencePool.GetAllReferencePoolInfos())
                    if (info.UsingReferenceCount != 0) return;
                ReferencePool.ClearAll();
                resultText.text = "全部类型集合已清理；下次 Acquire 会重新创建。";
            });
        }

        private void Update()
        {
            bool canClear = true;
            poolsText.text = $"类型集合：{ReferencePool.Count}";
            foreach (var info in ReferencePool.GetAllReferencePoolInfos())
            {
                poolsText.text += $"\n{info.Type.Name}：空闲 {info.UnusedReferenceCount}，借出 {info.UsingReferenceCount}";
                canClear &= info.UsingReferenceCount == 0;
            }
            checkButton.interactable = ReferencePool.EnableStrictCheck;
            clearButton.interactable = canClear;
        }

        public sealed class CheckedEntry : IReference { public void Clear() { } }

        private void OnDestroy()
        {
            ReferencePool.EnableStrictCheck = originalStrict;
            ReferencePool.RemoveAll<CheckedEntry>();
        }
    }
}
