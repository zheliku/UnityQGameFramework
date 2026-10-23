using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples
{
    public sealed class ReuseReference : MonoBehaviour
    {
        [SerializeField] private Text heldText, previousText, poolText;
        [SerializeField] private Button acquireButton, releaseButton;
        private Cargo held;
        private int previousId;

        private void Start()
        {
            // Acquire 优先复用空闲引用；没有空闲对象时才创建。
            acquireButton.onClick.AddListener(() => held = ReferencePool.Acquire<Cargo>());
            releaseButton.onClick.AddListener(() =>
            {
                // 归还之后只保存编号，不再读写这份对象。
                previousId = held.Id;
                ReferencePool.Release(held);
                held = null;
            });
        }

        private void Update()
        {
            heldText.text = held == null ? "手中：没有货箱" : $"手中：货箱 #{held.Id}";
            previousText.text = previousId == 0 ? "尚未归还过" : $"上次归还：#{previousId}";
            poolText.text = "空闲：0    借出：0";
            foreach (var info in ReferencePool.GetAllReferencePoolInfos())
                if (info.Type == typeof(Cargo)) poolText.text = $"空闲：{info.UnusedReferenceCount}    借出：{info.UsingReferenceCount}";
            acquireButton.interactable = held == null;
            releaseButton.interactable = held != null;
        }

        public sealed class Cargo : IReference
        {
            // Cargo 是池中复用的对象，Id 用于识别实例。
            private static int nextId;
            public int Id { get; } = ++nextId;
            public void Clear() { }
        }

        private void OnDestroy()
        {
            if (held != null) ReferencePool.Release(held);
            ReferencePool.RemoveAll<Cargo>();
        }
    }
}
