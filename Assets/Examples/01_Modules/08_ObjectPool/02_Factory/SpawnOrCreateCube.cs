using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示池空时由工厂创建。
    /// </summary>
    public sealed class SpawnOrCreateCube : MonoBehaviour
    {
        [SerializeField]
        private GameObject cubePrefab;
        [SerializeField]
        private Button spawnButton, returnButton;
        [SerializeField]
        private TMP_Text stateText;
        private IObjectPool<GrowingCubeItem> pool;
        private readonly List<GrowingCubeItem> borrowed = new List<GrowingCubeItem>();
        private int created, reused;
        private void Start()
        {
            pool = QGF.ObjectPool.CreateSingleSpawnObjectPool<GrowingCubeItem>("Cubes");
            spawnButton.onClick.AddListener(Spawn);
            returnButton.onClick.AddListener(() =>
            {
                var item = borrowed[borrowed.Count - 1];
                borrowed.RemoveAt(borrowed.Count - 1);
                pool.Unspawn(item);
            });
        }

        private void Spawn()
        {
            var item = pool.Spawn(); // 池空时返回 null，不会自动创建 Cube。
            if (item == null)
            {
                var cube = Instantiate(cubePrefab); // 业务侧工厂创建 Target。
                item = GrowingCubeItem.Create(cube);
                pool.Register(item, true); // 已借出，会执行一次 OnSpawn；不用再 Spawn。
                created++;
            }
            else
            {
                reused++;
            }

            ((GameObject)item.Target).transform.position = new Vector3(1.8f + borrowed.Count % 3 * 1.4f, 1.5f - borrowed.Count / 3 * 1.5f, 0);
            borrowed.Add(item);
        }

        private void Update()
        {
            stateText.text = $"Created {created}  Reused {reused}  Spawned {borrowed.Count}";
            spawnButton.interactable = borrowed.Count < 6;
            returnButton.interactable = borrowed.Count > 0;
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
