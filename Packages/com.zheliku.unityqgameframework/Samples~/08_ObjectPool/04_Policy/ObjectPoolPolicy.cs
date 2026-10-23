using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.ObjectPool;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ObjectPoolPolicy : MonoBehaviour
    {
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private Dropdown selectedCube, argumentMode, policyMode;
        [SerializeField] private Toggle lockedToggle, releaseAllowedToggle;
        [SerializeField] private Slider prioritySlider;
        [SerializeField] private Button createButton, applyButton;
        [SerializeField] private Text stateText, resultText;
        private IObjectPool<PolicyCubeItem> pool;
        private readonly PolicyCubeItem[] items = new PolicyCubeItem[3];
        private readonly string[] names = { "Temp A", "Temp B", "Keep" };

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // 完整参数：池名、检查间隔、容量、过期秒数、池优先级。
                // 每 0.25 秒检查一次，初始不过期；后面用下拉框调整策略。
                pool = QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>("Policy", .25f, 8, float.MaxValue, 0);
                selectedCube.ClearOptions(); selectedCube.AddOptions(new List<string>(names));
                argumentMode.ClearOptions();
                argumentMode.AddOptions(new List<string> { "传登记条目 item", "传目标 item.Target" });
                policyMode.ClearOptions();
                policyMode.AddOptions(new List<string>
                {
                    "Capacity = 1", "空闲 2 秒过期", "Release(1)", "ReleaseAllUnused()",
                    "自定义筛选全部 Temp", "自定义筛选一个 Temp", "Release() 按当前策略回收"
                });
                selectedCube.onValueChanged.AddListener(_ => ReadSelection());
                lockedToggle.onValueChanged.AddListener(value =>
                {
                    var item = items[selectedCube.value];
                    // 锁定同一个条目，可以传条目本身，也可以传它持有的 Target。
                    if (argumentMode.value == 0) pool.SetLocked(item, value);
                    else pool.SetLocked(item.Target, value);
                });
                releaseAllowedToggle.onValueChanged.AddListener(value => items[selectedCube.value].AllowRelease = value);
                prioritySlider.wholeNumbers = true;
                prioritySlider.onValueChanged.AddListener(value =>
                {
                    var item = items[selectedCube.value];
                    // 这里调整条目优先级；池的 Priority 用于管理器批量回收时的池排序。
                    if (argumentMode.value == 0) pool.SetPriority(item, (int)value);
                    else pool.SetPriority(item.Target, (int)value);
                });
                createButton.onClick.AddListener(() =>
                {
                    pool.Capacity = 8; pool.ExpireTime = float.MaxValue;
                    for (int i = 0; i < names.Length; i++)
                    {
                        var cube = Instantiate(cubePrefab, new Vector3(2 + i * 1.5f, 0, 0), Quaternion.Euler(20, 35, 0));
                        cube.name = names[i];
                        items[i] = PolicyCubeItem.Create(names[i], cube, i * 5);
                        pool.Register(items[i], false); // 全部为空闲，观察哪些会被策略真正销毁。
                    }
                    ReadSelection();
                });
                applyButton.onClick.AddListener(() =>
                {
                    // 锁定或自定义拒绝释放的目标先被排除；默认再按优先级、最后使用时间淘汰。
                    switch (policyMode.value)
                    {
                        // 这两个属性的 setter 都立即执行一次 Release；已有空闲条目可能当场释放。
                        case 0: pool.Capacity = 1; break;
                        case 1: pool.ExpireTime = 2; break;
                        case 2: pool.Release(1); break;
                        case 3: pool.ReleaseAllUnused(); break;
                        // 自定义回调返回哪些条目，就尝试释放哪些条目；此处选择所有空闲 Temp。
                        case 4:
                            pool.Release((candidates, requested, expiry) =>
                                candidates.FindAll(item => item.Name.StartsWith("Temp", StringComparison.Ordinal)));
                            break;
                        case 5: pool.Release(1, SelectTemporary); break;
                        case 6:
                            // 无参 Release 按当前容量和过期时间筛选；未超容量且未过期时保留对象。
                            pool.Release();
                            resultText.text = $"按容量 {pool.Capacity} 与空闲期限回收，当前保留 {pool.Count} 个 Cube。";
                            return;
                    }
                    resultText.text = "执行后观察 Cube 是否消失。受保护对象可以保留，数量不一定降到 Capacity。";
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void ReadSelection()
        {
            var item = items[selectedCube.value];
            if (item == null) return;
            lockedToggle.SetIsOnWithoutNotify(item.Locked);
            releaseAllowedToggle.SetIsOnWithoutNotify(item.CustomCanReleaseFlag);
            prioritySlider.SetValueWithoutNotify(item.Priority);
            // LastUseTime 使用 UTC；这里只读取仍在池中的条目，不保留释放后的引用。
            string label = pool.FullName.Replace(typeof(PolicyCubeItem).Namespace + ".", string.Empty);
            resultText.text = $"{item.Name} 最后使用（UTC）{item.LastUseTime:HH:mm:ss}\n池标识：{label}";
        }

        private List<PolicyCubeItem> SelectTemporary(List<PolicyCubeItem> candidates, int requested, DateTime expiry)
        {
            // candidates 已排除借用中和受保护的条目；这里最多返回 requested 个 Temp。
            var selected = new List<PolicyCubeItem>();
            foreach (var item in candidates)
                if (item.Name.StartsWith("Temp", StringComparison.Ordinal) && selected.Count < requested) selected.Add(item);
            return selected;
        }

        private void Update()
        {
            if (pool == null) return;
            var infos = ((ObjectPoolBase)pool).GetAllObjectInfos();
            // 用快照判断条目是否仍属于池，释放后的 ObjectBase 可能已经被其他业务复用。
            for (int i = 0; i < names.Length; i++)
                if (!Array.Exists(infos, info => info.Name == names[i])) items[i] = null;
            bool exists = items[selectedCube.value] != null;
            selectedCube.interactable = pool.Count > 0;
            lockedToggle.interactable = releaseAllowedToggle.interactable = prioritySlider.interactable = exists;
            createButton.interactable = pool.Count == 0;
            applyButton.interactable = pool.Count > 0;
            // ObjectType 是登记条目类型；本例的 Target 才是 GameObject。
            var itemStates = new List<string>();
            foreach (var info in infos)
            {
                string protection = info.Locked ? "锁定" : info.CustomCanReleaseFlag ? string.Empty : "保留";
                itemStates.Add($"{info.Name} P{info.Priority} {protection} {(DateTime.UtcNow - info.LastUseTime).TotalSeconds:F1}s");
            }
            string expiry = pool.ExpireTime == float.MaxValue ? "不过期" : $"空闲 {pool.ExpireTime:g} 秒过期";
            stateText.text = $"{pool.Name} / {pool.ObjectType.Name} / 池优先级 {pool.Priority}\n"
                + $"登记 {pool.Count} · 可释放 {pool.CanReleaseCount}/{pool.Capacity}\n"
                + string.Join(" / ", itemStates) + "\n"
                + $"每 {pool.AutoReleaseInterval:g} 秒检查 · {expiry}";
        }

        private void OnDestroy()
        {
            var manager = GameEntry.GetComponent<ObjectPoolComponent>();
            if (manager != null && pool != null) manager.DestroyObjectPool(pool);
        }
    }
}
