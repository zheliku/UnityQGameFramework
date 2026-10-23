using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples
{
    public sealed class DrawRandom : MonoBehaviour
    {
        [SerializeField] private Dropdown mode;
        [SerializeField] private InputField minInput, maxInput, seedInput;
        [SerializeField] private Button drawButton, seedButton;
        [SerializeField] private Text resultText, seedText;
        [SerializeField] private RectTransform markerRect;
        private float normalized;

        private void Start()
        {
            mode.ClearOptions();
            mode.AddOptions(new List<string> { "非负整数", "整数 [0, max)", "整数 [min, max)", "小数 [0, 1)", "8 个随机字节" });
            mode.value = 2;
            drawButton.onClick.AddListener(Draw);
            seedButton.onClick.AddListener(() =>
            {
                if (!int.TryParse(seedInput.text, out int seed))
                {
                    seedText.text = "种子请输入整数。";
                    return;
                }
                // Random 是框架共用的序列。相同种子与调用顺序可以重现结果。
                Utility.Random.SetSeed(seed);
                seedText.text = $"已应用全局种子 {seed}。相同种子与调用顺序可重现结果。";
                Draw();
            });
            mode.onValueChanged.AddListener(_ => UpdateInputs());
            UpdateInputs();
            seedText.text = "尚未重设种子。应用种子会改变框架共用的随机序列。";
            Draw();
        }

        private void UpdateInputs()
        {
            minInput.interactable = mode.value == 2;
            maxInput.interactable = mode.value == 1 || mode.value == 2;
        }

        private void Draw()
        {
            int min = 0, max = int.MaxValue;
            if (maxInput.interactable && (!int.TryParse(maxInput.text, out max) || (mode.value == 1 && max < 0)))
            {
                resultText.text = "max 请输入整数；[0, max) 的 max 不能小于 0。";
                return;
            }
            if (minInput.interactable && (!int.TryParse(minInput.text, out min) || min > max))
            {
                resultText.text = "min 请输入不大于 max 的整数。";
                return;
            }

            double position;
            if (mode.value == 3)
            {
                double value = Utility.Random.GetRandomDouble();
                resultText.text = "GetRandomDouble()：" + value.ToString("R", CultureInfo.InvariantCulture);
                position = value;
            }
            else if (mode.value == 4)
            {
                var bytes = new byte[8];
                // 写入调用方提供的数组，不返回新的数组。
                Utility.Random.GetRandomBytes(bytes);
                resultText.text = $"GetRandomBytes(buffer)：{BitConverter.ToString(bytes)}\n标记表示首字节：{bytes[0]} / 255";
                position = bytes[0] / 255d;
            }
            else
            {
                // 整数范围含下界、不含上界；两个边界相同时返回该值。
                int value = mode.value == 0 ? Utility.Random.GetRandom()
                    : mode.value == 1 ? Utility.Random.GetRandom(max) : Utility.Random.GetRandom(min, max);
                resultText.text = $"GetRandom：{value}\n范围 [{min}, {max})" + (min == max ? "，边界相同，返回边界值" : "");
                position = min == max ? 0 : (value - (double)min) / (max - (double)min);
            }
            normalized = (float)position;
        }

        private void LateUpdate()
        {
            var track = (RectTransform)markerRect.parent;
            // 标记落在轨道可见范围内；其位置对应本次结果在范围中的比例。
            float travel = Mathf.Max(0, track.rect.width - markerRect.rect.width);
            markerRect.anchoredPosition = new Vector2(normalized * travel, 0);
        }
    }
}
