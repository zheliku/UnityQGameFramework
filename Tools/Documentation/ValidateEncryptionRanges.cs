using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityQGameFramework.Core;

// 资源示例之外的边界回归：短文件、区间起点和缓冲区保护。
public static class ValidateEncryptionRanges
{
    public static object Run()
    {
        var checks = new List<string>();
        Action<bool, string> require = (condition, label) =>
        {
            if (!condition)
                throw new Exception(label);
            checks.Add(label);
        };
        byte[] key = { 17, 39, 63, 81 };
        byte[] source = Enumerable.Range(0, 300).Select(value => (byte)value).ToArray();
        byte[] encoded = Utility.Encryption.GetXorBytes(source, key);
        require(!encoded.SequenceEqual(source) && source[0] == 0 && source[1] == 1, "Encoding returns a copy and preserves original");
        require(encoded.SequenceEqual(source.Select((value, index) => (byte)(value ^ key[index % 4]))), "Full encoding matches known XOR vector");
        require(Utility.Encryption.GetXorBytes(encoded, key).SequenceEqual(source), "Full round trip");

        byte[] slice = (byte[])source.Clone();
        Utility.Encryption.GetSelfXorBytes(slice, 3, 7, key);
        require(slice.Take(3).SequenceEqual(source.Take(3)) && slice.Skip(10).SequenceEqual(source.Skip(10)), "In-place slice preserves prefix and suffix");
        require(slice.Skip(3).Take(7).SequenceEqual(encoded.Skip(3).Take(7)), "Slice length is a count; key phase follows source index");
        Utility.Encryption.GetSelfXorBytes(slice, 3, 7, key);
        require(slice.SequenceEqual(source), "In-place slice round trip");
        byte[] tail = Utility.Encryption.GetXorBytes(source, 3, -1, key);
        require(tail.Take(3).SequenceEqual(source.Take(3)) && tail.Skip(3).SequenceEqual(encoded.Skip(3)), "Documented negative length processes remaining bytes");

        byte[] shortData = { 1, 2, 3 };
        require(Utility.Encryption.GetQuickXorBytes(shortData, key).SequenceEqual(new byte[] { 16, 37, 60 }), "Quick encoding accepts files shorter than 220 bytes");
        Utility.Encryption.GetQuickSelfXorBytes(shortData, key);
        require(shortData.SequenceEqual(new byte[] { 16, 37, 60 }), "Quick in-place encoding accepts short files");
        byte[] quick = Utility.Encryption.GetQuickXorBytes(source, key);
        require(quick.Take(220).SequenceEqual(encoded.Take(220)) && quick.Skip(220).SequenceEqual(source.Skip(220)), "Quick encoding leaves bytes after 220 untouched");
        Utility.Encryption.GetQuickSelfXorBytes(quick, key);
        require(quick.SequenceEqual(source), "Quick round trip at boundary");
        require(Utility.Encryption.GetQuickXorBytes(Array.Empty<byte>(), key).Length == 0, "Quick encoding accepts empty file");
        require(Utility.Encryption.GetQuickXorBytes(null, key) == null, "Null-copy contract preserved");
        Utility.Encryption.GetQuickSelfXorBytes(null, key);
        Utility.Encryption.GetSelfXorBytes(slice, slice.Length, 0, key);
        require(slice.SequenceEqual(source), "Zero-length interval at end leaves buffer unchanged");
        foreach (var range in new[] { new[] { -1, 1 }, new[] { 301, 0 }, new[] { 2, 300 }, new[] { 2, int.MaxValue } })
        {
            bool rejected = false;
            try
            {
                Utility.Encryption.GetSelfXorBytes(slice, range[0], range[1], key);
            }
            catch (GameFrameworkException) { rejected = true; }
            require(rejected && slice.SequenceEqual(source), "Invalid interval rejected before writing: " + string.Join(",", range));
        }
        var result = new
        {
            passed = true,
            unity = Application.unityVersion,
            assertionCount = checks.Count,
            checks
        };
        Directory.CreateDirectory("Artifacts~/ResourceVariantExamples");
        File.WriteAllText("Artifacts~/ResourceVariantExamples/encryption-ranges.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
