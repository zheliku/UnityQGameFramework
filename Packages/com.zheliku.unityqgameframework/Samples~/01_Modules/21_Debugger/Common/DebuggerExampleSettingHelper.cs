using System.IO;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>将设置保存到示例专用文件。</summary>
    public sealed class DebuggerExampleSettingHelper : DefaultSettingHelper
    {
        public static string ExamplePath => Path.Combine(Application.persistentDataPath,
            "QgfExamples", "Debugger.dat");

        public override bool Load()
        {
            if (!File.Exists(ExamplePath)) return true;
            using (var stream = File.OpenRead(ExamplePath)) Serializer.Deserialize(stream);
            return true;
        }

        public override bool Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ExamplePath));
            using (var stream = File.Create(ExamplePath)) return Serializer.Serialize(stream, Setting);
        }
    }
}
