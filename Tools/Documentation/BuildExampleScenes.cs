using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Image = UnityEngine.UI.Image;
using Newtonsoft.Json.Linq;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.Modules;

public static class BuildExampleScenes
{
    private const string Root = "Assets/Examples";

    // Development-only authoring tool. Consumers receive already saved scenes.
    // After rebuilding, run StyleExampleUi.Run and NormalizeUguiNames.Run in the authoring project.
    public static object Run() => RunModules(null);

    public static object RunModules(string[] folders) => RunSelected(folders, null);

    public static object RunExamples(string[] names) => RunSelected(null, names);

    private static object RunSelected(string[] folders, string[] names)
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save the current scenes first.");
        int count = 0;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Packages/com.zheliku.unityqgameframework/Runtime/Prefabs/UnityQGameFramework.prefab");
            SerializedObject serialized;
            var definitions = JArray.Parse(File.ReadAllText("Tools/Documentation/examples-layout.json"));
            var moduleRoots = definitions
                .Select(entry => (string)entry["folder"])
                .Where(folder => folders == null || folders.Contains(folder))
                .Select(folder => Root + "/" + folder).ToArray();
            var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
            var examples = AssetDatabase.FindAssets("t:MonoScript", moduleRoots)
                .Select(guid => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid)).GetClass())
                .Where(t => t != null && t.BaseType == typeof(MonoBehaviour) && layouts[t.Name] != null)
                .Where(t => names == null || names.Contains(t.Name))
                .OrderBy(t => t.FullName).ToArray();
            foreach (var type in examples)
            {
                var script = AssetDatabase.FindAssets(type.Name + " t:MonoScript", new[] { Root })
                    .Select(AssetDatabase.GUIDToAssetPath).Single(p => Path.GetFileNameWithoutExtension(p) == type.Name);
                string folder = Path.GetDirectoryName(script).Replace('\\', '/');
                string moduleRoot = moduleRoots.Single(root => script.StartsWith(root + "/", StringComparison.Ordinal));
                string frameworkPath = Root + "/01_Modules/Common/Prefabs/QGF.prefab";
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                // ClearAll 的独立实验不启动其他模块，避免它们持有的池对象使清理始终不可用。
                var sceneFramework = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(frameworkPath));
                if (sceneFramework != null)
                {
                    var configuration = JArray.Parse(File.ReadAllText("Tools/Documentation/examples-layout.json"))
                        .Single(entry => Root + "/" + (string)entry["folder"] == moduleRoot);
                    var modules = configuration["frameworkModules"].Values<string>().ToArray();
                    foreach (Transform child in sceneFramework.transform.Cast<Transform>().ToArray())
                        if (!modules.Contains(child.name))
                            UnityEngine.Object.DestroyImmediate(child.gameObject);
                    // 资源模块随 frameworkModules 保留或移除；运行模式来自 Sample 的 ResourceSettings。
                    if ((int?)layouts[type.Name]["downloadAgentCount"] > 0)
                    {
                        var download = new SerializedObject(sceneFramework.GetComponentInChildren<DownloadComponent>());
                        download.FindProperty("m_DownloadAgentHelperCount").intValue = (int)layouts[type.Name]["downloadAgentCount"];
                        download.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                if ((bool?)layouts[type.Name]["entitySound"] == true)
                {
                    // Only the entity-follow scene starts Entity; the other Sound examples keep their smaller framework.
                    UnityEngine.Object.Instantiate(source.GetComponentInChildren<EntityComponent>().gameObject, sceneFramework.transform);
                }
                var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
                camera.tag = "MainCamera";
                float cameraX = (float?)layouts[type.Name]["cameraX"] ?? 0f;
                camera.transform.position = new Vector3(cameraX, 0, -10);
                camera.orthographic = true;
                camera.orthographicSize = 6;
                camera.clearFlags = CameraClearFlags.Skybox;
                if (moduleRoot.EndsWith("_ObjectPool", StringComparison.Ordinal))
                {
                    var light = new GameObject("Directional Light", typeof(Light)).GetComponent<Light>();
                    light.type = LightType.Directional;
                    light.intensity = 1.2f;
                    light.transform.rotation = Quaternion.Euler(50, -30, 0);
                }
                var example = new GameObject(type.Name).AddComponent(type);
                BuildPanel(example, (JObject)layouts[type.Name], moduleRoot);
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                string[] states = type == typeof(ProcedureEntryExample) ? new[] { "ExampleLaunch01" }
                    : type == typeof(ChangeProcedureExample) ? new[] { "ExampleLaunch02", "ExampleMenu02", "ExamplePlay02" }
                    : type == typeof(ProcedureDataExample) ? new[] { "ExampleSelect03", "ExampleLoad03" }
                    : null;
                if (states != null)
                {
                    var child = UnityEngine.Object.Instantiate(source.GetComponentInChildren<ProcedureComponent>().gameObject, sceneFramework.transform);
                    var procedures = new SerializedObject(child.GetComponent<ProcedureComponent>());
                    var procedureTypes = procedures.FindProperty("m_AvailableProcedureTypeNames");
                    procedureTypes.arraySize = states.Length;
                    for (int index = 0; index < states.Length; index++)
                        procedureTypes.GetArrayElementAtIndex(index).stringValue = type.Namespace + "." + states[index];
                    procedures.FindProperty("m_EntranceProcedureTypeName").stringValue = type.Namespace + "." + states[0];
                    procedures.ApplyModifiedPropertiesWithoutUndo();
                }
                string sharedHelper = (string)definitions.Single(entry => Root + "/" + (string)entry["folder"] == moduleRoot)["settingHelperType"];
                var helperType = type.Assembly.GetType(sharedHelper ?? type.Namespace + "." + type.Name + "SettingHelper");
                if (helperType != null)
                {
                    var child = UnityEngine.Object.Instantiate(source.GetComponentInChildren<SettingComponent>().gameObject, sceneFramework.transform);
                    var settings = new SerializedObject(child.GetComponent<SettingComponent>());
                    settings.FindProperty("m_SettingHelperTypeName").stringValue = helperType.FullName;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
                if (moduleRoot.EndsWith("_Debugger", StringComparison.Ordinal))
                {
                    var child = UnityEngine.Object.Instantiate(source.GetComponentInChildren<DebuggerComponent>().gameObject, sceneFramework.transform);
                    var debugger = new SerializedObject(child.GetComponent<DebuggerComponent>());
                    debugger.FindProperty("m_ActiveWindow").intValue = (int)DebuggerActiveWindowType.AlwaysClose;
                    debugger.ApplyModifiedPropertiesWithoutUndo();
                }
                string sharedCatalog = (string)layouts[type.Name]["catalogGuid"];
                string catalogPath = sharedCatalog == null ? folder + "/Config/Data/Catalog.asset"
                    : AssetDatabase.GUIDToAssetPath(sharedCatalog);
                if (File.Exists(catalogPath))
                {
                    var tables = new SerializedObject(sceneFramework.GetComponentInChildren<DataTableComponent>());
                    tables.FindProperty("m_LubanCatalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LubanDataTableCatalog>(catalogPath);
                    tables.ApplyModifiedPropertiesWithoutUndo();
                }
                serialized = new SerializedObject(example);
                var invalidCatalog = serialized.FindProperty("invalidCatalog");
                if (invalidCatalog != null)
                    invalidCatalog.objectReferenceValue = AssetDatabase.LoadAssetAtPath<LubanDataTableCatalog>(
                        AssetDatabase.GUIDToAssetPath((string)layouts[type.Name]["invalidCatalogGuid"]));
                if (serialized.FindProperty("sceneCamera") != null)
                    serialized.FindProperty("sceneCamera").objectReferenceValue = camera;
                string artPath = folder + "/Art";
                if (serialized.FindProperty("resourceComponent") != null)
                    serialized.FindProperty("resourceComponent").objectReferenceValue = sceneFramework.GetComponentInChildren<ResourceComponent>(true);
                if (serialized.FindProperty("cubePrefab") != null)
                {
                    // 对象池示例共享一个可实例化 Cube 资产；Target 由各示例代码创建与释放。
                    string art = moduleRoot + "/Art";
                    EnsureArtFolder(art);
                    string materialPath = art + "/Cube.mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material == null)
                    {
                        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        material.SetColor("_BaseColor", new Color(.12f, .65f, 1f));
                        AssetDatabase.CreateAsset(material, materialPath);
                    }
                    material.shader = Shader.Find("Universal Render Pipeline/Lit");
                    EditorUtility.SetDirty(material);
                    string path = art + "/Cube.prefab";
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null)
                    {
                        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.transform.localScale = Vector3.one * .8f;
                        cube.GetComponent<Renderer>().sharedMaterial = material;
                        prefab = PrefabUtility.SaveAsPrefabAsset(cube, path);
                        UnityEngine.Object.DestroyImmediate(cube);
                    }
                    serialized.FindProperty("cubePrefab").objectReferenceValue = prefab;
                }
                if (serialized.FindProperty("windowGuid") != null)
                {
                    EnsureArtFolder(artPath);
                    string path = CreateWindow(artPath, type.Assembly.GetType(type.Namespace + "." + type.Name + "View"));
                    serialized.FindProperty("windowGuid").stringValue = AssetDatabase.AssetPathToGUID(path);
                }
                if (serialized.FindProperty("soundGuid") != null)
                    serialized.FindProperty("soundGuid").stringValue = AssetDatabase.AssetPathToGUID(artPath + "/Tone.wav");
                if (serialized.FindProperty("roomGuid") != null || serialized.FindProperty("blueRoomGuid") != null)
                {
                    // Unity only permits additive scene authoring after the
                    // current scene has a saved path. Save again after binding.
                    EditorSceneManager.SaveScene(scene, folder + "/" + type.Name + ".unity");
                    EnsureArtFolder(artPath);
                    bool compareRooms = (bool?)layouts[type.Name]["compareRooms"] == true;
                    string blue = CreateRoom(artPath, "BlueRoom", new Color(0.12f, 0.65f, 1f), compareRooms ? 2.5f : 3f);
                    var room = serialized.FindProperty("roomGuid");
                    if (room != null)
                        room.stringValue = AssetDatabase.AssetPathToGUID(blue);
                    else
                    {
                        serialized.FindProperty("blueRoomGuid").stringValue = AssetDatabase.AssetPathToGUID(blue);
                        serialized.FindProperty("orangeRoomGuid").stringValue = AssetDatabase.AssetPathToGUID(
                            CreateRoom(artPath, "OrangeRoom", new Color(1f, 0.45f, 0.12f), compareRooms ? 4.5f : 3f));
                    }
                    if (serialized.FindProperty("markerMaterial") != null)
                        serialized.FindProperty("markerMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(artPath + "/BlueRoom.mat");
                }
                var prefabGuid = serialized.FindProperty("prefabGuid");
                if (prefabGuid != null && layouts[type.Name]["prefabGuid"] != null)
                    prefabGuid.stringValue = (string)layouts[type.Name]["prefabGuid"];
                else if (prefabGuid != null)
                {
                    string art = folder + "/Art";
                    Directory.CreateDirectory(art);
                    AssetDatabase.Refresh();
                    string materialPath = art + "/Cube.mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material == null)
                    {
                        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                        material = new Material(shader);
                        if (material.HasProperty("_BaseColor"))
                            material.SetColor("_BaseColor", new Color(0.12f, 0.65f, 1f));
                        if (material.HasProperty("_Color"))
                            material.SetColor("_Color", new Color(0.12f, 0.65f, 1f));
                        AssetDatabase.CreateAsset(material, materialPath);
                    }
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.GetComponent<Renderer>().sharedMaterial = material;
                    string prefabPath = art + "/Cube.prefab";
                    PrefabUtility.SaveAsPrefabAsset(cube, prefabPath);
                    UnityEngine.Object.DestroyImmediate(cube);
                    prefabGuid.stringValue = AssetDatabase.AssetPathToGUID(prefabPath);
                }
                if ((bool?)layouts[type.Name]["spatialSound"] == true)
                    CreateSpatialSoundMarkers(camera, serialized, artPath);
                foreach (string field in new[] { "config", "dictionary", "invalidDictionary" })
                {
                    var property = serialized.FindProperty(field);
                    if (property == null)
                        continue;
                    string sharedGuid = (string)layouts[type.Name][field + "Guid"];
                    string asset = sharedGuid != null ? AssetDatabase.GUIDToAssetPath(sharedGuid)
                        : AssetDatabase.FindAssets("t:TextAsset", new[] { folder })
                            .Select(AssetDatabase.GUIDToAssetPath).Single(p => p.EndsWith(".toml"));
                    property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(asset);
                }
                foreach (var entry in new[] { new[] { "json", "Player.json" }, new[] { "response", "status.json" }, new[] { "payload", "Payload.txt" } })
                {
                    var property = serialized.FindProperty(entry[0]);
                    if (property == null)
                        continue;
                    if (entry[0] == "payload" && layouts[type.Name]["payloadGuid"] != null)
                    {
                        string shared = AssetDatabase.GUIDToAssetPath((string)layouts[type.Name]["payloadGuid"]);
                        property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(shared);
                        if (property.objectReferenceValue == null)
                            throw new Exception("Missing shared payload: " + shared);
                        continue;
                    }
                    string asset = folder + "/" + entry[1];
                    if (!File.Exists(asset) && entry[0] == "payload")
                        asset = folder + "/Payload.bytes";
                    property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(asset);
                    if (property.objectReferenceValue == null)
                        throw new Exception("Missing example asset: " + asset);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if ((bool?)definitions.Single(entry => Root + "/" + (string)entry["folder"] == moduleRoot)["responsiveLayout"] == true)
                {
                    var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Single(item => item.name == "Canvas_Example");
                    var panel = canvas.GetComponentInChildren<ScrollRect>().GetComponent<RectTransform>();
                    if (panel.anchorMax.x < .75f)
                    {
                        var scroll = panel.GetComponent<ScrollRect>();
                        var barObject = DefaultControls.CreateScrollbar(new DefaultControls.Resources());
                        barObject.name = "Scrollbar_Controls";
                        barObject.transform.SetParent(panel, false);
                        var barRect = (RectTransform)barObject.transform;
                        barRect.anchorMin = new Vector2(1, 0);
                        barRect.anchorMax = Vector2.one;
                        barRect.offsetMin = new Vector2(-14, 12);
                        barRect.offsetMax = new Vector2(-6, -12);
                        barObject.transform.GetChild(0).name = "Panel_SlidingArea";
                        barObject.GetComponent<Image>().color = new Color(.12f, .24f, .32f, .4f);
                        var bar = barObject.GetComponent<Scrollbar>();
                        bar.direction = Scrollbar.Direction.BottomToTop;
                        bar.handleRect.name = "Image_Handle";
                        bar.targetGraphic.color = new Color(.65f, .9f, .96f, .8f);
                        scroll.verticalScrollbar = bar;
                        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                    }
                }
                EditorSceneManager.SaveScene(scene, folder + "/" + type.Name + ".unity");
                count++;
            }
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        return new
        {
            scenes = count,
            independentFrameworks = true
        };
    }

    private static void ReplaceLegacyLabels(GameObject root)
    {
        foreach (UnityEngine.UI.Text legacy in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        {
            GameObject owner = legacy.gameObject;
            UnityEngine.Object.DestroyImmediate(legacy);
            owner.AddComponent<TextMeshProUGUI>();
        }
    }

    private static TMP_DefaultControls.Resources TmpResources(DefaultControls.Resources resources)
    {
        return new TMP_DefaultControls.Resources
        {
            standard = resources.standard,
            background = resources.background,
            checkmark = resources.checkmark,
            knob = resources.knob,
            dropdown = resources.dropdown,
            mask = resources.mask
        };
    }

    private static void BuildPanel(Component example, JObject layout, string moduleRoot)
    {
        if (layout == null)
            throw new Exception("Missing uGUI layout: " + example.name);
        var canvas = new GameObject("Canvas_Example", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 640);
        scaler.matchWidthOrHeight = 0.5f;
        var viewport = new GameObject("ScrollView_Example", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        viewport.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)viewport.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = (bool?)layout["fullWidthPanel"] != true && ((bool?)layout["spatialSound"] == true || moduleRoot.EndsWith("_ObjectPool") || moduleRoot.EndsWith("_Resource") || moduleRoot.EndsWith("_Entity") || moduleRoot.EndsWith("_UI") || moduleRoot.EndsWith("_Scene") || moduleRoot.EndsWith("_Debugger"))
            ? new Vector2(0.5f, 1) : Vector2.one;
        rect.offsetMin = new Vector2(24, 24);
        rect.offsetMax = new Vector2(-24, -24);
        viewport.GetComponent<Image>().color = new Color(0.09f, 0.11f, 0.15f, 0.98f);
        var content = new GameObject("Panel_Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = Vector2.zero;
        var column = content.GetComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(18, 18, 18, 18);
        column.spacing = 10;
        column.childControlHeight = column.childControlWidth = true;
        column.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = rect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28;
        var serialized = new SerializedObject(example);
        var resources = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Examples/01_Modules/10_Localization/Common/ChineseFont.asset");
        var rows = new Dictionary<string, Transform>();
        Transform[] columns = null;
        foreach (JObject control in (JArray)layout["controls"])
        {
            string kind = (string)control["type"];
            GameObject go;
            Component value;
            switch (kind)
            {
                case "Button":
                    go = TMP_DefaultControls.CreateButton(TmpResources(resources));
                    value = go.GetComponent<Button>();
                    break;
                case "Toggle":
                    go = DefaultControls.CreateToggle(resources);
                    ReplaceLegacyLabels(go);
                    value = go.GetComponent<Toggle>();
                    break;
                case "Slider":
                    go = DefaultControls.CreateSlider(resources);
                    value = go.GetComponent<Slider>();
                    break;
                case "InputField":
                    go = TMP_DefaultControls.CreateInputField(TmpResources(resources));
                    value = go.GetComponent<TMP_InputField>();
                    break;
                case "Dropdown":
                    go = TMP_DefaultControls.CreateDropdown(TmpResources(resources));
                    value = go.GetComponent<TMP_Dropdown>();
                    break;
                case "Image":
                    go = new GameObject("Image", typeof(RectTransform), typeof(Image));
                    value = (bool?)control["marker"] == true ? (Component)go.GetComponent<RectTransform>() : go.GetComponent<Image>();
                    break;
                case "Text":
                    go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    value = go.GetComponent<TMP_Text>();
                    break;
                default:
                    throw new Exception("Unknown uGUI control: " + kind);
            }
            go.name = (string)control["objectName"];
            Transform parent = content.transform;
            int columnNumber = (int?)control["column"] ?? 0;
            if (columnNumber > 0)
            {
                if (columns == null)
                {
                    var body = new GameObject("Panel_Columns", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    body.transform.SetParent(content.transform, false);
                    var horizontal = body.GetComponent<HorizontalLayoutGroup>();
                    horizontal.spacing = 24;
                    horizontal.childControlWidth = horizontal.childControlHeight = true;
                    horizontal.childForceExpandWidth = true;
                    horizontal.childForceExpandHeight = false;
                    columns = new Transform[2];
                    for (int index = 0; index < columns.Length; index++)
                    {
                        var panel = new GameObject(index == 0 ? "Panel_Controls" : "Panel_Results",
                            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
                        panel.transform.SetParent(body.transform, false);
                        var vertical = panel.GetComponent<VerticalLayoutGroup>();
                        vertical.spacing = 8;
                        vertical.childControlWidth = vertical.childControlHeight = true;
                        vertical.childForceExpandHeight = false;
                        panel.GetComponent<LayoutElement>().flexibleWidth = 1;
                        panel.GetComponent<LayoutElement>().preferredWidth = 0;
                        columns[index] = panel.transform;
                    }
                }
                parent = columns[columnNumber - 1];
            }
            string rowName = (string)control["row"];
            if (rowName != null)
            {
                string rowKey = columnNumber + "/" + rowName;
                Transform rowParent = parent;
                if (!rows.TryGetValue(rowKey, out parent))
                {
                    var row = new GameObject("Panel_" + rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                    row.transform.SetParent(rowParent, false);
                    var horizontal = row.GetComponent<HorizontalLayoutGroup>();
                    horizontal.spacing = 10;
                    horizontal.childControlWidth = horizontal.childControlHeight = true;
                    horizontal.childForceExpandWidth = true;
                    horizontal.childForceExpandHeight = false;
                    row.GetComponent<LayoutElement>().preferredHeight = 38;
                    rows.Add(rowKey, parent = row.transform);
                }
            }
            go.transform.SetParent(parent, false);
            if ((bool?)control["overlay"] == true)
            {
                var panel = new GameObject("Panel_Status", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(canvas.transform, false);
                var panelRect = (RectTransform)panel.transform;
                panelRect.anchorMin = new Vector2(.54f, .72f);
                panelRect.anchorMax = new Vector2(.97f, .94f);
                panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
                panel.GetComponent<Image>().color = new Color(.025f, .055f, .08f, .42f);
                panel.GetComponent<Image>().raycastTarget = false;
                go.transform.SetParent(panel.transform, false);
                var statusRect = (RectTransform)go.transform;
                statusRect.anchorMin = Vector2.zero;
                statusRect.anchorMax = Vector2.one;
                statusRect.offsetMin = new Vector2(12, 12);
                statusRect.offsetMax = new Vector2(-12, -12);
            }
            foreach (var image in go.GetComponentsInChildren<Image>(true))
                image.color = new Color(0.16f, 0.23f, 0.32f);
            foreach (var label in go.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = font;
                label.fontSize = 18;
                label.color = new Color(0.9f, 0.92f, 0.96f);
                label.richText = false;
            }
            var element = go.AddComponent<LayoutElement>();
            if (rowName != null)
                element.flexibleWidth = 1;
            element.minHeight = kind == "Text" ? 28 : 38;
            if (kind != "Text")
                element.preferredHeight = element.minHeight;
            if (control["height"] != null)
                element.preferredHeight = (float)control["height"];
            string caption = (string)control["text"] ?? (string)control["label"] ?? string.Empty;
            if (kind == "Text")
            {
                var label = (TMP_Text)value;
                label.text = caption;
                label.raycastTarget = false;
                label.overflowMode = TextOverflowModes.Overflow;
                if ((bool?)control["title"] == true)
                {
                    label.fontSize = 28;
                    element.minHeight = 42;
                }
            }
            else if (kind == "Button")
                go.GetComponentInChildren<TMP_Text>().text = caption;
            else if (kind == "Toggle")
            {
                var toggle = (Toggle)value;
                toggle.isOn = (bool?)control["value"] ?? false;
                go.GetComponentInChildren<TMP_Text>().text = caption;
                toggle.graphic.color = new Color(0.25f, 0.65f, 1f);
            }
            else if (kind == "InputField")
            {
                var input = (TMP_InputField)value;
                input.text = (string)control["value"] ?? string.Empty;
                input.placeholder.GetComponent<TMP_Text>().text = (string)control["label"] ?? "Enter Text";
                if ((string)control["contentType"] == "IntegerNumber")
                    input.contentType = TMP_InputField.ContentType.IntegerNumber;
                if ((string)control["multiline"] == "true")
                {
                    input.lineType = TMP_InputField.LineType.MultiLineNewline;
                    element.preferredHeight = 110;
                    if (control["height"] != null)
                        element.preferredHeight = (float)control["height"];
                }
                input.customCaretColor = true;
                input.caretColor = Color.white;
            }
            else if (kind == "Slider")
            {
                var slider = (Slider)value;
                slider.minValue = (float?)control["low-value"] ?? 0;
                slider.maxValue = (float?)control["high-value"] ?? 1;
                slider.value = (float?)control["value"] ?? 0;
                slider.fillRect.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.9f);
                if ((bool?)control["interactable"] == false)
                {
                    slider.interactable = false;
                    slider.handleRect.gameObject.SetActive(false);
                }
            }
            else if (kind == "Dropdown")
            {
                var dropdown = (TMP_Dropdown)value;
                if (control["choices"] != null)
                {
                    dropdown.ClearOptions();
                    dropdown.AddOptions(((string)control["choices"]).Split('|').ToList());
                }
                var item = (RectTransform)dropdown.itemText.GetComponentInParent<Toggle>(true).transform;
                item.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40);
                dropdown.itemText.lineSpacing = 1;
                dropdown.itemText.overflowMode = TextOverflowModes.Overflow;
            }
            else if (kind == "Image" && (bool?)control["marker"] == true)
            {
                var track = go;
                var marker = new GameObject("Image_Marker", typeof(RectTransform), typeof(Image));
                marker.transform.SetParent(track.transform, false);
                var markerRect = (RectTransform)marker.transform;
                markerRect.anchorMin = markerRect.anchorMax = new Vector2(0, 0.5f);
                markerRect.pivot = new Vector2(0, 0.5f);
                markerRect.sizeDelta = new Vector2(18, 18);
                marker.GetComponent<Image>().color = new Color(0.25f, 0.65f, 1f);
                marker.GetComponent<Image>().raycastTarget = false;
                element.preferredHeight = 24;
                value = markerRect;
            }
            if (value is Selectable selectable && !(value is TMP_InputField))
            {
                var navigation = selectable.navigation;
                navigation.mode = Navigation.Mode.None;
                selectable.navigation = navigation;
            }
            string field = (string)control["field"];
            string localizationKey = (string)control["localizationKey"];
            if (!string.IsNullOrEmpty(localizationKey))
            {
                LocalizeComponent binding = kind == "Image" ? (LocalizeComponent)go.AddComponent<LocalizeImage>() : go.AddComponent<LocalizeText>();
                var localized = new SerializedObject(binding);
                localized.FindProperty("m_Key").stringValue = localizationKey;
                if (localized.FindProperty("m_FontKey") != null)
                    localized.FindProperty("m_FontKey").stringValue = (string)control["fontKey"] ?? string.Empty;
                localized.ApplyModifiedPropertiesWithoutUndo();
                value = binding;
            }
            if (field != null)
            {
                var property = serialized.FindProperty(field);
                if (property == null)
                    throw new Exception(example.name + " has no field " + field);
                property.objectReferenceValue = value;
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureArtFolder(string path)
    {
        Directory.CreateDirectory(path);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void CreateSpatialSoundMarkers(Camera camera, SerializedObject example, string art)
    {
        EnsureArtFolder(art);
        UnityEngine.Object.DestroyImmediate(camera.GetComponent<AudioListener>());
        foreach (string name in new[] { "Listener", "Source" })
        {
            var property = example.FindProperty(name == "Listener" ? "listenerMarker" : "sourceMarker");
            if (property == null)
                continue;
            bool listener = name == "Listener";
            var marker = GameObject.CreatePrimitive(listener ? PrimitiveType.Sphere : PrimitiveType.Cube);
            marker.name = name;
            marker.transform.position = new Vector3(listener ? 4 : 7, 0, 0);
            marker.transform.localScale = Vector3.one * 0.6f;
            if (listener)
                marker.AddComponent<AudioListener>();
            string path = art + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                var color = listener ? new Color(1, .65f, .18f) : new Color(.12f, .65f, 1);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", color);
                AssetDatabase.CreateAsset(material, path);
            }
            marker.GetComponent<Renderer>().sharedMaterial = material;
            property.objectReferenceValue = marker.transform;
        }
    }

    private static string CreateWindow(string art, Type viewType)
    {
        var root = new GameObject("Window", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), viewType);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 640);
        scaler.matchWidthOrHeight = 0.5f;
        var panel = new GameObject("Panel_Window", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.77f, 0.5f);
        rect.sizeDelta = new Vector2(240, 180);
        panel.GetComponent<Image>().color = new Color(0.07f, 0.32f, 0.55f, 0.98f);
        var label = new GameObject("Text_Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(panel.transform, false);
        rect = (RectTransform)label.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12, 12);
        rect.offsetMax = new Vector2(-12, -12);
        var text = label.GetComponent<TMP_Text>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Examples/01_Modules/10_Localization/Common/ChineseFont.asset");
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Center;
        text.text = "Window";
        var data = new SerializedObject(root.GetComponent(viewType));
        data.FindProperty("title").objectReferenceValue = text;
        data.ApplyModifiedPropertiesWithoutUndo();
        string path = art + "/Window.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return path;
    }

    private static string CreateRoom(string art, string name, Color color, float x = 3f)
    {
        var room = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cube, room);
        cube.name = name + " Cube";
        cube.transform.position = new Vector3(x, 0, 0);
        cube.transform.rotation = Quaternion.Euler(20, 35, 0);
        string materialPath = art + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        cube.GetComponent<Renderer>().sharedMaterial = material;
        string path = art + "/" + name + ".unity";
        EditorSceneManager.SaveScene(room, path);
        EditorSceneManager.CloseScene(room, true);
        return path;
    }
}
