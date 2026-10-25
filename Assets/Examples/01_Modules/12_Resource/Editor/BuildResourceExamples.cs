using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules.Editor
{
    /// <summary>
    /// 构建本地 HTTP 更新示例的资源包。
    /// </summary>
    public static class BuildResourceExamples
    {
        /// <summary>
        /// 从菜单构建资源。
        /// </summary>
        [MenuItem("QGF/Examples/Build Resource Fixtures", false, 400)]
        public static void BuildMenu()
        {
            try
            {
                Build();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>
        /// 构建更新示例资源并保存构建记录。
        /// </summary>
        /// <returns>本地更新示例的构建记录。</returns>
        public static ResourceFixtureManifest Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before building resources.");
            }
            var settings = AssetDatabase.LoadAssetAtPath<ResourceSettings>(AssetDatabase.GUIDToAssetPath("fc36230cdf792f762eec688199f11557"));
            ResourceBuildProfile profile = ResourceBuildService.GetProfile(settings, ResourceExamplePaths.RemotePackageName);
            string root = ResourceExamplePaths.BuildRoot;
            Directory.CreateDirectory(root);
            string record = Path.Combine(root, "fixture.json");
            if (File.Exists(record))
            {
                File.Delete(record);
            }
            // 资源包输出保留在 QgfBuild~，本地服务器直接读取，不写入 StreamingAssets。
            string output = ResourceBuildService.Build(profile, false);
            var fixture = new ResourceFixtureManifest
            {
                UnityVersion = Application.unityVersion,
                RemoteFolder = output
            };
            File.WriteAllText(record, JsonUtility.ToJson(fixture, true));
            return fixture;
        }
    }
}
