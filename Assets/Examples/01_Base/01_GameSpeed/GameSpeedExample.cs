using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class GameSpeedExample : MonoBehaviour
    {
        [SerializeField] private Text speedValueText;
        [SerializeField] private Slider speedSlider;
        [SerializeField] private Button slowButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button fastButton;
        [SerializeField] private Text gameTimeText;
        [SerializeField] private RectTransform gameMarkerRect;
        [SerializeField] private Text realTimeText;
        [SerializeField] private RectTransform realMarkerRect;
        private bool ready;
        private float originalSpeed, gameSeconds, realSeconds;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                // 组件先完成 Start，随后才能通过 QGF 使用模块。
                await UniTask.NextFrame(cancellation);
                // 保存原倍率，退出示例时恢复。
                originalSpeed = QGF.Base.GameSpeed;
                // GameSpeed 同时设置 Unity 的 timeScale，所以 deltaTime 随倍率变化。
                speedSlider.onValueChanged.AddListener(value => QGF.Base.GameSpeed = value);
                slowButton.onClick.AddListener(() => QGF.Base.GameSpeed = 0.5f);
                // 用框架方法恢复标准倍率，并同步暂停状态。
                normalButton.onClick.AddListener(() => QGF.Base.ResetNormalGameSpeed());
                fastButton.onClick.AddListener(() => QGF.Base.GameSpeed = 2f);
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!ready) return;
            gameSeconds += Time.deltaTime;
            realSeconds += Time.unscaledDeltaTime;
            // 按钮改动倍率后同步滑块；不再次触发滑块的写入回调。
            speedSlider.SetValueWithoutNotify(QGF.Base.GameSpeed);
            speedValueText.text = $"当前游戏速度：{QGF.Base.GameSpeed:F2} 倍";
            gameTimeText.text = $"游戏时长：{gameSeconds:F2} 秒";
            realTimeText.text = $"真实时长：{realSeconds:F2} 秒";
            // 同一条轨道，分别使用游戏时间和真实时间移动。
            gameMarkerRect.anchoredPosition = new Vector2(
                ((RectTransform)gameMarkerRect.parent).rect.width
                * Mathf.PingPong(gameSeconds * 15f, 90f) / 100f,
                0f);
            realMarkerRect.anchoredPosition = new Vector2(
                ((RectTransform)realMarkerRect.parent).rect.width
                * Mathf.PingPong(realSeconds * 15f, 90f) / 100f,
                0f);
        }

        private void OnDestroy()
        {
            if (!ready) return;
            // 通过组件恢复，保证 GameSpeed、暂停标记和 Unity 的 timeScale 一致。
            var framework = GameEntry.GetComponent<BaseComponent>();
            if (framework != null) framework.GameSpeed = originalSpeed;
            else Time.timeScale = originalSpeed;
        }
    }
}
