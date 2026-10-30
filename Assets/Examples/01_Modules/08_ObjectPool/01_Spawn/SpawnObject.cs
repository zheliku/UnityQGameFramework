using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示借出与归还。
    /// </summary>
    public sealed class SpawnObject : MonoBehaviour
    {
        [SerializeField]
        private GameObject cubePrefab;
        [SerializeField]
        private Button spawnButton, returnButton;
        [SerializeField]
        private TMP_Text stateText;
        private IObjectPool<SpawnObjectItem> pool;
        private SpawnObjectItem borrowed;
        private void Start()
        {
            pool = QGF.ObjectPool.CreateSingleSpawnObjectPool<SpawnObjectItem>("Cubes");
            // Target 是真正的 Cube；SpawnObjectItem 是记录它的池内条目。
            var cube = Instantiate(cubePrefab);
            pool.Register(SpawnObjectItem.Create(cube), false); // false：登记为空闲。
            spawnButton.onClick.AddListener(() => borrowed = pool.Spawn());
            returnButton.onClick.AddListener(() =>
            {
                // 归还会执行条目的 OnUnspawn；Cube 留在场景中供下次复用。
                pool.Unspawn(borrowed);
                borrowed = null;
            });
        }

        private void Update()
        {
            stateText.text = borrowed == null ? "Left: idle Cube" : "Right: the same Cube, now spawned";
            spawnButton.interactable = borrowed == null;
            returnButton.interactable = borrowed != null;
        }

        private void OnDestroy()
        {
            // 销毁池会执行条目的 Release，最终销毁 Target。
            var manager = GameEntry.GetComponent<ObjectPoolComponent>();
            if (manager != null && pool != null)
            {
                manager.DestroyObjectPool(pool);
            }
        }
    }
}
