using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples
{
    public sealed class ManageReferences : MonoBehaviour
    {
        [SerializeField] private Text countsText, totalsText, heldText;
        [SerializeField] private Dropdown apiMode;
        [SerializeField] private Button addButton, removeButton, clearButton, borrowButton;
        private CapacityEntry held;

        private void Start()
        {
            apiMode.ClearOptions();
            apiMode.AddOptions(new System.Collections.Generic.List<string> { "泛型入口", "Type 入口" });
            // Add 预先创建空闲引用；Remove 裁剪空闲部分，不改变借出计数。
            addButton.onClick.AddListener(() =>
            {
                if (apiMode.value == 0) ReferencePool.Add<CapacityEntry>(3);
                else ReferencePool.Add(typeof(CapacityEntry), 3);
            });
            removeButton.onClick.AddListener(() =>
            {
                if (apiMode.value == 0) ReferencePool.Remove<CapacityEntry>(2);
                else ReferencePool.Remove(typeof(CapacityEntry), 2);
            });
            clearButton.onClick.AddListener(() =>
            {
                // RemoveAll 只移除空闲引用，不会收走已经借出的 held。
                if (apiMode.value == 0) ReferencePool.RemoveAll<CapacityEntry>();
                else ReferencePool.RemoveAll(typeof(CapacityEntry));
            });
            borrowButton.onClick.AddListener(() =>
            {
                if (held == null) held = ReferencePool.Acquire<CapacityEntry>();
                else { ReferencePool.Release(held); held = null; }
            });
        }

        private void Update()
        {
            countsText.text = "空闲：0    借出：0";
            totalsText.text = "尚无池记录";
            foreach (var info in ReferencePool.GetAllReferencePoolInfos())
            {
                if (info.Type != typeof(CapacityEntry)) continue;
                countsText.text = $"空闲：{info.UnusedReferenceCount}    借出：{info.UsingReferenceCount}";
                totalsText.text = $"Acquire：{info.AcquireReferenceCount}    Release：{info.ReleaseReferenceCount}\n"
                    + $"创建：{info.AddReferenceCount}    移除：{info.RemoveReferenceCount}";
            }
            heldText.text = held == null ? "手中没有引用" : $"手中持有 #{held.Id}，清空空闲池也不影响它";
            borrowButton.GetComponentInChildren<Text>().text = held == null ? "借出一个" : "归还手中的引用";
        }

        public sealed class CapacityEntry : IReference
        {
            private static int nextId;
            public int Id { get; } = ++nextId;
            public void Clear() { }
        }

        private void OnDestroy()
        {
            if (held != null) ReferencePool.Release(held);
            ReferencePool.RemoveAll<CapacityEntry>();
        }
    }
}
