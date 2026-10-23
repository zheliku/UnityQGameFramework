using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.ObjectPool;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class SpawnOrCreateCube : MonoBehaviour
    {
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private Slider capacitySlider;
        [SerializeField] private Button spawnButton, returnButton, clearButton;
        [SerializeField] private Text stateText, resultText;
        private IObjectPool<GrowingCubeItem> pool;
        private readonly List<GrowingCubeItem> borrowed = new List<GrowingCubeItem>();
        private int created, reused;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // 第二个参数 2 是 int，表示容量；写成 2f 会选中过期秒数重载。
                // Capacity 与登记总数比较；借出的条目保留，超出部分在空闲时回收。
                pool = QGF.ObjectPool.CreateSingleSpawnObjectPool<GrowingCubeItem>("Growing", 2);
                capacitySlider.wholeNumbers = true;
                capacitySlider.onValueChanged.AddListener(value => pool.Capacity = (int)value);
                spawnButton.onClick.AddListener(() =>
                {
                    var item = pool.Spawn(); // 池只查找空闲条目；没有可借条目时返回 null。
                    if (item == null)
                    {
                        // 业务侧工厂：Instantiate 创建 Target，Create 只包装登记条目。
                        var target = Instantiate(cubePrefab);
                        target.name = "Created Cube " + ++created;
                        item = GrowingCubeItem.Create(target);
                        // Register(true) 将借用计数设为 1，并调用一次 OnSpawn。
                        pool.Register(item, true);
                        resultText.text = $"池中无空闲对象，业务工厂新建 Cube #{target.GetInstanceID()}。";
                    }
                    else
                    {
                        reused++;
                        resultText.text = $"复用已有 Cube #{((GameObject)item.Target).GetInstanceID()}。";
                    }
                    var cube = (GameObject)item.Target;
                    cube.transform.position = new Vector3(1.8f + borrowed.Count % 3 * 1.4f, 1.5f - borrowed.Count / 3 * 1.5f, 0);
                    borrowed.Add(item);
                });
                returnButton.onClick.AddListener(() =>
                {
                    // 先移出业务列表。超过 Capacity 时，Unspawn 可能立即淘汰这份条目。
                    var item = borrowed[borrowed.Count - 1];
                    borrowed.RemoveAt(borrowed.Count - 1);
                    pool.Unspawn(item);
                    resultText.text = "已归还：保留下来的 Cube 进入下方缓存区；超过容量的空闲 Cube 被销毁。";
                });
                clearButton.onClick.AddListener(() => pool.ReleaseAllUnused());
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (pool == null) return;
            stateText.text = $"累计创建 {created}    复用 {reused}\n借出 {borrowed.Count}    缓存 {pool.Count - borrowed.Count}    登记 {pool.Count}\n"
                + $"Capacity {pool.Capacity}（空闲回收阈值）";
            spawnButton.interactable = borrowed.Count < 6; // 面板最多展示六个 Cube。
            returnButton.interactable = borrowed.Count > 0;
            clearButton.interactable = pool.CanReleaseCount > 0;
        }

        private void OnDestroy()
        {
            var manager = GameEntry.GetComponent<ObjectPoolComponent>();
            if (manager != null && pool != null) manager.DestroyObjectPool(pool);
        }
    }
}
