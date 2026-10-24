using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    // 各场景共用存储实现，按场景名分开保存。
    public sealed class SettingExampleHelper : DefaultSettingHelper
    {
        private string filePath;

        public static string ExamplePath => Path.Combine(Application.persistentDataPath,
            "QgfExamples", "Setting", SceneManager.GetActiveScene().name + ".dat");

        public override bool Load()
        {
            // 首次加载时确定文件；重新读取和退出保存都沿用它，不受活动场景切换影响。
            if (filePath == null) filePath = ExamplePath;
            if (!File.Exists(filePath)) return true;
            using (var stream = File.OpenRead(filePath)) Serializer.Deserialize(stream);
            return true;
        }

        public override bool Save()
        {
            if (filePath == null) filePath = ExamplePath;
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using (var stream = File.Create(filePath)) return Serializer.Serialize(stream, Setting);
        }
    }
}
