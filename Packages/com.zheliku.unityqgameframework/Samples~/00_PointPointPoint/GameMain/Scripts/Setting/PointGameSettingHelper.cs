using System.IO;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.PointPointPoint
{
    // 最高分保存到示例专用文件。
    public sealed class PointGameSettingHelper : DefaultSettingHelper
    {
        public static string SavePath => Path.Combine(Application.persistentDataPath, "QgfExamples", "PointPointPoint.dat");

        public override bool Load()
        {
            if (!File.Exists(SavePath)) return true;
            using (var stream = File.OpenRead(SavePath)) Serializer.Deserialize(stream);
            return true;
        }

        public override bool Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            using (var stream = File.Create(SavePath)) return Serializer.Serialize(stream, Setting);
        }
    }
}
