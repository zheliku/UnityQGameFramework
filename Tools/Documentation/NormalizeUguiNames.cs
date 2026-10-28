using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 仓库制作工具：只在隔离工程处理已保存资产，不随 Sample 导入。
public static class NormalizeUguiNames
{
    private static readonly List<object> changes = new List<object>();
    private static readonly Dictionary<GameObject, string> hints = new Dictionary<GameObject, string>();
    private static JObject layouts;
    private static readonly string[] Prefixes = { "Canvas", "Button", "Toggle", "Slider", "Scrollbar", "Dropdown", "InputField", "ScrollView", "Text", "RawImage", "Image", "Panel" };

    public static object Run() => RunModules(null);

    public static object RunModules(string[] folders) => RunSelected(folders, null);

    public static object RunExamples(string[] names) => RunSelected(null, names);

    private static object RunSelected(string[] folders, string[] names)
    {
        if (!Path.GetFullPath(Application.dataPath).Contains(Path.DirectorySeparatorChar + "Artifacts~" + Path.DirectorySeparatorChar))
            throw new Exception("Use the isolated authoring project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new Exception("Unsaved scene.");
        changes.Clear();
        layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var roots = folders == null ? new[] { "Assets/Examples" } : folders.Select(folder => "Assets/Examples/" + folder).ToArray();
        var cases = names?.Select(name => Path.GetDirectoryName((string)layouts[name]["script"]).Replace('\\', '/')).ToArray();
        int prefabs = 0, scenes = 0, uiObjects = 0;
        try
        {
            foreach (string path in AssetDatabase.FindAssets("t:Prefab", roots).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
            {
                if (cases != null && !cases.Any(folder => path.StartsWith(folder + "/", StringComparison.Ordinal)))
                    continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int start = changes.Count;
                    ReadControlNames(new[] { root });
                    // Unity keeps a prefab root equal to its asset filename. UGF UIForm roots retain that identity.
                    uiObjects += Rename(new[] { root.transform }, path, false);
                    if (changes.Count != start)
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabs++;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (string path in AssetDatabase.FindAssets("t:Scene", roots).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
            {
                if (cases != null && !cases.Any(folder => path.StartsWith(folder + "/", StringComparison.Ordinal)))
                    continue;
                var scene = EditorSceneManager.OpenScene(path);
                int start = changes.Count;
                ReadControlNames(scene.GetRootGameObjects());
                uiObjects += Rename(scene.GetRootGameObjects().Select(go => go.transform).ToArray(), path);
                if (changes.Count != start)
                    EditorSceneManager.SaveScene(scene);
                scenes++;
            }
        }
        finally
        {
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Directory.CreateDirectory("Artifacts~/ExampleUiReview");
        var result = new
        {
            passed = true,
            scenes,
            prefabs,
            uiObjects,
            renamed = changes.Count,
            changes
        };
        File.WriteAllText("Artifacts~/ExampleUiReview/ugui-names.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return new
        {
            result.passed,
            scenes,
            prefabs,
            uiObjects,
            result.renamed
        };
    }

    private static int Rename(Transform[] siblings, string path, bool rename = true)
    {
        var used = new HashSet<string>(StringComparer.Ordinal);
        int count = 0;
        foreach (var item in siblings)
        {
            if (item is RectTransform && rename)
            {
                count++;
                string prefix = Prefix(item.gameObject);
                string role = Role(hints.TryGetValue(item.gameObject, out string hint) ? hint : item.name, prefix);
                if (prefix == "Text" && role == "Label" && item.parent != null
                    && item.parent.GetComponent<Selectable>() == null && !item.parent.name.StartsWith("Button_", StringComparison.Ordinal))
                    role = item.GetComponent<Text>().fontSize >= 24 ? "Title" : "Description";
                if (role == "Example" && (prefix == "Canvas" || prefix == "ScrollView"))
                    role = "Example";
                string name = prefix + "_" + role;
                string unique = name;
                for (int suffix = 2; !used.Add(unique); suffix++)
                    unique = name + suffix;
                if (item.name != unique)
                {
                    changes.Add(new
                    {
                        asset = path,
                        before = item.name,
                        after = unique
                    });
                    item.name = unique;
                    if (PrefabUtility.IsPartOfPrefabInstance(item))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(item.gameObject);
                    EditorUtility.SetDirty(item.gameObject);
                }
            }
            else
                used.Add(item.name);
            count += Rename(item.Cast<Transform>().ToArray(), path);
        }
        return count;
    }

    private static string Prefix(GameObject go)
    {
        if (go.GetComponent<Canvas>() != null)
            return "Canvas";
        if (go.GetComponent<Button>() != null)
            return "Button";
        if (go.GetComponents<MonoBehaviour>().Any(component => component != null && component.GetType().FullName == "StarForce.CommonButton"))
            return "Button";
        if (go.GetComponent<Toggle>() != null)
            return "Toggle";
        if (go.GetComponent<Slider>() != null)
            return "Slider";
        if (go.GetComponent<Scrollbar>() != null)
            return "Scrollbar";
        if (go.GetComponent<Dropdown>() != null)
            return "Dropdown";
        if (go.GetComponent<InputField>() != null)
            return "InputField";
        if (go.GetComponent<ScrollRect>() != null)
            return "ScrollView";
        if (go.GetComponent<Text>() != null)
            return "Text";
        if (go.GetComponent<RawImage>() != null)
            return "RawImage";
        if (go.GetComponent<Image>() != null && go.transform.childCount == 0)
            return "Image";
        return "Panel";
    }

    private static void ReadControlNames(GameObject[] roots)
    {
        hints.Clear();
        foreach (var example in roots.SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).Where(component => component != null))
        {
            if (!(layouts[example.GetType().Name] is JObject layout))
                continue;
            var data = new SerializedObject(example);
            foreach (var control in ((JArray)layout["controls"]).OfType<JObject>())
            {
                string field = (string)control["field"];
                if (field == null)
                    continue;
                var value = data.FindProperty(field)?.objectReferenceValue;
                var go = value is Component component ? component.gameObject : value as GameObject;
                if (go != null)
                {
                    string name = (string)control["objectName"];
                    // marker 字段指向轨道里的移动标记，轨道名称不能写到标记上。
                    if ((bool?)control["marker"] == true && name.EndsWith("Track", StringComparison.Ordinal))
                    {
                        hints[go.transform.parent.gameObject] = name;
                        name = "Image_Marker";
                    }
                    hints[go] = name;
                }
            }
        }
    }

    private static string Role(string original, string prefix)
    {
        string value = original.Replace("(Legacy)", "").Trim();
        foreach (string known in Prefixes)
            if (value.StartsWith(known + "_", StringComparison.Ordinal))
            {
                value = value.Substring(known.Length + 1);
                break;
            }
        foreach (string known in Prefixes.OrderByDescending(p => p.Length))
        {
            if (value.Length > known.Length && value.EndsWith(known, StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(0, value.Length - known.Length);
                break;
            }
        }
        value = string.Concat(Regex.Matches(value, "[A-Za-z0-9]+").Cast<Match>().Select(match => char.ToUpperInvariant(match.Value[0]) + match.Value.Substring(1)));
        if (value == "Label" || value == "Text")
            value = "Label";
        if (value == "VisualElement")
            value = "Track";
        if (string.IsNullOrEmpty(value) || value == prefix)
            value = prefix == "Text" ? "Label" : "Main";
        return value;
    }
}
