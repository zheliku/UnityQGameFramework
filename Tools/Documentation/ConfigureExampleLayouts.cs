using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Examples.Modules.Presentation;

// 为已有场景补充布局组件，不重建场景中的业务引用。
public static class ConfigureExampleLayouts
{
    public static object Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Use the isolated authoring project in Edit Mode.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (setup.Any(item => item.isLoaded && UnityEngine.SceneManagement.SceneManager.GetSceneByPath(item.path).isDirty))
            throw new InvalidOperationException("Save scenes first.");
        var definitions = JArray.Parse(File.ReadAllText("Tools/Documentation/examples-layout.json"));
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var changed = new List<string>();
        try
        {
            foreach (var entry in layouts.Properties())
            {
                string script = (string)entry.Value["script"];
                string module = "01_Modules/" + script.Split('/')[3];
                if ((bool?)definitions.Single(item => (string)item["folder"] == module)["responsiveLayout"] != true) continue;
                string path = script.Substring(0, script.Length - 3) + ".unity";
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Single(item => item.name == "Canvas_Example");
                var panel = canvas.GetComponentInChildren<ScrollRect>().GetComponent<RectTransform>();
                if (panel.anchorMax.x >= .75f) continue;
                AddScrollbar(panel.GetComponent<ScrollRect>());
                var component = canvas.GetComponent<ExampleSceneLayout>() ?? canvas.gameObject.AddComponent<ExampleSceneLayout>();
                var data = new SerializedObject(component);
                data.FindProperty("controls").objectReferenceValue = panel;
                data.FindProperty("legend").objectReferenceValue = canvas.transform.Find("Panel_Status");
                data.FindProperty("sceneCamera").objectReferenceValue = module.EndsWith("/14_UI", StringComparison.Ordinal) ? null : Camera.main;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene);
                changed.Add(path);
                if (!module.EndsWith("/14_UI", StringComparison.Ordinal)) continue;
                string prefab = Path.GetDirectoryName(script).Replace('\\', '/') + "/Art/Window.prefab";
                var root = PrefabUtility.LoadPrefabContents(prefab);
                try
                {
                    var window = root.GetComponent<ExampleSceneLayout>() ?? root.AddComponent<ExampleSceneLayout>();
                    var fields = new SerializedObject(window);
                    fields.FindProperty("window").objectReferenceValue = root.transform.Find("Panel_Window");
                    fields.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, prefab);
                    changed.Add(prefab);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        finally
        {
            if (setup.Any(item => item.isActive && item.isLoaded && !string.IsNullOrEmpty(item.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Directory.CreateDirectory("Artifacts~/ResponsiveExamples");
        File.WriteAllText("Artifacts~/ResponsiveExamples/configured.json", Newtonsoft.Json.JsonConvert.SerializeObject(changed));
        return new { passed = true, assets = changed.Count, changed };
    }

    private static void AddScrollbar(ScrollRect scroll)
    {
        if (scroll.verticalScrollbar != null) return;
        var root = DefaultControls.CreateScrollbar(new DefaultControls.Resources());
        root.name = "Scrollbar_Controls";
        root.transform.SetParent(scroll.transform, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-14, 12);
        rect.offsetMax = new Vector2(-6, -12);
        root.transform.GetChild(0).name = "Panel_SlidingArea";
        root.GetComponent<Image>().color = new Color(.12f, .24f, .32f, .4f);
        var bar = root.GetComponent<Scrollbar>();
        bar.direction = Scrollbar.Direction.BottomToTop;
        bar.handleRect.name = "Image_Handle";
        bar.targetGraphic.color = new Color(.65f, .9f, .96f, .8f);
        scroll.verticalScrollbar = bar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }
}
