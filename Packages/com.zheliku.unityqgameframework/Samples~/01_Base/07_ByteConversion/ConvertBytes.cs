using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples
{
    public sealed class ConvertBytes : MonoBehaviour
    {
        [SerializeField] private InputField valueInput;
        [SerializeField] private Dropdown valueType;
        [SerializeField] private Dropdown writeMode;
        [SerializeField] private Button convertButton;
        [SerializeField] private Text resultText;

        private void Start()
        {
            valueType.ClearOptions();
            valueType.AddOptions(new List<string>
            {
                "Int32", "Single", "Boolean", "Char（一个 UTF-16 代码单元）", "Int16", "UInt16",
                "Int64", "UInt64", "UInt32", "Double", "字符串 · UTF-8", "字符串 · UTF-16 LE"
            });
            writeMode.ClearOptions();
            writeMode.AddOptions(new List<string> { "返回新数组", "写入已有数组", "从偏移 2 写入" });
            string[] defaults =
            {
                "42", "3.5", "true", "中", "-123", "65535", "-1234567890123", "12345678901234",
                "4000000000", "3.141592653589793", "飞船 QGF", "飞船 QGF"
            };
            valueType.onValueChanged.AddListener(index => valueInput.text = defaults[index]);
            convertButton.onClick.AddListener(ConvertValue);
            valueInput.text = defaults[0];
            ConvertValue();
        }

        private void ConvertValue()
        {
            int offset = writeMode.value == 2 ? 2 : 0;
            byte[] buffer;
            object restored;
            try
            {
                // GetBytes(value) 分配新数组；另两个重载写入调用方提供的缓冲区。
                // 有包头时，写入与读取必须使用同一个偏移。数字按本机字节序保存。
                switch (valueType.value)
                {
                    case 0:
                    {
                        var value = int.Parse(valueInput.text, CultureInfo.InvariantCulture);
                        buffer = writeMode.value == 0 ? Utility.Converter.GetBytes(value) : new byte[4 + offset];
                        if (writeMode.value == 1) Utility.Converter.GetBytes(value, buffer);
                        if (writeMode.value == 2) Utility.Converter.GetBytes(value, buffer, offset);
                        restored = offset == 0 ? Utility.Converter.GetInt32(buffer) : Utility.Converter.GetInt32(buffer, offset);
                        break;
                    }
                    case 1:
                    {
                        var value = float.Parse(valueInput.text, CultureInfo.InvariantCulture);
                        buffer = writeMode.value == 0 ? Utility.Converter.GetBytes(value) : new byte[4 + offset];
                        if (writeMode.value == 1) Utility.Converter.GetBytes(value, buffer);
                        if (writeMode.value == 2) Utility.Converter.GetBytes(value, buffer, offset);
                        restored = offset == 0 ? Utility.Converter.GetSingle(buffer) : Utility.Converter.GetSingle(buffer, offset);
                        break;
                    }
                    case 2:
                    {
                        var value = bool.Parse(valueInput.text);
                        buffer = writeMode.value == 0 ? Utility.Converter.GetBytes(value) : new byte[1 + offset];
                        if (writeMode.value == 1) Utility.Converter.GetBytes(value, buffer);
                        if (writeMode.value == 2) Utility.Converter.GetBytes(value, buffer, offset);
                        restored = offset == 0 ? Utility.Converter.GetBoolean(buffer) : Utility.Converter.GetBoolean(buffer, offset);
                        break;
                    }
                    case 3:
                    {
                        var value = char.Parse(valueInput.text);
                        buffer = writeMode.value == 0 ? Utility.Converter.GetBytes(value) : new byte[2 + offset];
                        if (writeMode.value == 1) Utility.Converter.GetBytes(value, buffer);
                        if (writeMode.value == 2) Utility.Converter.GetBytes(value, buffer, offset);
                        restored = offset == 0 ? Utility.Converter.GetChar(buffer) : Utility.Converter.GetChar(buffer, offset);
                        break;
                    }
                    case 4:
                    {
                        var value = short.Parse(valueInput.text, CultureInfo.InvariantCulture);
                        buffer = writeMode.value == 0 ? Utility.Converter.GetBytes(value) : new byte[2 + offset];
                        if (writeMode.value == 1) Utility.Converter.GetBytes(value, buffer);
                        if (writeMode.value == 2) Utility.Converter.GetBytes(value, buffer, offset);
                        restored = offset == 0 ? Utility.Converter.GetInt16(buffer) : Utility.Converter.GetInt16(buffer, offset);
                        break;
                    }
                    case 5:
                    {
                        var value = ushort.Parse(valueInput.text, CultureInfo.InvariantCulture);
                        buffer = writeMode.value == 0 ? Utility.Converter.GetBytes(value) : new byte[2 + offset];
                        if (writeMode.value == 1) Utility.Converter.GetBytes(value, buffer);
                        if (writeMode.value == 2) Utility.Converter.GetBytes(value, buffer, offset);
                        restored = offset == 0 ? Utility.Converter.GetUInt16(buffer) : Utility.Converter.GetUInt16(buffer, offset);
                        break;
                    }
                    case 6:
                    {
                        var value = long.Parse(valueInput.text, CultureInfo.InvariantCulture);
                        buffer = writeMode.value == 0 ? Utility.Converter.GetBytes(value) : new byte[8 + offset];
                        if (writeMode.value == 1) Utility.Converter.GetBytes(value, buffer);
                        if (writeMode.value == 2) Utility.Converter.GetBytes(value, buffer, offset);
                        restored = offset == 0 ? Utility.Converter.GetInt64(buffer) : Utility.Converter.GetInt64(buffer, offset);
                        break;
                    }
                    case 7:
                    {
                        var value = ulong.Parse(valueInput.text, CultureInfo.InvariantCulture);
                        buffer = writeMode.value == 0 ? Utility.Converter.GetBytes(value) : new byte[8 + offset];
                        if (writeMode.value == 1) Utility.Converter.GetBytes(value, buffer);
                        if (writeMode.value == 2) Utility.Converter.GetBytes(value, buffer, offset);
                        restored = offset == 0 ? Utility.Converter.GetUInt64(buffer) : Utility.Converter.GetUInt64(buffer, offset);
                        break;
                    }
                    case 8:
                    {
                        var value = uint.Parse(valueInput.text, CultureInfo.InvariantCulture);
                        buffer = writeMode.value == 0 ? Utility.Converter.GetBytes(value) : new byte[4 + offset];
                        if (writeMode.value == 1) Utility.Converter.GetBytes(value, buffer);
                        if (writeMode.value == 2) Utility.Converter.GetBytes(value, buffer, offset);
                        restored = offset == 0 ? Utility.Converter.GetUInt32(buffer) : Utility.Converter.GetUInt32(buffer, offset);
                        break;
                    }
                    case 9:
                    {
                        var value = double.Parse(valueInput.text, CultureInfo.InvariantCulture);
                        buffer = writeMode.value == 0 ? Utility.Converter.GetBytes(value) : new byte[8 + offset];
                        if (writeMode.value == 1) Utility.Converter.GetBytes(value, buffer);
                        if (writeMode.value == 2) Utility.Converter.GetBytes(value, buffer, offset);
                        restored = offset == 0 ? Utility.Converter.GetDouble(buffer) : Utility.Converter.GetDouble(buffer, offset);
                        break;
                    }
                    case 10:
                    {
                        int size = Encoding.UTF8.GetByteCount(valueInput.text);
                        buffer = writeMode.value == 0 ? Utility.Converter.GetBytes(valueInput.text) : new byte[size + offset];
                        if (writeMode.value == 1) Utility.Converter.GetBytes(valueInput.text, buffer);
                        if (writeMode.value == 2) Utility.Converter.GetBytes(valueInput.text, buffer, offset);
                        // 字符串范围按字节计算，不能用中文字符串的 Length 代替字节数。
                        restored = writeMode.value == 0
                            ? Utility.Converter.GetString(buffer)
                            : Utility.Converter.GetString(buffer, offset, size);
                        break;
                    }
                    default:
                    {
                        var encoding = Encoding.Unicode;
                        int size = encoding.GetByteCount(valueInput.text);
                        buffer = writeMode.value == 0 ? Utility.Converter.GetBytes(valueInput.text, encoding) : new byte[size + offset];
                        if (writeMode.value == 1) Utility.Converter.GetBytes(valueInput.text, encoding, buffer);
                        if (writeMode.value == 2) Utility.Converter.GetBytes(valueInput.text, encoding, buffer, offset);
                        // 指定编码时，写入和还原都使用同一种 Encoding。
                        restored = writeMode.value == 0
                            ? Utility.Converter.GetString(buffer, encoding)
                            : Utility.Converter.GetString(buffer, offset, size, encoding);
                        break;
                    }
                }
                // R 保留浮点数往返所需精度，避免显示时四舍五入掩盖实际还原值。
                string display = restored is double number ? number.ToString("R", CultureInfo.InvariantCulture)
                    : restored is float single ? single.ToString("R", CultureInfo.InvariantCulture)
                    : Convert.ToString(restored, CultureInfo.InvariantCulture);
                resultText.text = $"本机字节序：{(Utility.Converter.IsLittleEndian ? "小端" : "大端")}\n"
                    + $"写入 / 读取偏移：{offset}；数据长度：{buffer.Length - offset} 字节\n"
                    + $"缓冲区：{BitConverter.ToString(buffer)}\n"
                    + "还原值：" + display;
            }
            catch (FormatException) { resultText.text = "输入与所选类型不符。小数使用点号；Boolean 填 true 或 false。"; }
            catch (OverflowException) { resultText.text = "输入超过所选类型的数值范围。"; }
        }
    }
}

