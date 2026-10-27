using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 检查示例的脚本绑定和资源归属。
/// </summary>
public static class ValidateSampleStructure
{
    /// <summary>
    /// 使用移动后的 Profile 重新生成模块示例配置。
    /// </summary>
    /// <returns>生成结果。</returns>
    public static async Task<object> GenerateTables()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use the isolated Consumer in Edit Mode.");
        }
        const string path = "Assets/Examples/01_Modules/UnityQGameFramework/Config/LubanProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<UnityQGameFramework.Editor.LubanProfile>(path);
        await UnityQGameFramework.Editor.LubanGenerator.GenerateAsync(profile);
        return new { generated = true, profile = path };
    }

    /// <summary>
    /// 检查三个示例目录。
    /// </summary>
    /// <returns>场景、预制体和依赖检查结果。</returns>
    public static object Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use the isolated Consumer in Edit Mode.");
        }

        var setup = EditorSceneManager.GetSceneManagerSetup();
        var errors = new List<string>();
        int scenes = 0;
        int prefabs = 0;
        int dependencies = 0;
        try
        {
            foreach (string sample in new[] { "00_PointPointPoint", "01_Modules", "02_StarForce" })
            {
                string root = "Assets/Examples/" + sample;
                string[] assets = AssetDatabase.FindAssets("t:Scene t:Prefab", new[] { root })
                    .Select(AssetDatabase.GUIDToAssetPath).ToArray();
                foreach (string dependency in AssetDatabase.GetDependencies(assets, true))
                {
                    if (dependency.StartsWith("Assets/", StringComparison.Ordinal)
                        && !dependency.StartsWith(root + "/", StringComparison.Ordinal))
                    {
                        errors.Add(sample + " external asset: " + dependency);
                    }
                    dependencies++;
                }

                foreach (string asset in assets)
                {
                    if (asset.EndsWith(".unity", StringComparison.Ordinal))
                    {
                        var scene = EditorSceneManager.OpenScene(asset);
                        foreach (GameObject gameObject in scene.GetRootGameObjects())
                        {
                            Inspect(gameObject, asset, errors);
                        }
                        scenes++;
                    }
                    else
                    {
                        var prefab = PrefabUtility.LoadPrefabContents(asset);
                        try
                        {
                            Inspect(prefab, asset, errors);
                            prefabs++;
                        }
                        finally
                        {
                            PrefabUtility.UnloadPrefabContents(prefab);
                        }
                    }
                }
            }
        }
        finally
        {
            if (setup.Any(scene => scene.isLoaded && scene.isActive && !string.IsNullOrEmpty(scene.path)))
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            }
        }

        var result = new { passed = errors.Count == 0, scenes, prefabs, dependencies, errors };
        Directory.CreateDirectory("Artifacts~/ExamplesAlignment");
        File.WriteAllText("Artifacts~/ExamplesAlignment/structure.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static void Inspect(GameObject root, string asset, List<string> errors)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
            {
                errors.Add(asset + ": missing script on " + transform.name);
            }
        }

        foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null)
            {
                continue;
            }
            var data = new SerializedObject(component);
            var property = data.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference
                    && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                {
                    errors.Add(asset + ": broken reference " + component.name + "." + property.propertyPath);
                }
            }
        }
    }
}
