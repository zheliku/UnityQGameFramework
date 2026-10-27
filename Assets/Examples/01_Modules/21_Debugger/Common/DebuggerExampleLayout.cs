using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    // 三个示例共用的画面布局；业务操作仍写在各自的控制脚本中。
    /// <summary>
    /// 布置调试器示例的演示区域。
    /// </summary>
    internal static class DebuggerExampleLayout
    {
        /// <summary>
        /// 根据屏幕方向布置控制面板并计算调试器区域。
        /// </summary>
        /// <param name="controls">控制面板中的文本组件。</param>
        /// <returns>调试器使用的屏幕像素区域。</returns>
        public static Rect Arrange(Text controls)
        {
            bool portrait = Screen.width < Screen.height;
            var panel = controls.GetComponentInParent<ScrollRect>().GetComponent<RectTransform>();
            panel.anchorMin = portrait ? new Vector2(.025f, .52f) : new Vector2(.025f, .05f);
            panel.anchorMax = portrait ? new Vector2(.975f, .97f) : new Vector2(.475f, .95f);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            return portrait ? new Rect(Screen.width * .04f, Screen.height * .53f, Screen.width * .92f, Screen.height * .43f) : new Rect(Screen.width * .53f, Screen.height * .06f, Screen.width * .44f, Screen.height * .88f);
        }

        /// <summary>
        /// 屏幕尺寸变化时重新布局调试器。
        /// </summary>
        /// <param name="controls">控制面板中的文本组件。</param>
        /// <param name="previousSize">上次布局使用的屏幕尺寸，布局后更新。</param>
        public static void Place(Text controls, ref Vector2Int previousSize)
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size == previousSize)
            {
                return;
            }

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
