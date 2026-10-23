using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples
{
    public sealed class CheckAndXor : MonoBehaviour
    {
        [SerializeField] private InputField contentInput;
        [SerializeField] private InputField keyInput;
        [SerializeField] private Dropdown crcMode;
        [SerializeField] private Dropdown xorMode;
        [SerializeField] private Button crcButton;
        [SerializeField] private Button xorButton;
        [SerializeField] private Button longTextButton;
        [SerializeField] private Text crcText;
        [SerializeField] private Text xorText;

        private void Start()
        {
            crcMode.ClearOptions();
            crcMode.AddOptions(new List<string> { "完整数组", "数组范围（跳过前 2 字节）", "流（从 Position=2 读到末尾）" });
            xorMode.ClearOptions();
            xorMode.AddOptions(new List<string> { "副本 · 全部", "副本 · 范围", "副本 · Quick", "原地 · 全部", "原地 · 范围", "原地 · Quick" });
            crcButton.onClick.AddListener(CheckCrc);
            xorButton.onClick.AddListener(CheckXor);
            longTextButton.onClick.AddListener(() => contentInput.text = new string('A', 300));
            CheckCrc();
            CheckXor();
        }

        private void CheckCrc()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(contentInput.text);
            int offset = crcMode.value == 0 ? 0 : Math.Min(2, bytes.Length);
            int crc;
            if (crcMode.value == 0) crc = Utility.Verifier.GetCrc32(bytes);
            else if (crcMode.value == 1) crc = Utility.Verifier.GetCrc32(bytes, offset, bytes.Length - offset);
            else
            {
                using (var stream = new MemoryStream(bytes))
                {
                    // 流重载从当前位置计算，并把 Position 推进到末尾。
                    stream.Position = offset;
                    crc = Utility.Verifier.GetCrc32(stream);
                }
            }

            // 三种写入方式结果相同。CRC 字节固定为大端序，与 Converter 的本机字节序不同。
            byte[] allocated = Utility.Verifier.GetCrc32Bytes(crc);
            var buffer = new byte[4];
            Utility.Verifier.GetCrc32Bytes(crc, buffer);
            var packet = new byte[6];
            Utility.Verifier.GetCrc32Bytes(crc, packet, 2);
            crcText.text = $"UTF-8：{bytes.Length} 字节；校验范围 [{offset}, {bytes.Length})\n"
                + $"CRC32：{unchecked((uint)crc):X8}\n新数组：{BitConverter.ToString(allocated)}\n"
                + $"已有数组：{BitConverter.ToString(buffer)}\n偏移 2：{BitConverter.ToString(packet)}";
        }

        private void CheckXor()
        {
            byte[] original = Encoding.UTF8.GetBytes(contentInput.text);
            byte[] key = Encoding.UTF8.GetBytes(keyInput.text);
            if (key.Length == 0)
            {
                xorText.text = "异或键不能为空。";
                return;
            }

            int offset = Math.Min(2, original.Length);
            int length = Math.Max(0, original.Length - offset - 2);
            byte[] working = (byte[])original.Clone();
            byte[] encoded = Transform(working, key, offset, length);
            int changed = original.Where((value, index) => value != encoded[index]).Count();
            bool sameArray = ReferenceEquals(working, encoded);
            string preview = BitConverter.ToString(encoded, 0, Math.Min(24, encoded.Length));
            // 同一范围、同一个键再异或一次即可还原；XOR 用于字节混淆。
            byte[] restored = Transform(encoded, key, offset, length);
            bool restoredMatches = restored.SequenceEqual(original);
            string range = xorMode.value % 3 == 0 ? $"[0, {original.Length})"
                : xorMode.value % 3 == 1 ? $"[{offset}, {offset + length})" : $"[0, {Math.Min(220, original.Length)})";
            xorText.text = $"处理范围 {range}；改变 {changed} / {original.Length} 字节\n"
                + $"还原结果：{(restoredMatches ? "一致" : "不一致")}；"
                + $"返回数组：{(sameArray ? "原数组" : "新数组")}\n"
                + $"变换后前 24 字节：{preview}";
        }

        private byte[] Transform(byte[] bytes, byte[] key, int offset, int length)
        {
            // 副本重载保留原数组；Self 重载直接改写 bytes。Quick 只处理前 220 字节。
            switch (xorMode.value)
            {
                case 0: return Utility.Encryption.GetXorBytes(bytes, key);
                case 1: return Utility.Encryption.GetXorBytes(bytes, offset, length, key);
                case 2: return Utility.Encryption.GetQuickXorBytes(bytes, key);
                case 3: Utility.Encryption.GetSelfXorBytes(bytes, key); break;
                case 4: Utility.Encryption.GetSelfXorBytes(bytes, offset, length, key); break;
                case 5: Utility.Encryption.GetQuickSelfXorBytes(bytes, key); break;
            }
            return bytes;
        }
    }
}
