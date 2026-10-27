using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示查看运行信息。
    /// </summary>
    public sealed class InspectDebuggerExample : MonoBehaviour
    {
        [SerializeField]
        private Toggle activeToggle;
        [SerializeField]
        private Text stateText;
        private Vector2Int screenSize;
        private void Start()
        {
            QGF.Debugger.ActiveWindow = QGF.Debugger.ShowFullWindow = true;
            // 路径对应调试窗口树；也可在调试窗口里手动切换页面。
            QGF.Debugger.SelectDebuggerWindow("Information/Other/Time");
            activeToggle.SetIsOnWithoutNotify(true);
            activeToggle.onValueChanged.AddListener(value => QGF.Debugger.ActiveWindow = value);
            PlaceWindow();
        }

        private void PlaceWindow()
        {
            Rect area = DebuggerExampleLayout.Arrange(stateText);
            float scale = Mathf.Min(1f, area.width / 800f);
            QGF.Debugger.WindowScale = scale;
            QGF.Debugger.WindowRect = new Rect(area.x / scale, area.y / scale, area.width / scale, area.height / scale);
            screenSize = new Vector2Int(Screen.width, Screen.height);
        }

        private void Update()
        {
            if (screenSize != new Vector2Int(Screen.width, Screen.height))
            {
                PlaceWindow();
            }

            stateText.text = "在右侧调试窗口查看时间、场景和环境信息。";
        }
    }
}
