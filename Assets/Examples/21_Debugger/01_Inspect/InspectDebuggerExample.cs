using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class InspectDebuggerExample : MonoBehaviour
    {
        [SerializeField] private Toggle activeToggle, fullToggle;
        [SerializeField] private Dropdown pageDropdown;
        [SerializeField] private Slider scaleSlider;
        [SerializeField] private Button pauseButton, resetButton, positionButton;
        [SerializeField] private Text stateText, resultText;
        private readonly string[] pages = { "Information/Other/Time", "Information/Other/Scene", "Information/Environment", "Missing/Page" };
        private bool ready;
        private float originalSpeed;
        private Vector2Int screenSize;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                originalSpeed = QGF.Base.GameSpeed;
                QGF.Debugger.ActiveWindow = QGF.Debugger.ShowFullWindow = true;
                // ActiveWindow 控制入口开关，ShowFullWindow 控制展开或图标模式。
                activeToggle.onValueChanged.AddListener(value => QGF.Debugger.ActiveWindow = value);
                fullToggle.onValueChanged.AddListener(value => QGF.Debugger.ShowFullWindow = value);
                pageDropdown.onValueChanged.AddListener(index =>
                {
                    // 路径对应调试树中的已注册窗口，未找到时返回 false。
                    bool selected = QGF.Debugger.SelectDebuggerWindow(pages[index]);
                    resultText.text = selected ? "已选中 " + pages[index] : "未找到 " + pages[index];
                });
                scaleSlider.onValueChanged.AddListener(_ => PlaceWindow());
                resetButton.onClick.AddListener(() =>
                {
                    QGF.Debugger.ResetLayout();
                    scaleSlider.SetValueWithoutNotify(QGF.Debugger.WindowScale);
                });
                positionButton.onClick.AddListener(PlaceWindow);
                pauseButton.onClick.AddListener(() =>
                {
                    if (QGF.Base.IsGamePaused) QGF.Base.ResumeGame();
                    else QGF.Base.PauseGame();
                });
                QGF.Debugger.SelectDebuggerWindow(pages[0]);
                PlaceWindow();
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void PlaceWindow()
        {
            Rect area = DebuggerExampleLayout.Arrange(stateText);
            float scale = Mathf.Min(scaleSlider.value, area.width / 800f);
            QGF.Debugger.WindowScale = scale;
            // WindowRect、IconRect 使用缩放前坐标；竖屏时调试窗口排在操作区下方。
            QGF.Debugger.WindowRect = new Rect(area.x / scale, area.y / scale, area.width / scale, area.height / scale);
            QGF.Debugger.IconRect = new Rect(area.x / scale, area.y / scale, 140, 80);
            screenSize = new Vector2Int(Screen.width, Screen.height);
        }

        private void Update()
        {
            if (!ready) return;
            if (screenSize != new Vector2Int(Screen.width, Screen.height)) PlaceWindow();
            var debugger = QGF.Debugger;
            var bounds = debugger.WindowRect;
            activeToggle.SetIsOnWithoutNotify(debugger.ActiveWindow);
            fullToggle.SetIsOnWithoutNotify(debugger.ShowFullWindow);
            stateText.text = $"缩放 {debugger.WindowScale:F2}    游戏速度 {QGF.Base.GameSpeed:F1}\n"
                + $"窗口 ({bounds.x:F0}, {bounds.y:F0})  {bounds.width:F0} × {bounds.height:F0}";
            pauseButton.GetComponentInChildren<Text>().text = QGF.Base.IsGamePaused ? "恢复游戏" : "暂停游戏";
        }

        private void OnDestroy()
        {
            if (!ready) return;
            // 同步恢复组件记录和 Unity 时间，避免控制脚本退出后仍留下暂停状态。
            var framework = GameEntry.GetComponent<BaseComponent>();
            if (framework != null) framework.GameSpeed = originalSpeed;
            else Time.timeScale = originalSpeed;
        }
    }
}
