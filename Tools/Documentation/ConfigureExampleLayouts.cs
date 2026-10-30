using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 将布局直接保存到场景，不添加运行时展示组件。
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
                if ((bool?)definitions.Single(item => (string)item["folder"] == module)["responsiveLayout"] != true)
                    continue;
                string path = script.Substring(0, script.Length - 3) + ".unity";
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Single(item => item.name == "Canvas_Example");
                var panel = canvas.GetComponentInChildren<ScrollRect>().GetComponent<RectTransform>();
                if (panel.anchorMax.x >= .75f)
                    continue;
                AddScrollbar(panel.GetComponent<ScrollRect>());
                panel.anchorMin = new Vector2(.025f, .05f);
                panel.anchorMax = new Vector2(.475f, .95f);
                panel.offsetMin = panel.offsetMax = Vector2.zero;
                EditorSceneManager.SaveScene(scene);
                changed.Add(path);
            }
        }
        finally
        {
            if (setup.Any(item => item.isActive && item.isLoaded && !string.IsNullOrEmpty(item.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Directory.CreateDirectory("Artifacts~/ResponsiveExamples");
        File.WriteAllText("Artifacts~/ResponsiveExamples/configured.json", Newtonsoft.Json.JsonConvert.SerializeObject(changed));
        return new
        {
            passed = true,
            assets = changed.Count,
            changed
        };
    }

    private static void AddScrollbar(ScrollRect scroll)
    {
        if (scroll.verticalScrollbar != null)
            return;
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
