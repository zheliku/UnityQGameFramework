using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityQGameFramework.Examples.PointPointPoint;
using UnityQGameFramework.Runtime;

public static class BuildPointGameScene
{
    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene");
        const string root = "Assets/Examples/00_PointPointPoint/GameMain";
        try
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var framework = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Packages/com.zheliku.unityqgameframework/Runtime/Prefabs/UnityQGameFramework.prefab"));
            PrefabUtility.UnpackPrefabInstance(framework, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            framework.name = "QGF";
            framework.AddComponent<PointGameResources>();
            foreach (var component in framework.GetComponentsInChildren<FrameworkComponent>(true))
                if (!(component is BaseComponent || component is ConfigComponent || component is EventComponent
                    || component is FsmComponent || component is ProcedureComponent || component is SettingComponent
                    || component is ResourceComponent || component is UIComponent || component is ObjectPoolComponent))
                    UnityEngine.Object.DestroyImmediate(component.gameObject);
            var procedures = new SerializedObject(framework.GetComponentInChildren<ProcedureComponent>());
            var types = procedures.FindProperty("m_AvailableProcedureTypeNames");
            string[] names = { typeof(PointGameLaunch).FullName, typeof(PointGameMenu).FullName,
                typeof(PointGamePlay).FullName, typeof(PointGameResult).FullName };
            types.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++) types.GetArrayElementAtIndex(i).stringValue = names[i];
            procedures.FindProperty("m_EntranceProcedureTypeName").stringValue = names[0];
            procedures.ApplyModifiedPropertiesWithoutUndo();
            var settings = new SerializedObject(framework.GetComponentInChildren<SettingComponent>());
            settings.FindProperty("m_SettingHelperTypeName").stringValue = typeof(PointGameSettingHelper).FullName;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(framework, root + "/Prefabs/QGF.prefab");
            UnityEngine.Object.DestroyImmediate(framework);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(root + "/Prefabs/QGF.prefab"));
            Directory.CreateDirectory(root + "/UI/UIForms");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            BuildForm(root + "/UI/UIForms/PointGameView.prefab");
            new GameObject("EventSystem", typeof(EventSystem), typeof(UIInputAdapter));
            var camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.Skybox;
            EditorSceneManager.SaveScene(scene, root + "/Scenes/PointPointPoint.unity");
            AssetDatabase.SaveAssets();
            return new { passed = true, scene = root + "/Scenes/PointPointPoint.unity", procedures = names };
        }
        finally
        {
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    private static void BuildForm(string path)
    {
        var form = new GameObject("PointGameView", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(PointGameView));
        form.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = form.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        scaler.matchWidthOrHeight = 0.5f;
        var page = Panel("Page", form.transform, new Vector2(0, 0), new Vector2(1, 1));
        page.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.11f, 0.15f);
        Label("Title", page, "点点点", 24, 14, 50, 30);
        var phase = Label("Phase", page, string.Empty, 24, 72, 48, 18);
        var menu = Panel("Menu", page, Vector2.zero, Vector2.one);
        var play = Panel("Play", page, Vector2.zero, Vector2.one);
        var result = Panel("Result", page, Vector2.zero, Vector2.one);
        foreach (var panel in new[] { menu, play, result })
        {
            panel.offsetMin = new Vector2(24, 24);
            panel.offsetMax = new Vector2(-24, -132);
        }
        var rules = Label("Rules", menu, string.Empty, 0, 0, 40);
        var record = Label("Record", menu, string.Empty, 0, 48, 60);
        var start = Button("Start", menu, "开始", 0, 132, new Vector2(220, 44));
        var status = Label("Status", play, string.Empty, 0, 0, 52);
        var board = Panel("Board", play, new Vector2(0, 0), Vector2.one);
        board.offsetMin = Vector2.zero;
        board.offsetMax = new Vector2(0, -64);
        board.gameObject.AddComponent<Image>().color = new Color(0.045f, 0.06f, 0.085f);
        var miss = board.gameObject.AddComponent<Button>();
        board.name = "Button_Board";
        miss.targetGraphic = board.GetComponent<Image>();
        var target = Button("Target", board, "点我", 0, 0, new Vector2(64, 64));
        var resultTitle = Label("ResultTitle", result, string.Empty, 0, 0, 50, 28);
        var resultScore = Label("ResultScore", result, string.Empty, 0, 64, 50);
        var back = Button("MenuButton", result, "返回菜单", 0, 132, new Vector2(220, 44));
        foreach (var button in form.GetComponentsInChildren<Button>(true))
        {
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }
        var serialized = new SerializedObject(form.GetComponent<PointGameView>());
        string[] names = { "menu", "play", "result", "phase", "rules", "record", "status", "resultTitle", "resultScore",
            "startButton", "targetButton", "missButton", "menuButton", "board", "target" };
        UnityEngine.Object[] values = { menu.gameObject, play.gameObject, result.gameObject, phase, rules, record, status,
            resultTitle, resultScore, start, target, miss, back, board, target.transform };
        for (int i = 0; i < names.Length; i++) serialized.FindProperty(names[i]).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(form, path);
        UnityEngine.Object.DestroyImmediate(form);
    }

    private static RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = new GameObject("Panel_" + name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Text Label(string name, Transform parent, string caption, float x, float y, float height, int size = 18)
    {
        var rect = Panel(name, parent, new Vector2(0, 1), Vector2.one);
        rect.pivot = new Vector2(0, 1);
        rect.offsetMin = new Vector2(x, -y - height);
        rect.offsetMax = new Vector2(-24, -y);
        var label = rect.gameObject.AddComponent<Text>();
        rect.name = "Text_" + name;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.text = caption;
        label.color = new Color(0.9f, 0.92f, 0.96f);
        label.supportRichText = false;
        label.raycastTarget = false;
        return label;
    }

    private static Button Button(string name, Transform parent, string caption, float x, float y, Vector2 size)
    {
        var rect = Panel(name, parent, new Vector2(0, 1), new Vector2(0, 1));
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(x, -y);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.32f, 0.5f);
        var button = rect.gameObject.AddComponent<Button>();
        rect.name = "Button_" + name.Replace("Button", string.Empty);
        button.targetGraphic = image;
        var label = Label("Text", rect, caption, 0, 0, size.y);
        label.alignment = TextAnchor.MiddleCenter;
        ((RectTransform)label.transform).offsetMax = Vector2.zero;
        return button;
    }
}
