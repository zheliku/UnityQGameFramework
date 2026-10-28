using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示时间倍率。
    /// </summary>
    public sealed class GameSpeedExample : MonoBehaviour
    {
        [SerializeField]
        private Slider speedSlider;
        [SerializeField]
        private Text speedValueText, gameTimeText, realTimeText;
        private float originalSpeed, gameSeconds, realSeconds;
        private void Start()
        {
            originalSpeed = QGF.Base.GameSpeed;
            // GameSpeed 会同步 Unity 的 timeScale，游戏计时随倍率变化。
            speedSlider.SetValueWithoutNotify(originalSpeed);
            speedSlider.onValueChanged.AddListener(value => QGF.Base.GameSpeed = value);
        }

        private void Update()
        {
            gameSeconds += Time.deltaTime;
            realSeconds += Time.unscaledDeltaTime;
            speedValueText.text = $"Speed: {QGF.Base.GameSpeed:F1}x";
            gameTimeText.text = $"Game time: {gameSeconds:F1} s";
            realTimeText.text = $"Real time: {realSeconds:F1} s";
        }

        private void OnDestroy()
        {
            var framework = GameEntry.GetComponent<BaseComponent>();
            if (framework != null)
            {
                framework.GameSpeed = originalSpeed;
            }
        }
    }
}
