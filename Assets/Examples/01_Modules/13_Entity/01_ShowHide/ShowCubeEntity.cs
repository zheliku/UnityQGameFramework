using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 通过 QGF.Entity 显示和隐藏 Cube 实体。
    /// </summary>
    public sealed class ShowCubeEntity : MonoBehaviour
    {
        private const string AssetName = "Entity_ShowHide_Cube";
        [SerializeField]
        private Button showButton, hideButton;
        [SerializeField]
        private Text stateText;
        private string failure;
        private void Start()
        {
            QGF.Entity.AddEntityGroup("Example", 30f, 8, 60f, 0);
            QGF.Event.Subscribe(ShowEntityFailureEventArgs.EventId, OnFailed);
            showButton.onClick.AddListener(() =>
            {
                failure = null;
                // 1 是业务实体编号；资源就绪后执行 ShowCubeEntityLogic.OnShow。
                QGF.Entity.ShowEntity<ShowCubeEntityLogic>(1, AssetName, "Example", new Vector3(3, 0, 0));
            });
            hideButton.onClick.AddListener(() => QGF.Entity.HideEntity(1));
        }

        private void Update()
        {
            bool loading = QGF.Entity.IsLoadingEntity(1);
            var entity = QGF.Entity.GetEntity(1);
            stateText.text = failure ?? (loading ? "Loading" : entity == null ? "Not shown" : "Shown: rotating Cube");
            showButton.interactable = !loading && entity == null;
            hideButton.interactable = entity != null;
        }

        private void OnFailed(object sender, GameEventArgs args)
        {
            var e = (ShowEntityFailureEventArgs)args;
            if (e.EntityId == 1)
            {
                failure = e.ErrorMessage;
            }
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(ShowEntityFailureEventArgs.EventId, OnFailed))
            {
                events.Unsubscribe(ShowEntityFailureEventArgs.EventId, OnFailed);
            }

            var entities = GameEntry.GetComponent<EntityComponent>();
            // Hide 结束显示并回池；加载中的同一编号也可以取消。
            if (entities != null && (entities.HasEntity(1) || entities.IsLoadingEntity(1)))
            {
                entities.HideEntity(1);
            }
        }
    }
}
