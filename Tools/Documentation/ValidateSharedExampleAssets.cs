using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityQGameFramework.Runtime;

// 只在隔离工程比较合并前后的场景配置；不播放，不切换开发主工程场景。
public static class ValidateSharedExampleAssets
{
    public static object Before() => Inspect(false);
    public static object After() => Inspect(true);

    private static object Inspect(bool migrated)
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Use the isolated editor outside Play Mode.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new Exception("Unsaved scene.");
        var snapshots = new SortedDictionary<string, object>();
        int scenes = 0, prefabs = 0;
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var layout = JArray.Parse(File.ReadAllText("Tools/Documentation/examples-layout.json"));
            foreach (var entry in layout)
            {
                string module = "Assets/Examples/" + (string)entry["folder"];
                foreach (string path in Directory.GetFiles(module, "*.unity", SearchOption.AllDirectories).OrderBy(p => p))
                {
                    var scene = EditorSceneManager.OpenScene(path.Replace('\\', '/'));
                    var roots = scene.GetRootGameObjects();
                    if (roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                        .Any(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0))
                        throw new Exception("Missing script: " + path);
                    var framework = roots.FirstOrDefault(g => g.GetComponent<BaseComponent>() != null);
                    if (framework != null)
                    {
                        if (migrated)
                        {
                            if (framework.name != "QGF" || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(framework) != "Assets/Examples/01_Modules/Common/Prefabs/QGF.prefab")
                                throw new Exception("Expected shared QGF instance: " + path);
                            prefabs++;
                        }
                        var fields = new SortedDictionary<string, string>();
                        foreach (var component in framework.GetComponentsInChildren<MonoBehaviour>(true))
                        {
                            var serialized = new SerializedObject(component);
                            var property = serialized.GetIterator();
                            while (property.NextVisible(true))
                            {
                                if (property.propertyPath == "m_Name" || property.propertyPath == "m_GameObject")
                                    continue;
                                string value = null;
                                switch (property.propertyType)
                                {
                                    case SerializedPropertyType.Integer:
                                        value = property.longValue.ToString();
                                        break;
                                    case SerializedPropertyType.Boolean:
                                        value = property.boolValue.ToString();
                                        break;
                                    case SerializedPropertyType.Float:
                                        value = property.doubleValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                                        break;
                                    case SerializedPropertyType.String:
                                        value = property.stringValue;
                                        break;
                                    case SerializedPropertyType.Enum:
                                        value = property.intValue.ToString();
                                        break;
                                    case SerializedPropertyType.ObjectReference:
                                        var reference = property.objectReferenceValue;
                                        if (reference == null)
                                            value = "null";
                                        else if (AssetDatabase.Contains(reference))
                                            value = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(reference));
                                        else if (reference is Component c)
                                            value = Relative(c.transform, framework.transform) + ":" + c.GetType().Name;
                                        else if (reference is GameObject g)
                                            value = Relative(g.transform, framework.transform);
                                        else
                                            value = reference.name;
                                        break;
                                }
                                if (value != null)
                                    fields[Relative(component.transform, framework.transform) + ":" + component.GetType().Name + "." + property.propertyPath] = value;
                            }
                        }
                        snapshots[path.Replace('\\', '/')] = fields;
                    }
                    if (migrated)
                        EditorSceneManager.SaveScene(scene);
                    scenes++;
                }
            }
        }
        finally
        {
            if (setup.Any(s => s.isActive && s.isLoaded && !string.IsNullOrEmpty(s.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Directory.CreateDirectory("Artifacts~/SharedExamples");
        string file = "Artifacts~/SharedExamples/" + (migrated ? "after" : "before") + ".json";
        string json = JsonConvert.SerializeObject(snapshots, Formatting.Indented);
        File.WriteAllText(file, json);
        if (migrated)
        {
            var before = JObject.Parse(File.ReadAllText("Artifacts~/SharedExamples/before.json"));
            var after = JObject.Parse(json);
            var differences = before.Properties().Where(p => !JToken.DeepEquals(p.Value, after[p.Name])).Select(p => p.Name).ToArray();
            if (differences.Length != 0)
                throw new Exception("Framework configuration changed: " + string.Join(", ", differences));
            if (Directory.GetFiles("Assets/Examples/01_Modules/Common/Prefabs", "*.prefab").Length != 1)
                throw new Exception("Expected one shared prefab.");
        }
        return new
        {
            passed = true,
            scenes,
            frameworkInstances = snapshots.Count,
            sharedInstances = prefabs,
            configurationsPreserved = migrated
        };
    }

    private static string Relative(Transform value, Transform root)
    {
        if (value == root)
            return "QGF";
        return value.IsChildOf(root) ? "QGF/" + AnimationUtility.CalculateTransformPath(value, root) : value.name;
    }
}
