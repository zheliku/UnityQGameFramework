using UnityEngine;

namespace UnityQGameFramework.Examples.Presentation.Resource
{
    // 公共画面布局：宽屏左右排列，窄屏上下排列。
    public sealed class ExampleSceneLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform controls;
        [SerializeField] private RectTransform legend;
        [SerializeField] private RectTransform window;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Bounds worldBounds = new Bounds(new Vector3(3.5f, 0, 0), new Vector3(9, 6, 1));
        private Vector2Int screenSize;
        private Vector2 windowAnchor, appliedAnchor;
        private Vector3 cameraPosition;
        private float cameraSize;
        private bool initialized;

        private void Awake()
        {
            if (sceneCamera != null)
            {
                cameraPosition = sceneCamera.transform.position;
                cameraSize = sceneCamera.orthographicSize;
            }
            if (window != null) windowAnchor = appliedAnchor = window.anchorMin;
            initialized = true;
        }

        private void OnEnable() => screenSize = Vector2Int.zero;

        private void LateUpdate()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            bool windowMoved = window != null && window.anchorMin != appliedAnchor;
            if (size == screenSize && !windowMoved) return;
            screenSize = size;
            bool portrait = size.x < size.y;
            Rect area = portrait ? new Rect(.04f, .04f, .92f, .44f) : new Rect(.53f, .06f, .44f, .88f);
            if (controls != null)
                Place(controls, portrait ? new Rect(.025f, .52f, .95f, .45f) : new Rect(.025f, .05f, .45f, .9f));
            if (legend != null)
            {
                Place(legend, portrait ? new Rect(.04f, .37f, .92f, .11f) : new Rect(.54f, .72f, .43f, .22f));
                area.height = portrait ? .29f : .62f;
            }
            if (sceneCamera != null)
            {
                // 用完整屏幕投影到空出的观察区，天空盒仍覆盖整个 Game 视图。
                float aspect = Mathf.Max(sceneCamera.aspect, .01f);
                float halfHeight = Mathf.Max(cameraSize, worldBounds.extents.y / area.height,
                    worldBounds.extents.x / (area.width * aspect));
                sceneCamera.orthographicSize = halfHeight;
                Vector2 offset = (area.center - Vector2.one * .5f) * 2 * halfHeight;
                sceneCamera.transform.position = new Vector3(worldBounds.center.x - offset.x * aspect,
                    worldBounds.center.y - offset.y, cameraPosition.z);
            }
            if (window != null)
            {
                // OnOpen 可设置窗口的宽屏位置；缩放或复用时保留窗口间的相对偏移。
                if (windowMoved) windowAnchor = window.anchorMin;
                var relative = new Vector2((windowAnchor.x - .53f) / .44f, (windowAnchor.y - .06f) / .88f);
                Vector2 anchor = area.min + Vector2.Scale(relative, area.size);
                var canvas = (RectTransform)window.GetComponentInParent<Canvas>().transform;
                if (canvas.rect.width <= 0 || canvas.rect.height <= 0)
                {
                    screenSize = Vector2Int.zero;
                    return;
                }
                Vector2 margin = Vector2.Scale(window.rect.size, new Vector2(1 / canvas.rect.width, 1 / canvas.rect.height)) * .5f;
                anchor.x = Mathf.Clamp(anchor.x, area.xMin + margin.x, area.xMax - margin.x);
                anchor.y = Mathf.Clamp(anchor.y, area.yMin + margin.y, area.yMax - margin.y);
                window.anchorMin = window.anchorMax = appliedAnchor = anchor;
                window.anchoredPosition = Vector2.zero;
            }
        }

        private static void Place(RectTransform target, Rect area)
        {
            target.anchorMin = area.min;
            target.anchorMax = area.max;
            target.offsetMin = target.offsetMax = Vector2.zero;
        }

        private void OnDisable()
        {
            if (!initialized || sceneCamera == null) return;
            sceneCamera.transform.position = cameraPosition;
            sceneCamera.orthographicSize = cameraSize;
        }
    }
}
