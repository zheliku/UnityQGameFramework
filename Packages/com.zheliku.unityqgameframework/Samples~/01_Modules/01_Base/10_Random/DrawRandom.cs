using System.Globalization;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityQGameFramework.Core;

    namespace UnityQGameFramework.Examples.Modules
    {
        public sealed class DrawRandom : MonoBehaviour
        {
            [SerializeField] private InputField minInput, maxInput, seedInput;
            [SerializeField] private Button drawButton, seedButton;
            [SerializeField] private Text resultText, seedText;

            private void Start()
            {
                drawButton.onClick.AddListener(Draw);
                seedButton.onClick.AddListener(ApplySeed);
                Draw();
            }

            private void ApplySeed()
            {
                if (!int.TryParse(seedInput.text, out int seed))
                {
                    seedText.text = "种子请输入整数。";
                    return;
                }
                // 相同种子和调用顺序会得到相同的随机序列，便于复现测试。
                Utility.Random.SetSeed(seed);
                seedText.text = $"已应用种子 {seed}";
                Draw();
            }

            private void Draw()
            {
                if (!int.TryParse(minInput.text, out int min) || !int.TryParse(maxInput.text, out int max) || min >= max)
                {
                    resultText.text = "请输入满足 min < max 的整数范围。";
                    return;
                }
                int value = Utility.Random.GetRandom(min, max);
                resultText.text = $"GetRandom({min}, {max}) = {value}（范围含 min，不含 max）";
            }
        }
    }
