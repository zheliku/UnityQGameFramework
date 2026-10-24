using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class GameSpeedExample : MonoBehaviour
    {
        [SerializeField] private Slider speedSlider;
        [SerializeField] private Text speedValueText, gameTimeText, realTimeText;
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
            speedValueText.text = $"速度：{QGF.Base.GameSpeed:F1} 倍";
            gameTimeText.text = $"游戏时间：{gameSeconds:F1} 秒";
            realTimeText.text = $"真实时间：{realSeconds:F1} 秒";
        }

        private void OnDestroy()
        {
            var framework = GameEntry.GetComponent<BaseComponent>();
            if (framework != null) framework.GameSpeed = originalSpeed;
        }
    }
}
