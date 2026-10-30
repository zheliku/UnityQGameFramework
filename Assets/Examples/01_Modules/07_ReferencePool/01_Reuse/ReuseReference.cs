using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示借出、归还、再借出。
    /// </summary>
    public sealed class ReuseReference : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text heldText, previousText, poolText;
        [SerializeField]
        private Button acquireButton, releaseButton;
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
            heldText.text = held == null ? "Holding: no crate" : $"Holding: crate #{held.Id}";
            previousText.text = previousId == 0 ? "Never released" : $"Last released: #{previousId}";
            poolText.text = "Idle: 0    Spawned: 0";
            foreach (var info in ReferencePool.GetAllReferencePoolInfos())
            {
                if (info.Type == typeof(Cargo))
                {
                    poolText.text = $"Idle: {info.UnusedReferenceCount}    Spawned: {info.UsingReferenceCount}";
                }
            }

            acquireButton.interactable = held == null;
            releaseButton.interactable = held != null;
        }

        /// <summary>
        /// 可被引用池复用的货物记录。
        /// </summary>
        public sealed class Cargo : IReference
        {
            // Cargo 是池中复用的对象，Id 用于识别实例。
            private static int nextId;

            /// <summary>
            /// 获取当前标识。
            /// </summary>
            public int Id { get; } = ++nextId;

            /// <summary>
            /// 重置数据，供引用池复用。
            /// </summary>
            public void Clear()
            {
            }
        }

        private void OnDestroy()
        {
            if (held != null)
            {
                ReferencePool.Release(held);
            }

            ReferencePool.RemoveAll<Cargo>();
        }
    }
}
