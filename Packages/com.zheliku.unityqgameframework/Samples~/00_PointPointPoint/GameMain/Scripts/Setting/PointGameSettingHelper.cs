using System.IO;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.PointPointPoint
{
    // 最高分保存到示例专用文件。
    /// <summary>
    /// 将点点点最高纪录保存到独立文件。
    /// </summary>
    public sealed class PointGameSettingHelper : DefaultSettingHelper
    {
        /// <summary>
        /// 获取示例存档路径。
        /// </summary>
        public static string SavePath
        {
            get
            {
                return Path.Combine(Application.persistentDataPath, "QgfExamples", "PointPointPoint.dat");
            }
        }

        /// <summary>
        /// 加载游戏配置。
        /// </summary>
        /// <returns>是否加载游戏配置成功。</returns>
        public override bool Load()
        {
            if (!File.Exists(SavePath))
            {
                return true;
            }

            using (var stream = File.OpenRead(SavePath))
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
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            using (var stream = File.Create(SavePath))
            {
                return Serializer.Serialize(stream, Setting);
            }
        }
    }
}
