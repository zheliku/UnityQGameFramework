using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class ConvertStructure : MonoBehaviour
    {
        [SerializeField] private InputField idInput, speedInput;
        [SerializeField] private Button convertButton, reserveButton, freeButton;
        [SerializeField] private Text resultText, cacheText;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Record { public int Id; public float Speed; }

        private void Start()
        {
            convertButton.onClick.AddListener(Convert);
            reserveButton.onClick.AddListener(() => Utility.Marshal.EnsureCachedHGlobalSize(8192));
            freeButton.onClick.AddListener(() =>
            {
                Utility.Marshal.FreeCachedHGlobal();
                resultText.text = "已释放共用缓存。";
            });
            Convert();
        }

        private void Convert()
        {
            if (!int.TryParse(idInput.text, out int id)
                || !float.TryParse(speedInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float speed))
            { resultText.text = "请输入整数和小数。"; return; }
            var record = new Record { Id = id, Speed = speed };
            int size = Marshal.SizeOf<Record>();
            var bytes = new byte[size + 2];
            // 结构体写入带包头的缓冲区，再从同一偏移还原。
            Utility.Marshal.StructureToBytes(record, bytes, 2);
            Record restored = Utility.Marshal.BytesToStructure<Record>(bytes, 2);
            resultText.text = $"结构体 {size} 字节，偏移 2\n缓冲区：{BitConverter.ToString(bytes)}\n"
                + $"还原：Id {restored.Id}，Speed {restored.Speed.ToString("R", CultureInfo.InvariantCulture)}";
        }

        private void Update() => cacheText.text = $"共用非托管缓存：{Utility.Marshal.CachedHGlobalSize} 字节";
    }
}
