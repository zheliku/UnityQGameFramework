using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示结构体与字节。
    /// </summary>
    public sealed class ConvertStructure : MonoBehaviour
    {
        [SerializeField]
        private InputField idInput, speedInput;
        [SerializeField]
        private Button convertButton, reserveButton, freeButton;
        [SerializeField]
        private Text resultText, cacheText;

        /// <summary>
        /// 用于结构体字节转换的编号和速度记录。
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Record
        {
            /// <summary>
            /// 获取当前标识。
            /// </summary>
            public int Id;

            /// <summary>
            /// 速度。
            /// </summary>
            public float Speed;
        }

        private void Start()
        {
            convertButton.onClick.AddListener(Convert);
            reserveButton.onClick.AddListener(() => Utility.Marshal.EnsureCachedHGlobalSize(8192));
            freeButton.onClick.AddListener(() =>
            {
                Utility.Marshal.FreeCachedHGlobal();
                resultText.text = "Shared buffer released.";
            });
            Convert();
        }

        private void Convert()
        {
            if (!int.TryParse(idInput.text, out int id) || !float.TryParse(speedInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float speed))
            {
                resultText.text = "Enter an integer and a decimal.";
                return;
            }

            var record = new Record
            {
                Id = id,
                Speed = speed
            };
            int size = Marshal.SizeOf<Record>();
            var bytes = new byte[size + 2];
            // 结构体写入带包头的缓冲区，再从同一偏移还原。
            Utility.Marshal.StructureToBytes(record, bytes, 2);
            Record restored = Utility.Marshal.BytesToStructure<Record>(bytes, 2);
            resultText.text = $"Structure {size} bytes, offset 2\nBuffer: {BitConverter.ToString(bytes)}\n" + $"Restored: Id {restored.Id}，Speed {restored.Speed.ToString("R", CultureInfo.InvariantCulture)}";
        }

        private void Update()
        {
            cacheText.text = $"Shared unmanaged buffer: {Utility.Marshal.CachedHGlobalSize} bytes";
        }
    }
}
