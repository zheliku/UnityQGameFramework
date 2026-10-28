using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示可复现随机数。
    /// </summary>
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
                seedText.text = "Enter an integer seed.";
                return;
            }
            // 相同种子和调用顺序会得到相同的随机序列，便于复现测试。
            Utility.Random.SetSeed(seed);
            seedText.text = $"Seed applied: {seed}";
            Draw();
        }

        private void Draw()
        {
            if (!int.TryParse(minInput.text, out int min) || !int.TryParse(maxInput.text, out int max) || min >= max)
            {
                resultText.text = "Enter an integer range with min < max.";
                return;
            }
            int value = Utility.Random.GetRandom(min, max);
            resultText.text = $"GetRandom({min}, {max}) = {value}(min inclusive, max exclusive)";
        }
    }
}
