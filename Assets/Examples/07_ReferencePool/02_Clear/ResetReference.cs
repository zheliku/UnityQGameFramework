using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples
{
    public sealed class ResetReference : MonoBehaviour
    {
        [SerializeField] private Text rewardText, previousText;
        [SerializeField] private InputField coinsInput;
        [SerializeField] private Dropdown acquireMode;
        [SerializeField] private Button acquireButton, setButton, releaseButton;
        private Reward held;
        private int previousCoins;

        private void Start()
        {
            acquireMode.ClearOptions();
            acquireMode.AddOptions(new System.Collections.Generic.List<string> { "Acquire<Reward>", "Acquire(typeof(Reward))" });
            // 泛型与 Type 入口共用 Reward 的引用池。
            acquireButton.onClick.AddListener(() => held = acquireMode.value == 0
                ? ReferencePool.Acquire<Reward>() : (Reward)ReferencePool.Acquire(typeof(Reward)));
            setButton.onClick.AddListener(() =>
            {
                int.TryParse(coinsInput.text, out int coins);
                held.Coins = Mathf.Clamp(coins, 0, 999);
            });
            releaseButton.onClick.AddListener(() =>
            {
                previousCoins = held.Coins;
                // Release 会调用 Reward.Clear；下次借出时业务字段应已重置。
                ReferencePool.Release(held);
                held = null;
            });
        }

        private void Update()
        {
            rewardText.text = held == null ? "奖励在池中" : $"奖励 #{held.Id}\n当前金币：{held.Coins}\nClear 已执行：{held.ClearCalls} 次";
            previousText.text = $"上次归还前的金币：{previousCoins}";
            acquireButton.interactable = held == null;
            setButton.interactable = releaseButton.interactable = held != null;
        }

        public sealed class Reward : IReference
        {
            private static int nextId;
            public int Id { get; } = ++nextId;
            public int Coins;
            public int ClearCalls { get; private set; }
            public void Clear()
            {
                Coins = 0;
                // Id 和 Clear 次数用于观察复用过程。
                ClearCalls++;
            }
        }

        private void OnDestroy()
        {
            if (held != null) ReferencePool.Release(held);
            ReferencePool.RemoveAll<Reward>();
        }
    }
}
