using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.ObjectPool;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class MultiSpawn : MonoBehaviour
    {
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private Button borrowButton, returnButton, releaseButton, createButton;
        [SerializeField] private Text stateText, resultText;
        private IObjectPool<SharedCubeItem> pool;
        private readonly List<SharedCubeItem> users = new List<SharedCubeItem>();
        private GameObject cube;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // MultiSpawn 允许多个使用者借用同一条目。
                // 此处省略过期参数，空闲条目由下面的释放按钮回收。
                pool = QGF.ObjectPool.CreateMultiSpawnObjectPool<SharedCubeItem>("Shared");
                createButton.onClick.AddListener(() =>
                {
                    cube = Instantiate(cubePrefab);
                    cube.transform.position = new Vector3(3.3f, 0, 0);
                    pool.Register(SharedCubeItem.Create(cube), false);
                });
                borrowButton.onClick.AddListener(() =>
                {
                    users.Add(pool.Spawn("Cube"));
                    resultText.text = $"使用者 {users.Count} 得到同一个 Target #{cube.GetInstanceID()}。";
                });
                returnButton.onClick.AddListener(() =>
                {
                    var item = users[users.Count - 1];
                    users.RemoveAt(users.Count - 1);
                    pool.Unspawn(item);
                    resultText.text = users.Count > 0 ? "只归还一份，其他使用者仍持有 Cube。" : "最后一份已归还，Cube 恢复原状，可以最终释放。";
                });
                releaseButton.onClick.AddListener(() =>
                {
                    pool.ReleaseAllUnused();
                    resultText.text = pool.Count == 0 ? "共享 Cube 已销毁。" : "仍有人使用，不能释放。";
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (pool == null) return;
            var infos = ((ObjectPoolBase)pool).GetAllObjectInfos();
            // IsInUse 来自借用计数；共享池在使用中仍允许再次借出同一个条目。
            stateText.text = $"场景 Cube 数：{(cube == null ? 0 : 1)}    登记数：{pool.Count}\n"
                + $"允许共享：{(pool.AllowMultiSpawn ? "是" : "否")}    当前状态：{(infos.Length > 0 && infos[0].IsInUse ? "使用中" : "空闲")}\n"
                + $"借用次数：{(infos.Length == 0 ? 0 : infos[0].SpawnCount)}\n";
            for (int i = 0; i < users.Count; i++) stateText.text += $"使用者 {i + 1} → 同一个 GameObject #{((GameObject)users[i].Target).GetInstanceID()}\n";
            createButton.interactable = pool.Count == 0;
            // 按条目名称检查可借状态；最多展示三个使用者是本示例的界面限制。
            borrowButton.interactable = pool.CanSpawn("Cube") && users.Count < 3;
            returnButton.interactable = users.Count > 0;
            releaseButton.interactable = pool.Count > 0;
        }

        private void OnDestroy()
        {
            var manager = GameEntry.GetComponent<ObjectPoolComponent>();
            if (manager != null && pool != null) manager.DestroyObjectPool(pool);
        }
    }
}
