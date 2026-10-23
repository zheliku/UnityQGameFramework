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
    public sealed class CompressData : MonoBehaviour
    {
        [SerializeField] private InputField contentInput;
        [SerializeField] private Dropdown mode;
        [SerializeField] private Button compressButton;
        [SerializeField] private Button invalidButton;
        [SerializeField] private Text resultText;

        private void Start()
        {
            mode.ClearOptions();
            mode.AddOptions(new List<string> { "数组 → 数组", "数组范围 → 数组", "流 → 数组", "数组 → 输出流", "数组范围 → 输出流", "流 → 输出流" });
            contentInput.text = string.Concat(Enumerable.Repeat("QGF 配置与存档。", 20));
            compressButton.onClick.AddListener(RoundTrip);
            invalidButton.onClick.AddListener(CheckInvalid);
            RoundTrip();
        }

        private void RoundTrip()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(contentInput.text);
            // 加入两字节包头。范围重载和流重载跳过包头，只压缩正文。
            var packet = new byte[bytes.Length + 2];
            packet[0] = 0x51;
            packet[1] = 0x47;
            Array.Copy(bytes, 0, packet, 2, bytes.Length);
            byte[] compressed;
            byte[] restored;
            using (var input = new MemoryStream(packet))
            using (var output = new MemoryStream())
            using (var decoded = new MemoryStream())
            {
                input.Position = 2;
                // Base 安装默认 GZip Helper。数组返回重载失败时为 null，输出流重载返回 bool。
                bool success = true;
                switch (mode.value)
                {
                    case 0: compressed = Utility.Compression.Compress(bytes); break;
                    case 1: compressed = Utility.Compression.Compress(packet, 2, bytes.Length); break;
                    case 2: compressed = Utility.Compression.Compress(input); break;
                    case 3: success = Utility.Compression.Compress(bytes, output); compressed = output.ToArray(); break;
                    case 4: success = Utility.Compression.Compress(packet, 2, bytes.Length, output); compressed = output.ToArray(); break;
                    default: success = Utility.Compression.Compress(input, output); compressed = output.ToArray(); break;
                }
                if (!success || compressed == null)
                {
                    resultText.text = "压缩失败。";
                    return;
                }

                var framed = new byte[compressed.Length + 2];
                Array.Copy(compressed, 0, framed, 2, compressed.Length);
                using (var encoded = new MemoryStream(framed))
                {
                    encoded.Position = 2;
                    switch (mode.value)
                    {
                        case 0: restored = Utility.Compression.Decompress(compressed); break;
                        case 1: restored = Utility.Compression.Decompress(framed, 2, compressed.Length); break;
                        case 2: restored = Utility.Compression.Decompress(encoded); break;
                        case 3: success = Utility.Compression.Decompress(compressed, decoded); restored = decoded.ToArray(); break;
                        case 4: success = Utility.Compression.Decompress(framed, 2, compressed.Length, decoded); restored = decoded.ToArray(); break;
                        default: success = Utility.Compression.Decompress(encoded, decoded); restored = decoded.ToArray(); break;
                    }
                }
                if (!success || restored == null)
                {
                    resultText.text = "解压失败。";
                    return;
                }
                // ToArray 取出整个输出；若要 Read 输出流，先把它的 Position 归零。
                resultText.text = $"原文 {bytes.Length} → GZip {compressed.Length} → 还原 {restored.Length} 字节\n"
                    + $"还原结果：{(bytes.SequenceEqual(restored) ? "一致" : "不一致")}\n"
                    + (mode.value % 3 == 0 ? "完整正文数组" : mode.value % 3 == 1 ? $"范围 [2, {packet.Length})，跳过包头" : $"输入流 Position：2 → {input.Position}")
                    + $"\n还原内容：{Encoding.UTF8.GetString(restored)}";
            }
        }

        private void CheckInvalid()
        {
            byte[] invalid = Encoding.UTF8.GetBytes("This is not GZip.");
            byte[] restored = Utility.Compression.Decompress(invalid);
            using (var output = new MemoryStream())
            {
                bool success = Utility.Compression.Decompress(invalid, output);
                resultText.text = $"普通文本不是 GZip 数据。\n数组接口：{(restored == null ? "返回 null" : "返回数据")}\n"
                    + $"输出流接口：{(success ? "解压成功" : "返回失败（false）")}";
            }
        }
    }
}
