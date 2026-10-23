using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class RecentDebuggerLogs : MonoBehaviour
    {
        [SerializeField] private InputField messageInput;
        [SerializeField] private Dropdown queryMode;
        [SerializeField] private Slider countSlider;
        [SerializeField] private Button infoButton, warningButton;
        [SerializeField] private Text stateText, logsText;
        private readonly List<DebuggerComponent.LogNode> logs = new List<DebuggerComponent.LogNode>();
        private readonly StringBuilder display = new StringBuilder();
        private bool ready;
        private Vector2Int screenSize;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // 打开调试器并选中内置 Console 窗口。
                QGF.Debugger.ActiveWindow = QGF.Debugger.ShowFullWindow = true;
                DebuggerExampleLayout.Place(stateText, ref screenSize);
                QGF.Debugger.SelectDebuggerWindow("Console");
                countSlider.wholeNumbers = true;
                // 日志仍通过 QGF 输出，由 Debugger 收集。
                infoButton.onClick.AddListener(() => Log.Info(messageInput.text));
                warningButton.onClick.AddListener(() => Log.Warning(messageInput.text));
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!ready) return;
            DebuggerExampleLayout.Place(stateText, ref screenSize);
            // 两个重载都会先清空 List；count 选取末尾的若干条。
            countSlider.interactable = queryMode.value == 0;
            if (queryMode.value == 0) QGF.Debugger.GetRecentLogs(logs, (int)countSlider.value);
            else QGF.Debugger.GetRecentLogs(logs);
            stateText.text = queryMode.value == 0
                ? $"最近 {(int)countSlider.value} 条    查询结果 {logs.Count} 条"
                : $"全部日志    查询结果 {logs.Count} 条";
            display.Clear();
            foreach (var entry in logs)
                display.AppendLine($"{entry.LogTime:HH:mm:ss} UTC · {entry.LogType}\n{entry.LogMessage}");
            logsText.text = display.Length == 0 ? "暂无日志。QGF > Logging 可设置日志级别。" : display.ToString();
            // LogNode 由调试器持有，这里只留下已经复制的显示文本。
            logs.Clear();
        }
    }
}
