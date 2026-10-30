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
        /// 只定制路径，读取、备份和原子写入沿用默认辅助器。
        /// </summary>
        /// <returns>当前示例的配置文件路径。</returns>
        protected override string GetFilePath()
        {
            return ExamplePath;
        }
    }
}
