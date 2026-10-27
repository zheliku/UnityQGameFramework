using System.IO;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 将设置保存到示例专用文件。
    /// </summary>
    public sealed class DebuggerExampleSettingHelper : DefaultSettingHelper
    {
        /// <summary>
        /// 获取当前示例独立的存档路径。
        /// </summary>
        public static string ExamplePath
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, "QgfExamples", "Debugger.dat");
            }
        }

        /// <summary>
        /// 加载游戏配置。
        /// </summary>
        /// <returns>是否加载游戏配置成功。</returns>
        public override bool Load()
        {
            if (!File.Exists(ExamplePath))
            {
                return true;
            }

            using (var stream = File.OpenRead(ExamplePath))
            {
                Serializer.Deserialize(stream);
            }

            return true;
        }

        /// <summary>
        /// 保存游戏配置。
        /// </summary>
        /// <returns>是否保存游戏配置成功。</returns>
        public override bool Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ExamplePath));
            using (var stream = File.Create(ExamplePath))
            {
                return Serializer.Serialize(stream, Setting);
            }
        }
    }
}
