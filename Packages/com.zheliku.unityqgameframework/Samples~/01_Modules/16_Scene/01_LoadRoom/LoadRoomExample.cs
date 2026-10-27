using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 通过 QGF.Scene 加载并卸载房间场景。
    /// </summary>
    public sealed class LoadRoomExample : MonoBehaviour
    {
        [SerializeField]
        private Button loadButton, unloadButton;
        [SerializeField]
        private Text stateText;
        private const string RoomAddress = "Scene_LoadRoom_BlueRoom";
        private bool ownsRoom;
        private void Start()
        {
            QGF.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, OnResult);
            QGF.Event.Subscribe(LoadSceneFailureEventArgs.EventId, OnResult);
            QGF.Event.Subscribe(UnloadSceneSuccessEventArgs.EventId, OnResult);
            QGF.Event.Subscribe(UnloadSceneFailureEventArgs.EventId, OnResult);
            loadButton.onClick.AddListener(() =>
            {
                // 以叠加方式加载，框架和操作面板保留在当前场景。
                QGF.Scene.LoadScene(RoomAddress);
                ownsRoom = true;
                stateText.text = "正在加载";
            });
            unloadButton.onClick.AddListener(() => QGF.Scene.UnloadScene(RoomAddress));
        }

        private void Update()
        {
            bool busy = QGF.Scene.SceneIsLoading(RoomAddress) || QGF.Scene.SceneIsUnloading(RoomAddress);
            bool loaded = QGF.Scene.SceneIsLoaded(RoomAddress);
            loadButton.interactable = !busy && !loaded;
            unloadButton.interactable = !busy && loaded;
        }

        private void OnResult(object sender, GameEventArgs args)
        {
            if (args is LoadSceneSuccessEventArgs loaded && loaded.SceneAssetName == RoomAddress)
            {
                stateText.text = "房间已加载";
            }
            else if (args is LoadSceneFailureEventArgs failed && failed.SceneAssetName == RoomAddress)
            {
                stateText.text = failed.ErrorMessage;
                ownsRoom = false;
            }
            else if (args is UnloadSceneSuccessEventArgs unloaded && unloaded.SceneAssetName == RoomAddress)
            {
                stateText.text = "房间已卸载";
                ownsRoom = false;
            }
            else if (args is UnloadSceneFailureEventArgs failure && failure.SceneAssetName == RoomAddress)
            {
                stateText.text = "卸载失败，可以重试。";
            }
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(LoadSceneSuccessEventArgs.EventId, OnResult))
                {
                    events.Unsubscribe(LoadSceneSuccessEventArgs.EventId, OnResult);
                }

                if (events.Check(LoadSceneFailureEventArgs.EventId, OnResult))
                {
                    events.Unsubscribe(LoadSceneFailureEventArgs.EventId, OnResult);
                }

                if (events.Check(UnloadSceneSuccessEventArgs.EventId, OnResult))
                {
                    events.Unsubscribe(UnloadSceneSuccessEventArgs.EventId, OnResult);
                }

                if (events.Check(UnloadSceneFailureEventArgs.EventId, OnResult))
                {
                    events.Unsubscribe(UnloadSceneFailureEventArgs.EventId, OnResult);
                }
            }

            // 加载不可中止；示例退出时等待已发出的加载结束，再卸载它。
            if (ownsRoom)
            {
                SceneExampleCleanup.Release(RoomAddress).Forget();
            }
        }
    }
}
