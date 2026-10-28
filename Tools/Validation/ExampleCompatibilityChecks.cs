using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 批处理检查导入、类型和序列化引用；游戏行为由各模块的 Play 检查覆盖。
public static class ExampleCompatibilityChecks
{
    public static void Run()
    {
        if (!Application.dataPath.Contains("Artifacts~"))
            throw new InvalidOperationException("Use an isolated project.");
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        int references = 0;
        foreach (var pair in layouts)
        {
            string typeName = "UnityQGameFramework.Examples.Modules." + pair.Key;
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(typeName)).FirstOrDefault(candidate => candidate != null);
            if (type == null)
                throw new Exception("Missing example type: " + typeName);
            string script = (string)pair.Value["script"];
            EditorSceneManager.OpenScene(Path.ChangeExtension(script, ".unity"));
            var controller = UnityEngine.Object.FindFirstObjectByType(type);
            if (controller == null)
                throw new Exception("Missing scene controller: " + script);
            var serialized = new SerializedObject(controller);
            foreach (var control in (JArray)pair.Value["controls"])
            {
                string field = (string)control["field"];
                if (field == null)
                    continue;
                if (serialized.FindProperty(field)?.objectReferenceValue == null)
                    throw new Exception("Missing serialized reference: " + pair.Key + "." + field);
                references++;
            }
        }
        foreach (string name in new[] { "StarForce.BuiltinDialogForm", "StarForce.DialogForm", "StarForce.ProcedureMenu",
            "UnityQGameFramework.Examples.PointPointPoint.PointGamePlay" })
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetType(name) != null))
                throw new Exception("Missing game example type: " + name);
        EditorSceneManager.OpenScene("Assets/Examples/02_StarForce/StarForce Launcher.unity");
        var builtinType = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("StarForce.BuiltinDataComponent")).First(type => type != null);
        var builtin = new SerializedObject(UnityEngine.Object.FindFirstObjectByType(builtinType));
        if (builtin.FindProperty("m_BuiltinDialogFormTemplate").objectReferenceValue == null)
            throw new Exception("Missing builtin dialog prefab reference.");
        var result = new
        {
            passed = true,
            unity = Application.unityVersion,
            scenes = layouts.Count,
            references,
            scope = "Compilation, scene loading and serialized references; no Play Mode or rendered UI"
        };
        Directory.CreateDirectory("Artifacts~/Compatibility");
        File.WriteAllText("Artifacts~/Compatibility/current-examples.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        Debug.Log(JsonConvert.SerializeObject(result));
    }
}
