#if UNITY_EDITOR
using System;
using System.IO;
using UnityQGameFramework.Runtime;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples.Modules.Editor
{
    /// <summary>
    /// 绘制远程资源更新示例的编辑器操作。
    /// </summary>
    [CustomEditor(typeof(UpdateRemoteResources))]
    public sealed class UpdateRemoteResourcesInspector : UnityEditor.Editor
    {
        /// <summary>
        /// 绘制示例组件 Inspector。
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if (!GUILayout.Button("Build Remote Resources Example"))
            {
                return;
            }
            try
            {
                BuildResources();
                AssetDatabase.Refresh();
            }
            catch (System.Exception exception)
            {
                FrameworkLog.Error(exception);
            }
        }

        /// <summary>
        /// 构建本示例的远程资源并保存构建记录。
        /// </summary>
        /// <returns>本地更新示例的构建记录。</returns>
        public static ResourceFixtureManifest BuildResources()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Stop Play Mode before building resources.");
            }

            ResourceSettings settings = FindSettings();
            string root = ResourceExamplePaths.BuildRoot;
            Directory.CreateDirectory(root);
            string record = Path.Combine(root, "fixture.json");
            if (File.Exists(record))
            {
                File.Delete(record);
            }

            // 通过编辑器注册的构建方法调用 YooAsset，不让示例的运行时程序集引用 Editor 程序集。
            string output = ResourceComponent.EditorPackageBuilder(settings, ResourceExamplePaths.RemotePackageName, false);
            var fixture = new ResourceFixtureManifest
            {
                UnityVersion = Application.unityVersion,
                RemoteFolder = output
            };
            File.WriteAllText(record, JsonUtility.ToJson(fixture, true));
            UnityQGameFramework.Core.FrameworkLog.Info("Resource update example built: " + output);
            return fixture;
        }

        private static ResourceSettings FindSettings()
        {
            ResourceSettings result = null;
            foreach (string guid in AssetDatabase.FindAssets("t:ResourceSettings"))
            {
                ResourceSettings settings = AssetDatabase.LoadAssetAtPath<ResourceSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings != null && settings.DefaultPackageName == "QgfExamples.Modules")
                {
                    if (result != null)
                    {
                        throw new InvalidOperationException("Duplicate module ResourceSettings assets.");
                    }
                    result = settings;
                }
            }
            if (result == null)
            {
                throw new InvalidOperationException("Module ResourceSettings asset is missing.");
            }
            return result;
        }
    }
}
#endif
