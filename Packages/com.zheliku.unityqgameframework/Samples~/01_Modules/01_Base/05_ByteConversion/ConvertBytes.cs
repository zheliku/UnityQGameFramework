using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示字节转换与偏移。
    /// </summary>
    public sealed class ConvertBytes : MonoBehaviour
    {
        [SerializeField] private InputField valueInput;
        [SerializeField] private Button convertButton;
        [SerializeField] private Text resultText;

        private void Start()
        {
            convertButton.onClick.AddListener(ConvertValue);
            ConvertValue();
        }

        private void ConvertValue()
        {
            if (!int.TryParse(valueInput.text, out int value))
            {
                resultText.text = "请输入整数。";
                return;
            }

            // 把 Int32 写入带有两字节包头的缓冲区，再从同一偏移读回。
            var buffer = new byte[6];
            buffer[0] = 0x51;
            buffer[1] = 0x47;
            Utility.Converter.GetBytes(value, buffer, 2);
            int restored = Utility.Converter.GetInt32(buffer, 2);
            resultText.text = string.Join(Environment.NewLine,
                $"原值：{value}", $"缓冲区：{BitConverter.ToString(buffer)}", $"还原：{restored}");
        }
    }
}
