using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.ObjectPool;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed partial class InspectObjectPools : MonoBehaviour
    {
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private Text poolsText, resultText;
        [SerializeField] private Dropdown queryMode, destroyMode, poolReleaseMode;
        [SerializeField] private Button createButton, queryButton, destroyButton, releaseButton, clearButton, releasePoolButton;
        [SerializeField] private Dropdown creationKind, creationArguments;
        [SerializeField] private Button compareButton, removeComparisonButton;
        [SerializeField] private Text comparisonText;
        private readonly List<ObjectPoolBase> results = new List<ObjectPoolBase>();
        private ObjectPoolBase comparisonPool;
        private bool ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                queryMode.ClearOptions();
                queryMode.AddOptions(new List<string> { "泛型：无名称", "Type：无名称", "泛型：Left", "Type：Left", "条件：Left",
                    "条件筛选 → 数组", "条件筛选 → List", "全部 → 数组", "全部 → List", "按优先级排序 → 数组", "按优先级排序 → List" });
                destroyMode.ClearOptions();
                destroyMode.AddOptions(new List<string>
                {
                    "泛型：无名称", "Type：无名称", "泛型与 Left", "Type 与 Left", "IObjectPool 引用", "ObjectPoolBase 引用"
                });
                poolReleaseMode.ClearOptions();
                poolReleaseMode.AddOptions(new List<string> { "Left：Release()", "Left：Release(1)", "Left：ReleaseAllUnused()" });
                createButton.onClick.AddListener(() =>
                {
                    // 类型和名称共同标识池，先检查再取得。
                    if (!QGF.ObjectPool.HasObjectPool<RegistryItem>())
                    {
                        // 两个 int 分别是容量与池优先级；未传名称时使用空名称。
                        var pool = QGF.ObjectPool.CreateSingleSpawnObjectPool<RegistryItem>(8, 5);
                        pool.Register(RegistryItem.Create(Instantiate(cubePrefab, new Vector3(2, 1.5f, 0), Quaternion.identity)), false);
                    }
                    if (!QGF.ObjectPool.HasObjectPool<RegistryItem>("Left"))
                    {
                        // 增加名称参数后，仍按容量 8、池优先级 10 创建。
                        var pool = QGF.ObjectPool.CreateSingleSpawnObjectPool<RegistryItem>("Left", 8, 10);
                        pool.Register(RegistryItem.Create(Instantiate(cubePrefab, new Vector3(3.5f, 0, 0), Quaternion.identity)), false);
                    }
                    if (!QGF.ObjectPool.HasObjectPool<RegistryItem>("Right"))
                    {
                        var pool = QGF.ObjectPool.CreateMultiSpawnObjectPool<RegistryItem>("Right", 8, 0);
                        pool.Register(RegistryItem.Create(Instantiate(cubePrefab, new Vector3(5, -1.5f, 0), Quaternion.identity)), false);
                    }
                });
                queryButton.onClick.AddListener(() =>
                {
                    results.Clear();
                    bool exists = true;
                    // 数组和 List 查询返回符合条件的池引用。
                    switch (queryMode.value)
                    {
                        case 0:
                            exists = QGF.ObjectPool.HasObjectPool<RegistryItem>();
                            if (exists) results.Add((ObjectPoolBase)QGF.ObjectPool.GetObjectPool<RegistryItem>());
                            break;
                        case 1:
                            exists = QGF.ObjectPool.HasObjectPool(typeof(RegistryItem));
                            if (exists) results.Add(QGF.ObjectPool.GetObjectPool(typeof(RegistryItem)));
                            break;
                        case 2:
                            exists = QGF.ObjectPool.HasObjectPool<RegistryItem>("Left");
                            if (exists) results.Add((ObjectPoolBase)QGF.ObjectPool.GetObjectPool<RegistryItem>("Left"));
                            break;
                        case 3:
                            exists = QGF.ObjectPool.HasObjectPool(typeof(RegistryItem), "Left");
                            if (exists) results.Add(QGF.ObjectPool.GetObjectPool(typeof(RegistryItem), "Left"));
                            break;
                        case 4:
                            exists = QGF.ObjectPool.HasObjectPool(pool => pool.Name == "Left");
                            if (exists) results.Add(QGF.ObjectPool.GetObjectPool(pool => pool.Name == "Left"));
                            break;
                        case 5: results.AddRange(QGF.ObjectPool.GetObjectPools(pool => pool.Name.Length > 0)); break;
                        case 6: QGF.ObjectPool.GetObjectPools(pool => pool.Name.Length > 0, results); break;
                        case 7: results.AddRange(QGF.ObjectPool.GetAllObjectPools()); break;
                        case 8: QGF.ObjectPool.GetAllObjectPools(results); break;
                        case 9: results.AddRange(QGF.ObjectPool.GetAllObjectPools(true)); break;
                        case 10: QGF.ObjectPool.GetAllObjectPools(true, results); break;
                    }
                    resultText.text = exists ? $"查到 {results.Count} 个池：" : "查询目标不存在";
                    foreach (var pool in results)
                    {
                        string name = pool.Name.Length == 0 ? "无名称" : pool.Name;
                        resultText.text += $"\n{name}    优先级 {pool.Priority}";
                    }
                    if (results.Count == 1)
                    {
                        // Type 入口返回 ObjectPoolBase，仍可检查和管理同一个池。
                        var pool = results[0];
                        string interval = pool.AutoReleaseInterval == float.MaxValue ? "不自动检查" : $"每 {pool.AutoReleaseInterval:g} 秒检查";
                        string expiry = pool.ExpireTime == float.MaxValue ? "不过期" : $"空闲 {pool.ExpireTime:g} 秒过期";
                        // 界面省略示例命名空间，保留“条目类型.池名”。
                        string label = pool.FullName.Replace(typeof(RegistryItem).Namespace + ".", string.Empty);
                        string spawnMode = pool.AllowMultiSpawn ? "共享借用" : "单次借用";
                        string capacity = $"可释放 {pool.CanReleaseCount}/{pool.Count}，容量 {pool.Capacity}";
                        resultText.text += $"\n标识：{label}\n{spawnMode}，{capacity}\n{interval}；{expiry}";
                    }
                });
                releasePoolButton.onClick.AddListener(() =>
                {
                    // 只回收 Left 的空闲目标，保留这个池；另外两个 Cube 属于其他池。
                    var pool = QGF.ObjectPool.GetObjectPool(typeof(RegistryItem), "Left");
                    switch (poolReleaseMode.value)
                    {
                        case 0: pool.Release(); break; // 未超容量且未过期，Cube 会保留。
                        case 1: pool.Release(1); break;
                        case 2: pool.ReleaseAllUnused(); break;
                    }
                    resultText.text = $"Left 仍在管理器中，剩余 {pool.Count} 个 Cube；其他池不受影响。";
                });
                destroyButton.onClick.AddListener(() =>
                {
                    bool removed = false;
                    switch (destroyMode.value)
                    {
                        // 销毁整个池会释放登记对象，之后不要再使用这份池引用。
                        case 0: removed = QGF.ObjectPool.DestroyObjectPool<RegistryItem>(); break;
                        case 1: removed = QGF.ObjectPool.DestroyObjectPool(typeof(RegistryItem)); break;
                        case 2: removed = QGF.ObjectPool.DestroyObjectPool<RegistryItem>("Left"); break;
                        case 3: removed = QGF.ObjectPool.DestroyObjectPool(typeof(RegistryItem), "Left"); break;
                        case 4: removed = QGF.ObjectPool.DestroyObjectPool(QGF.ObjectPool.GetObjectPool<RegistryItem>("Left")); break;
                        case 5:
                        {
                            var typePool = QGF.ObjectPool.GetObjectPool(typeof(RegistryItem), "Left");
                            removed = QGF.ObjectPool.DestroyObjectPool(typePool);
                            break;
                        }
                    }
                    results.Clear();
                    resultText.text = removed ? "目标池已销毁；其他池继续保留。" : "未找到目标池；其他池继续保留。";
                });
                // 两个操作作用于管理器内的所有池。普通业务更适合使用指定池的 Release。
                releaseButton.onClick.AddListener(() => QGF.ObjectPool.Release());
                clearButton.onClick.AddListener(() => QGF.ObjectPool.ReleaseAllUnused());
                InitializeCreationComparison();
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void InitializeCreationComparison()
        {
            creationKind.ClearOptions();
            creationKind.AddOptions(new List<string> { "单次：泛型", "单次：Type", "共享：泛型", "共享：Type" });
            creationArguments.ClearOptions();
            creationArguments.AddOptions(new List<string> { "默认参数", "仅名称", "容量 8", "过期 4 秒", "名称+容量",
                "名称+过期", "容量+过期", "容量+优先级", "过期+优先级", "名称+容量+过期",
                "名称+容量+优先级", "名称+过期+优先级", "容量+过期+优先级",
                "名称+容量+过期+优先级", "完整参数（含间隔）" });
            removeComparisonButton.interactable = false;
            compareButton.onClick.AddListener(() =>
            {
                // 对照池没有 Target，用 PolicyCubeItem 区分上面三个 RegistryItem 池。
                if (comparisonPool != null) QGF.ObjectPool.DestroyObjectPool(comparisonPool);
                comparisonPool = CreateComparisonPool();
                string capacity = comparisonPool.Capacity == int.MaxValue ? "不限" : comparisonPool.Capacity.ToString();
                string expiry = comparisonPool.ExpireTime == float.MaxValue ? "不过期" : $"{comparisonPool.ExpireTime:g} 秒";
                string interval = comparisonPool.AutoReleaseInterval == float.MaxValue ? "不自动检查" : $"{comparisonPool.AutoReleaseInterval:g} 秒";
                comparisonText.text = $"{comparisonPool.ObjectType.Name} / {(comparisonPool.AllowMultiSpawn ? "共享" : "单次")}\n"
                    + $"池名：{(comparisonPool.Name.Length == 0 ? "无名称" : comparisonPool.Name)}\n容量：{capacity}；优先级：{comparisonPool.Priority}\n"
                    + $"过期：{expiry}\n检查间隔：{interval}\n登记目标：{comparisonPool.Count}";
                removeComparisonButton.interactable = true;
            });
            removeComparisonButton.onClick.AddListener(() =>
            {
                QGF.ObjectPool.DestroyObjectPool(comparisonPool);
                comparisonPool = null;
                comparisonText.text = "已销毁对照池，上面的三个池继续保留。";
                removeComparisonButton.interactable = false;
            });
        }

        private void Update()
        {
            if (!ready) return;
            // 这是池的数量；每个池内对象数量由该池的 Count 表示。
            poolsText.text = $"管理器内有 {QGF.ObjectPool.Count} 个池：";
            foreach (var pool in QGF.ObjectPool.GetAllObjectPools())
            {
                string name = ReferenceEquals(pool, comparisonPool)
                    ? "参数对照"
                    : pool.Name.Length == 0 ? "无名称" : pool.Name;
                poolsText.text += $"\n{name}：{pool.Count} 个对象，优先级 {pool.Priority}";
            }
            destroyButton.interactable = destroyMode.value < 2
                ? QGF.ObjectPool.HasObjectPool<RegistryItem>()
                : QGF.ObjectPool.HasObjectPool<RegistryItem>("Left");
            releasePoolButton.interactable = QGF.ObjectPool.HasObjectPool<RegistryItem>("Left");
        }

        private void OnDestroy()
        {
            var manager = GameEntry.GetComponent<ObjectPoolComponent>();
            if (manager == null) return;
            if (comparisonPool != null) manager.DestroyObjectPool(comparisonPool);
            foreach (var pool in manager.GetObjectPools(pool => pool.ObjectType == typeof(RegistryItem))) manager.DestroyObjectPool(pool);
        }
    }
}
