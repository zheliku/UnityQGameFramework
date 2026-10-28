using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示自定义调试窗口。
    /// </summary>
    public sealed class DebuggerWindowLifecycle : MonoBehaviour
    {
        [SerializeField]
        private Slider initialValueSlider;
        [SerializeField]
        private Button registerButton, unregisterButton, selectButton, timeButton, incrementButton;
        [SerializeField]
        private Text stateText, resultText;
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
                resultText.text = "Counter window registered and selected.";
            });
            unregisterButton.onClick.AddListener(() =>
            {
                // 注销会调用 Shutdown，移除后的路径查询返回 null。
                bool removed = QGF.Debugger.UnregisterDebuggerWindow(WindowPath);
                resultText.text = removed ? "Counter window unregistered." : "The window is not registered.";
            });
            selectButton.onClick.AddListener(() => QGF.Debugger.SelectDebuggerWindow(WindowPath));
            timeButton.onClick.AddListener(() => QGF.Debugger.SelectDebuggerWindow("Information/Other/Time"));
            incrementButton.onClick.AddListener(() => window.Count++);
            ready = true;
        }

        private void Update()
        {
            if (!ready)
            {
                return;
            }

            DebuggerExampleLayout.Place(stateText, ref screenSize);
            // 查询返回注册时的同一个窗口对象，两个界面操作同一份 Count。
            bool registered = ReferenceEquals(QGF.Debugger.GetDebuggerWindow(WindowPath), window);
            registerButton.interactable = !registered;
            unregisterButton.interactable = selectButton.interactable = incrementButton.interactable = registered;
            stateText.text = $"{(registered ? "Registered" : "Not registered")}    Initial value {(int)initialValueSlider.value}\n" + $"Count {window.Count}    Active for {window.ActiveSeconds:F1} s\n" + $"Initialize {window.InitializeCalls} times    Shutdown {window.ShutdownCalls} times\n" + $"Tab entries {window.EnterCalls} times    Leave {window.LeaveCalls} times";
        }

        private void OnDestroy()
        {
            GameEntry.GetComponent<DebuggerComponent>()?.UnregisterDebuggerWindow(WindowPath);
        }

        /// <summary>
        /// 显示计数并记录调试窗口生命周期。
        /// </summary>
        private sealed class CounterWindow : IDebuggerWindow
        {
            /// <summary>
            /// 获取或设置当前计数。
            /// </summary>
            public int Count
            {
                get; set;
            }

            /// <summary>
            /// 获取窗口初始化次数。
            /// </summary>
            public int InitializeCalls
            {
                get; private set;
            }

            /// <summary>
            /// 获取窗口关闭次数。
            /// </summary>
            public int ShutdownCalls
            {
                get; private set;
            }

            /// <summary>
            /// 获取窗口进入次数。
            /// </summary>
            public int EnterCalls
            {
                get; private set;
            }

            /// <summary>
            /// 获取窗口离开次数。
            /// </summary>
            public int LeaveCalls
            {
                get; private set;
            }

            /// <summary>
            /// 获取窗口累计活动时长，以秒为单位。
            /// </summary>
            public float ActiveSeconds
            {
                get; private set;
            }

            /// <summary>
            /// 从注册参数读取初始值并重置活动计时。
            /// </summary>
            /// <param name="args">注册窗口时传入的参数，首项为初始计数。</param>
            public void Initialize(params object[] args)
            {
                Count = (int)args[0];
                ActiveSeconds = 0;
                InitializeCalls++;
            }

            /// <summary>
            /// 记录窗口被注销。
            /// </summary>
            public void Shutdown()
            {
                ShutdownCalls++;
            }

            // 顶部页签切换触发这两个回调；SelectDebuggerWindow 只改变选中项。
            /// <summary>
            /// 记录窗口被切换为当前页签。
            /// </summary>
            public void OnEnter()
            {
                EnterCalls++;
            }

            /// <summary>
            /// 记录窗口离开当前页签。
            /// </summary>
            public void OnLeave()
            {
                LeaveCalls++;
            }

            // 框架只更新当前选中的页面；切到时间页后计时停止。
            /// <summary>
            /// 累计当前页签处于活动状态的真实时长。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                ActiveSeconds += realElapseSeconds;
            }

            /// <summary>
            /// 绘制计数和累加按钮。
            /// </summary>
            public void OnDraw()
            {
                GUILayout.Label("Count: " + Count);
                GUILayout.Label($"Active for: {ActiveSeconds:F1} s");
                if (GUILayout.Button("+1", GUILayout.Height(36)))
                {
                    Count++;
                }
            }
        }
    }
}
