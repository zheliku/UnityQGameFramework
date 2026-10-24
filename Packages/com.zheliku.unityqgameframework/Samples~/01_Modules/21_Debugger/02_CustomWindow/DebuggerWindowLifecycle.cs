using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Debugger;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class DebuggerWindowLifecycle : MonoBehaviour
    {
        [SerializeField] private Slider initialValueSlider;
        [SerializeField] private Button registerButton, unregisterButton, selectButton, timeButton, incrementButton;
        [SerializeField] private Text stateText, resultText;
        private const string WindowPath = "Examples/Counter";
        private readonly CounterWindow window = new CounterWindow();
        private bool ready;
        private Vector2Int screenSize;

        private void Start()
        {
            QGF.Debugger.ActiveWindow = QGF.Debugger.ShowFullWindow = true;
            DebuggerExampleLayout.Place(stateText, ref screenSize);
            initialValueSlider.wholeNumbers = true;
            registerButton.onClick.AddListener(() =>
            {
                // 最后的参数传给 Initialize；路径中的 / 创建窗口组。
                QGF.Debugger.RegisterDebuggerWindow(WindowPath, window, (int)initialValueSlider.value);
                QGF.Debugger.SelectDebuggerWindow(WindowPath);
                resultText.text = "已注册并选中计数窗口。";
            });
            unregisterButton.onClick.AddListener(() =>
            {
                // 注销会调用 Shutdown，移除后的路径查询返回 null。
                bool removed = QGF.Debugger.UnregisterDebuggerWindow(WindowPath);
                resultText.text = removed ? "已注销计数窗口。" : "窗口尚未注册。";
            });
            selectButton.onClick.AddListener(() => QGF.Debugger.SelectDebuggerWindow(WindowPath));
            timeButton.onClick.AddListener(() => QGF.Debugger.SelectDebuggerWindow("Information/Other/Time"));
            incrementButton.onClick.AddListener(() => window.Count++);
            ready = true;
        }

        private void Update()
        {
            if (!ready) return;
            DebuggerExampleLayout.Place(stateText, ref screenSize);
            // 查询返回注册时的同一个窗口对象，两个界面操作同一份 Count。
            bool registered = ReferenceEquals(QGF.Debugger.GetDebuggerWindow(WindowPath), window);
            registerButton.interactable = !registered;
            unregisterButton.interactable = selectButton.interactable = incrementButton.interactable = registered;
            stateText.text = $"{(registered ? "已注册" : "未注册")}    初始值 {(int)initialValueSlider.value}\n"
                + $"计数 {window.Count}    页面更新 {window.ActiveSeconds:F1} 秒\n"
                + $"Initialize {window.InitializeCalls} 次    Shutdown {window.ShutdownCalls} 次\n"
                + $"页签进入 {window.EnterCalls} 次    离开 {window.LeaveCalls} 次";
        }

        private void OnDestroy() => GameEntry.GetComponent<DebuggerComponent>()?.UnregisterDebuggerWindow(WindowPath);

        private sealed class CounterWindow : IDebuggerWindow
        {
            public int Count { get; set; }
            public int InitializeCalls { get; private set; }
            public int ShutdownCalls { get; private set; }
            public int EnterCalls { get; private set; }
            public int LeaveCalls { get; private set; }
            public float ActiveSeconds { get; private set; }

            public void Initialize(params object[] args)
            {
                Count = (int)args[0];
                ActiveSeconds = 0;
                InitializeCalls++;
            }

            public void Shutdown() => ShutdownCalls++;
            // 顶部页签切换触发这两个回调；SelectDebuggerWindow 只改变选中项。
            public void OnEnter() => EnterCalls++;
            public void OnLeave() => LeaveCalls++;
            // 框架只更新当前选中的页面；切到时间页后计时停止。
            public void OnUpdate(float elapseSeconds, float realElapseSeconds) => ActiveSeconds += realElapseSeconds;

            public void OnDraw()
            {
                GUILayout.Label("计数：" + Count);
                GUILayout.Label($"页面更新：{ActiveSeconds:F1} 秒");
                if (GUILayout.Button("+1", GUILayout.Height(36))) Count++;
            }
        }
    }
}
