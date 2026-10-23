using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    // 三个示例共用的画面布局；业务操作仍写在各自的控制脚本中。
    internal static class DebuggerExampleLayout
    {
        public static Rect Arrange(Text controls)
        {
            bool portrait = Screen.width < Screen.height;
            var panel = controls.GetComponentInParent<ScrollRect>().GetComponent<RectTransform>();
            panel.anchorMin = portrait ? new Vector2(.025f, .52f) : new Vector2(.025f, .05f);
            panel.anchorMax = portrait ? new Vector2(.975f, .97f) : new Vector2(.475f, .95f);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            return portrait
                ? new Rect(Screen.width * .04f, Screen.height * .53f, Screen.width * .92f, Screen.height * .43f)
                : new Rect(Screen.width * .53f, Screen.height * .06f, Screen.width * .44f, Screen.height * .88f);
        }

        public static void Place(Text controls, ref Vector2Int previousSize)
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size == previousSize) return;
            previousSize = size;
            Rect area = Arrange(controls);
            // 页签有最小宽度；先限制缩放，再把像素区域换成 IMGUI 的缩放前坐标。
            float scale = Mathf.Min(1f, area.width / 800f);
            QGF.Debugger.WindowScale = scale;
            QGF.Debugger.WindowRect = new Rect(area.x / scale, area.y / scale, area.width / scale, area.height / scale);
            QGF.Debugger.IconRect = new Rect(area.x / scale, area.y / scale, 140, 80);
        }
    }
}
