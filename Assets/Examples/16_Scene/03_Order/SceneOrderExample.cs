#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace UnityQGameFramework.Examples
{
    public sealed class SceneOrderExample : MonoBehaviour
    {
        [SerializeField] private string blueRoomGuid, orangeRoomGuid;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Material markerMaterial;
        [SerializeField] private Button blueButton, orangeButton, spawnButton;
        [SerializeField] private Toggle cameraTagToggle;
        [SerializeField] private Text stateText;

        private string blue, orange, change = "等待加载", spawned = "尚未创建";
        private int blueOrder = 10, orangeOrder = 20;
        private readonly List<string> ownedRooms = new List<string>();
        private string originalCameraTag;
        private bool ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                // 等场景与事件模块初始化，之后才加载两个房间。
                await UniTask.NextFrame(cancellation);
                blue = UnityEditor.AssetDatabase.GUIDToAssetPath(blueRoomGuid);
                orange = UnityEditor.AssetDatabase.GUIDToAssetPath(orangeRoomGuid);
                // 激活变化事件通知当前被选中的场景。
                QGF.Event.Subscribe(ActiveSceneChangedEventArgs.EventId, OnActiveChanged);
                // 附加加载：两个房间同时存在，激活操作不会卸载另一个。
                QGF.Scene.LoadScene(blue);
                ownedRooms.Add(blue);
                QGF.Scene.LoadScene(orange);
                ownedRooms.Add(orange);
                // 加载中也能登记顺序；最大值对应的场景成为 Unity 的激活场景。
                QGF.Scene.SetSceneOrder(blue, blueOrder);
                QGF.Scene.SetSceneOrder(orange, orangeOrder);
                blueButton.onClick.AddListener(() =>
                {
                    blueOrder = 30;
                    orangeOrder = 20;
                    QGF.Scene.SetSceneOrder(blue, blueOrder);
                    QGF.Scene.SetSceneOrder(orange, orangeOrder);
                });
                orangeButton.onClick.AddListener(() =>
                {
                    blueOrder = 10;
                    orangeOrder = 20;
                    QGF.Scene.SetSceneOrder(blue, blueOrder);
                    QGF.Scene.SetSceneOrder(orange, orangeOrder);
                });
                spawnButton.onClick.AddListener(() =>
                {
                    // Unity 将未指定归属的新物体放入激活场景；它不会自动搬走已有物体。
                    var scene = SceneManager.GetActiveScene();
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    marker.name = "Born in " + scene.name;
                    marker.transform.position = new Vector3(4.5f, 1.5f, 0);
                    marker.transform.localScale = Vector3.one * .5f;
                    marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;
                    spawned = marker.scene.name;
                });
                originalCameraTag = sceneCamera.tag;
                cameraTagToggle.onValueChanged.AddListener(value =>
                {
                    sceneCamera.tag = value ? "MainCamera" : "Untagged";
                    // 更换相机标签后刷新 MainCamera 缓存。
                    QGF.Scene.RefreshMainCamera();
                });
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void OnActiveChanged(object sender, GameEventArgs args)
        {
            // 事件参数会回池，只拷贝本次需要显示的场景名称。
            var e = (ActiveSceneChangedEventArgs)args;
            change = e.LastActiveScene.name + " → " + e.ActiveScene.name;
        }

        private void Update()
        {
            if (!ready) return;
            // 两个房间都可用后才允许比较顺序，避免对未加载场景设置顺序。
            bool loaded = QGF.Scene.SceneIsLoaded(blue) && QGF.Scene.SceneIsLoaded(orange);
            blueButton.interactable = orangeButton.interactable = spawnButton.interactable = loaded;
            stateText.text = $"顺序：Blue {blueOrder} / Orange {orangeOrder}\n激活场景：{SceneManager.GetActiveScene().name}"
                + $"\n最近变化：{change}\n新物体归属：{spawned}"
                + $"\n主相机缓存：{(QGF.Scene.MainCamera == null ? "无" : QGF.Scene.MainCamera.name)}";
        }

        private void OnDestroy()
        {
            // 只给仍存在的组件解绑，框架可能已经退出。
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null && events.Check(ActiveSceneChangedEventArgs.EventId, OnActiveChanged))
                events.Unsubscribe(ActiveSceneChangedEventArgs.EventId, OnActiveChanged);
            foreach (string path in ownedRooms) SceneExampleCleanup.Release(path).Forget();
            // 相机属于示例入口场景，控制脚本退出后恢复它原来的标签与缓存。
            if (sceneCamera != null && originalCameraTag != null) sceneCamera.tag = originalCameraTag;
            var scenes = GameEntry.GetComponent<SceneComponent>();
            if (scenes != null) scenes.RefreshMainCamera();
        }
    }
}
#endif
