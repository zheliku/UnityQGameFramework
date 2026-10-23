using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class PauseGameExample : MonoBehaviour
    {
        [SerializeField] private Text stateText;
        [SerializeField] private Text gameTimeText;
        [SerializeField] private Text realTimeText;
        [SerializeField] private Text speedValueText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private RectTransform markerRect;
        private Text pauseButtonText;
        private bool ready;
        private float originalSpeed, gameSeconds, realSeconds;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // 保存原倍率，退出示例时恢复。
                originalSpeed = QGF.Base.GameSpeed;
                pauseButtonText = pauseButton.GetComponentInChildren<Text>();
                // PauseGame 记录当前倍率并暂停；ResumeGame 会恢复暂停前的倍率。
                pauseButton.onClick.AddListener(() =>
                {
                    if (QGF.Base.IsGamePaused) QGF.Base.ResumeGame();
                    else QGF.Base.PauseGame();
                });
                // 暂停时关闭速度按钮，避免暂停状态下改写恢复倍率。
                speedButton.onClick.AddListener(() =>
                    QGF.Base.GameSpeed = QGF.Base.IsNormalGameSpeed ? 0.5f : 1f);
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!ready) return;
            // 游戏时间受暂停影响；真实时间继续走，用来对照两种时钟。
            gameSeconds += Time.deltaTime;
            realSeconds += Time.unscaledDeltaTime;
            bool isPaused = QGF.Base.IsGamePaused;
            stateText.text = isPaused ? "已暂停" : "运行中";
            gameTimeText.text = $"游戏总时长：{gameSeconds:F2} 秒";
            realTimeText.text = $"真实总时长：{realSeconds:F2} 秒";
            speedValueText.text = $"当前速度：{QGF.Base.GameSpeed:F1} 倍";
            pauseButtonText.text = isPaused ? "继续" : "暂停";
            speedButton.interactable = !isPaused;
            markerRect.anchoredPosition = new Vector2(
                ((RectTransform)markerRect.parent).rect.width
                * Mathf.PingPong(gameSeconds * 15f, 90f) / 100f,
                0f);
        }

        private void OnDestroy()
        {
            if (!ready) return;
            // 暂停时退出也要恢复组件记录；只改 timeScale 会留下 IsGamePaused = true。
            var framework = GameEntry.GetComponent<BaseComponent>();
            if (framework != null) framework.GameSpeed = originalSpeed;
            else Time.timeScale = originalSpeed;
        }
    }
}
