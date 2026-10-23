using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.ObjectPool;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class SpawnObject : MonoBehaviour
    {
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private Dropdown releaseArgument;
        [SerializeField] private Button createButton, spawnButton, returnButton, releaseButton;
        [SerializeField] private Text stateText, resultText;
        private IObjectPool<SpawnObjectItem> pool;
        private SpawnObjectItem item, borrowed;
        private GameObject cube;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // 泛型指定条目类型，Cubes 是池名；创建池不会创建 Cube。
                // 此重载使用默认容量和过期参数，本例通过释放按钮回收目标。
                pool = QGF.ObjectPool.CreateSingleSpawnObjectPool<SpawnObjectItem>("Cubes");
                releaseArgument.ClearOptions();
                releaseArgument.AddOptions(new List<string> { "登记条目 item", "目标 item.Target" });
                createButton.onClick.AddListener(() =>
                {
                    cube = Instantiate(cubePrefab);
                    cube.name = "Pooled Cube";
                    item = SpawnObjectItem.Create(cube);
                    pool.Register(item, false); // false：先登记为空闲，未执行 OnSpawn。
                    resultText.text = $"创建 Cube #{cube.GetInstanceID()}，登记后留在左侧缓存区。";
                });
                spawnButton.onClick.AddListener(() =>
                {
                    // SingleSpawn 不会重复借出同一条目。已借出或池为空时返回 null。
                    var next = pool.Spawn();
                    if (next != null) borrowed = next;
                    resultText.text = next == null ? "Spawn 返回 null：没有可借对象，也没有创建新 Cube。"
                        : $"借出条目，Target 是同一个 Cube #{cube.GetInstanceID()}。";
                });
                returnButton.onClick.AddListener(() =>
                {
                    pool.Unspawn(borrowed.Target); // 也可传 borrowed；归还不会销毁 Cube 或登记条目。
                    borrowed = null;
                    resultText.text = "归还：Cube 回到左侧，下一次借出复用同一实例。";
                });
                releaseButton.onClick.AddListener(() =>
                {
                    // 两个重载查找同一个池内条目；Target 是条目持有的 Cube。
                    bool released = releaseArgument.value == 0
                        ? pool.ReleaseObject(item)
                        : pool.ReleaseObject(item.Target);
                    resultText.text = released ? "最终释放：Release 销毁 Cube，框架回收登记条目。" : "仍被借用，拒绝释放。先归还再试。";
                    if (released) item = null; // 回收后的 ObjectBase 不能继续使用。
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (pool == null) return;
            stateText.text = $"登记 {pool.Count}    可借 {pool.CanSpawn()}    可释放 {pool.CanReleaseCount}\n"
                + (cube == null
                    ? "场景中没有 Cube。"
                    : $"Target：GameObject #{cube.GetInstanceID()}\nObjectBase：SpawnObjectItem；当前{(borrowed == null ? "空闲" : "已借出")}");
            createButton.interactable = item == null;
            spawnButton.interactable = item != null;
            returnButton.interactable = borrowed != null;
            releaseButton.interactable = item != null;
        }

        private void OnDestroy()
        {
            var manager = GameEntry.GetComponent<ObjectPoolComponent>();
            if (manager != null && pool != null) manager.DestroyObjectPool(pool);
        }
    }
}
