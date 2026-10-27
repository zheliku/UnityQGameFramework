using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.ObjectPool;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示锁定对象，控制回收。
    /// </summary>
    public sealed class ObjectPoolPolicy : MonoBehaviour
    {
        [SerializeField]
        private GameObject cubePrefab;
        [SerializeField]
        private Toggle protectKeep;
        [SerializeField]
        private Button createButton, releaseButton;
        [SerializeField]
        private Text stateText, resultText;
        private IObjectPool<PolicyCubeItem> pool;
        private PolicyCubeItem keep;
        private void Start()
        {
            // 空闲条目可以回收，锁定的 Keep 除外。
            pool = QGF.ObjectPool.CreateSingleSpawnObjectPool<PolicyCubeItem>("Policy", .25f, 8, float.MaxValue, 0);
            protectKeep.onValueChanged.AddListener(ProtectKeep);
            createButton.onClick.AddListener(CreateCubes);
            releaseButton.onClick.AddListener(() =>
            {
                // 只回收空闲且允许释放的条目。
                pool.ReleaseAllUnused();
                if (!pool.CanSpawn("Keep"))
                {
                    keep = null;
                }

                resultText.text = "已执行 ReleaseAllUnused。";
            });
        }

        private void CreateCubes()
        {
            Register("Temp A", new Vector3(2, 0, 0));
            Register("Temp B", new Vector3(3.5f, 0, 0));
            keep = Register("Keep", new Vector3(5, 0, 0));
            ProtectKeep(protectKeep.isOn);
            resultText.text = "三个 Cube 已登记；执行回收观察 Keep 是否保留。";
        }

        private PolicyCubeItem Register(string name, Vector3 position)
        {
            var cube = Instantiate(cubePrefab, position, Quaternion.identity);
            cube.name = name;
            var item = PolicyCubeItem.Create(name, cube, name == "Keep" ? 10 : 0);
            pool.Register(item, false); // 登记为空闲，不计入借用次数。
            return item;
        }

        private void ProtectKeep(bool value)
        {
            if (keep == null)
            {
                return;
            }

            // SetLocked 针对池内条目；条目 Target 才是场景中的 Cube。
            pool.SetLocked(keep, value);
        }

        private void Update()
        {
            if (pool == null)
            {
                return;
            }

            var infos = ((ObjectPoolBase)pool).GetAllObjectInfos();
            bool keepExists = System.Array.Exists(infos, info => info.Name == "Keep");
            stateText.text = $"登记 {pool.Count} · 可释放 {pool.CanReleaseCount}\n" + (pool.Count == 0 ? "尚未创建 Cube" : $"Keep：{(keepExists ? protectKeep.isOn ? "已保护" : "可回收" : "已回收")}");
            createButton.interactable = pool.Count == 0;
            protectKeep.interactable = keep != null;
            releaseButton.interactable = pool.CanReleaseCount > 0;
        }

        private void OnDestroy()
        {
            var manager = GameEntry.GetComponent<ObjectPoolComponent>();
            if (manager != null && pool != null)
            {
                manager.DestroyObjectPool(pool);
            }
        }
    }
}
