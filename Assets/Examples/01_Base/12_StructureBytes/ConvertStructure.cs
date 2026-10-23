using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples
{
    public sealed class ConvertStructure : MonoBehaviour
    {
        [SerializeField] private InputField idInput, speedInput;
        [SerializeField] private Dropdown mode;
        [SerializeField] private Button convertButton, reserveButton, freeButton;
        [SerializeField] private Text resultText, cacheText;

        // 这里只用固定布局、无引用字段的结构体：int 4 字节 + float 4 字节。
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Record
        {
            public int Id;
            public float Speed;
        }

        private void Start()
        {
            mode.ClearOptions();
            mode.AddOptions(new List<string> { "返回新数组", "写入已有数组", "从偏移 2 写入" });
            convertButton.onClick.AddListener(Convert);
            reserveButton.onClick.AddListener(() =>
            {
                // 缓存是框架共用的非托管内存；只在容量不足时重新分配。
                Utility.Marshal.EnsureCachedHGlobalSize(8192);
            });
            freeButton.onClick.AddListener(() =>
            {
                Utility.Marshal.FreeCachedHGlobal();
                resultText.text = "已释放共用缓存。下次转换时会重新申请。";
            });
            Convert();
        }

        private void Convert()
        {
            if (!int.TryParse(idInput.text, out int id)
                || !float.TryParse(speedInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float speed)
                || float.IsNaN(speed) || float.IsInfinity(speed))
            {
                resultText.text = "编号请输入整数，速度请输入有限小数（使用点号）。";
                return;
            }

            var record = new Record { Id = id, Speed = speed };
            int offset = mode.value == 2 ? 2 : 0;
            int size = Marshal.SizeOf<Record>();
            byte[] bytes = mode.value == 0 ? Utility.Marshal.StructureToBytes(record) : new byte[size + offset];
            if (mode.value == 1) Utility.Marshal.StructureToBytes(record, bytes);
            if (mode.value == 2) Utility.Marshal.StructureToBytes(record, bytes, offset);
            // 写入和读取使用相同布局、类型与偏移；调用内部会确保共用缓存容量足够。
            Record restored = offset == 0 ? Utility.Marshal.BytesToStructure<Record>(bytes)
                : Utility.Marshal.BytesToStructure<Record>(bytes, offset);
            resultText.text = $"结构体 {size} 字节；写入 / 读取偏移 {offset}\n"
                + $"缓冲区：{BitConverter.ToString(bytes)}\n还原编号：{restored.Id}\n"
                + "还原速度：" + restored.Speed.ToString("R", CultureInfo.InvariantCulture);
        }

        private void Update()
        {
            // 转换完成后字节数组独立保存；该缓存由后续调用复用，Base 退出时统一释放。
            cacheText.text = $"共用非托管缓存：{Utility.Marshal.CachedHGlobalSize} 字节";
        }
    }
}
