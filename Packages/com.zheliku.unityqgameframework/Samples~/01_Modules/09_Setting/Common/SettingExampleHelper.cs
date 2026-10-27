using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    // 各场景共用存储实现，按场景名分开保存。
    /// <summary>
    /// 按场景名隔离示例的存档文件。
    /// </summary>
    public sealed class SettingExampleHelper : DefaultSettingHelper
    {
        private string filePath;

        /// <summary>
        /// 获取当前示例独立的存档路径。
        /// </summary>
        public static string ExamplePath
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, "QgfExamples", "Setting", SceneManager.GetActiveScene().name + ".dat");
            }
        }

        /// <summary>
        /// 加载游戏配置。
        /// </summary>
        /// <returns>是否加载游戏配置成功。</returns>
        public override bool Load()
        {
            // 首次加载时确定文件；重新读取和退出保存都沿用它，不受活动场景切换影响。
            if (filePath == null)
            {
                filePath = ExamplePath;
            }

            if (!File.Exists(filePath))
            {
                return true;
            }

            using (var stream = File.OpenRead(filePath))
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
            if (filePath == null)
            {
                filePath = ExamplePath;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using (var stream = File.Create(filePath))
            {
                return Serializer.Serialize(stream, Setting);
            }
        }
    }
}
