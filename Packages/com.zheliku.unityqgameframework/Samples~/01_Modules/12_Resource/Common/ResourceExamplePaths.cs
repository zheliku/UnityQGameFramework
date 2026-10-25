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
        /// Cube 预制体的 GUID。
        /// </summary>
        public const string CubeGuid = "51d003874f9f553f84bccd96d341285c";

        /// <summary>
        /// 原生数据文件的 GUID。
        /// </summary>
        public const string BinaryGuid = "1422c43f096854ac866b7470b6411b71";

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
                return "先停止运行，执行 QGF > Examples > Build Resource Fixtures。";
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
