using System;
using System.IO;
using UnityEngine;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 定位资源示例的构建产物。
    /// </summary>
    public sealed class ResourceExamplePaths : MonoBehaviour
    {
        /// <summary>
        /// 默认资源包中 Cube 预制体的地址。
        /// </summary>
        public const string CubeAddress = "Resource_Cube";

        /// <summary>
        /// 远程包中 Cube 预制体的地址。
        /// </summary>
        public const string RemoteCubeAddress = "Base_Cube";

        /// <summary>
        /// 原生数据文件的地址。
        /// </summary>
        public const string BinaryAddress = "Archive";

        /// <summary>
        /// 原生文件资源包名称。
        /// </summary>
        public const string RawPackageName = "QgfExamples.Modules.Raw";

        /// <summary>
        /// 本地更新示例的资源包名称。
        /// </summary>
        public const string RemotePackageName = "QgfExamples.Modules.Remote";

        /// <summary>
        /// 获取示例产物目录。
        /// </summary>
        public static string BuildRoot
        {
            get
            {
                return Path.GetFullPath("Library/QgfExamples/YooAsset/" + Application.unityVersion);
            }
        }

        /// <summary>
        /// 获取本地更新示例的构建记录。
        /// </summary>
        public static ResourceFixtureManifest Fixture { get; private set; }

        /// <summary>
        /// 获取尚未构建资源时的提示。
        /// </summary>
        public static string Notice
        {
            get
            {
                return "先停止运行，选中 UpdateRemoteResources 对象并在 Inspector 点击“构建远程资源示例”。";
            }
        }

        private void Awake()
        {
            string path = Path.Combine(BuildRoot, "fixture.json");
            Fixture = File.Exists(path) ? JsonUtility.FromJson<ResourceFixtureManifest>(File.ReadAllText(path)) : null;
            if (Fixture != null && Fixture.UnityVersion != Application.unityVersion)
            {
                Fixture = null;
            }
        }
    }

    /// <summary>
    /// 本地更新示例的构建记录。
    /// </summary>
    [Serializable]
    public sealed class ResourceFixtureManifest
    {
        /// <summary>
        /// 构建时的 Unity 版本。
        /// </summary>
        public string UnityVersion;

        /// <summary>
        /// 资源包输出目录。
        /// </summary>
        public string RemoteFolder;
    }
}
