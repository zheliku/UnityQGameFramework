// Run in a normal (not batch-mode) isolated Unity 6 URP consumer with a QGF Git dependency.
// Also copy SampleVisualCapture.cs to the consumer's Assets/Validation folder.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class SampleLifecycleValidation
{
    private const string Package = "com.zheliku.unityqgameframework";
    private static string PackageVersion => UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Single(p => p.name == Package).version;
    private const string StatePath = "Results/sample-stage.json";
    private const string Workspace = "Assets/UnityQGameFramework/Config";
    private static double s_Started = EditorApplication.timeSinceStartup;
    private static State s_State;
    private static int s_Errors;
    private static SampleVisualCapture s_Capture;

    [Serializable] private sealed class State
    {
        public string stage;
        public string sample;
        public string[] settings;
        public string[] hashes;
        public bool automaticWorkspace, repairedSchemas, importedFromPackage, menuOpened, battleStarted;
        public bool menuVisible, battleVisible;
        public bool urpShaders, hpBarShown, hpBarRecycled, hpBarReused, reversedStartOrder;
        public bool sampleRemoved, packageRemoved, hostSettingsUnchanged;
        public int materialCount;
        public int tiffTextures;
        public float damageTime;
        public string failure;
    }

    static SampleLifecycleValidation()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += (_, __, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) ++s_Errors; };
    }

    public static void Run()
    {
        Directory.CreateDirectory("Results");
        Require(!Application.isBatchMode, "Visual validation requires a normal Editor and a rendered Game view");
        ShaderUtil.allowAsyncCompilation = false;
        // The host chooses URP before importing the sample; the sample must not change it.
        Directory.CreateDirectory("Assets/HostPipeline");
        AssetDatabase.Refresh();
        var pipelineType = Type("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset");
        Require(pipelineType != null, "Install a compatible URP package in the validation host");
        var renderer = pipelineType.GetMethod("CreateRendererAsset", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { "Assets/HostPipeline/Universal.asset", 1, true, "Renderer" });
        var pipeline = (RenderPipelineAsset)pipelineType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new[] { renderer });
        AssetDatabase.CreateAsset(pipeline, "Assets/HostPipeline/Universal.asset");
        GraphicsSettings.defaultRenderPipeline = pipeline;
        AssetDatabase.SaveAssets();
        s_State = new State { stage = "install" };
        Save();
    }

    private static System.Type Type(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);
    private static object Call(string type, string method, params object[] args) => Type(type).GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
    private static string Hash(string path) { using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path))); }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Save() => File.WriteAllText(StatePath, JsonUtility.ToJson(s_State, true));
    private static void Stage(string stage) { s_State.stage = stage; s_Started = EditorApplication.timeSinceStartup; Save(); }
    private static void CheckSettings()
    {
        for (int i = 0; i < s_State.settings.Length; ++i)
            Require(Hash(s_State.settings[i]) == s_State.hashes[i], "Host settings changed: " + s_State.settings[i]);
    }

    private static void Tick()
    {
        if (s_State == null)
        {
            if (!File.Exists(StatePath)) return;
            s_State = JsonUtility.FromJson<State>(File.ReadAllText(StatePath));
        }
        if (s_State.stage == "done" || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try
        {
            Require(EditorApplication.timeSinceStartup - s_Started < 180, "Timed out in " + s_State.stage);
            switch (s_State.stage)
            {
                case "install":
                    if (!File.Exists(Workspace + "/LubanProfile.asset")) return;
                    Require(UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Single(p => p.name == Package).source == PackageSource.Git, "Not a UPM Git installation");
                    s_State.automaticWorkspace = true;
                    string[] schemas = { "__tables__", "__beans__", "__enums__" };
                    foreach (string schema in schemas) Require(File.Exists(Workspace + "/Source/" + schema + ".xlsx"), "Missing starter " + schema);
                    string item = Workspace + "/Source/Item.xlsx";
                    string itemHash = Hash(item);
                    string tableMeta = Hash(Workspace + "/Source/__tables__.xlsx.meta");
                    foreach (string schema in schemas) File.Delete(Workspace + "/Source/" + schema + ".xlsx");
                    Call("UnityQGameFramework.Editor.LubanWorkspace", "EnsureInitialized");
                    foreach (string schema in schemas) Require(File.Exists(Workspace + "/Source/" + schema + ".xlsx"), "Schema repair failed: " + schema);
                    Require(Hash(item) == itemHash && Hash(Workspace + "/Source/__tables__.xlsx.meta") == tableMeta, "Repair overwrote data or metadata");
                    s_State.repairedSchemas = true;
                    var collectionType = Type("UnityQGameFramework.Editor.ResourceTools.ResourceCollection");
                    var collection = Activator.CreateInstance(collectionType, new object[] { null });
                    Require((bool)collectionType.GetMethod("Save").Invoke(collection, null), "Could not create default resource configuration");
                    Require(File.Exists("Assets/UnityQGameFramework/Configs/ResourceCollection.toml"), "Default resource configuration was not written");
                    File.WriteAllText("Assets/KeepMe.txt", "Host-owned sentinel");
                    s_State.settings = Directory.GetFiles("ProjectSettings").Where(f => Path.GetFileName(f) != "UnityQGameFrameworkSettings.asset").OrderBy(f => f).ToArray();
                    s_State.hashes = s_State.settings.Select(Hash).ToArray();
                    var sample = Sample.FindByPackage(Package, PackageVersion).Single(s => s.displayName == "22 · StarForce");
                    s_State.sample = Path.GetRelativePath(Directory.GetCurrentDirectory(), sample.importPath).Replace('\\', '/');
                    Stage("imported");
                    Require(sample.Import(Sample.ImportOptions.OverridePreviousImports), "Package sample import failed");
                    break;
                case "imported":
                    if (Type("StarForce.MenuForm") == null) return;
                    Require(File.Exists(s_State.sample + "/StarForce Launcher.unity"), "Launcher missing after Sample.Import");
                    CheckSettings();
                    s_State.importedFromPackage = true;
                    Require(EditorBuildSettings.scenes.Length == 0, "Test requires an empty host scene list");
                    // Reproduce the old race deterministically, in the disposable consumer only.
                    MonoImporter.SetExecutionOrder(AssetDatabase.LoadAssetAtPath<MonoScript>(s_State.sample + "/GameMain/Scripts/HPBar/HPBarComponent.cs"), -1000);
                    MonoImporter.SetExecutionOrder(AssetDatabase.LoadAssetAtPath<MonoScript>(s_State.sample + "/GameMain/Scripts/Base/GameEntry.cs"), 1000);
                    s_State.reversedStartOrder = true;
                    Stage("ordered");
                    break;
                case "ordered":
                    EditorSceneManager.OpenScene(s_State.sample + "/StarForce Launcher.unity");
                    EditorWindow.GetWindow(Type("UnityEditor.GameView")).Focus();
                    Stage("playing");
                    EditorApplication.isPlaying = true;
                    break;
                case "playing":
                    if (!EditorApplication.isPlaying) return;
                    var menu = UnityEngine.Object.FindFirstObjectByType(Type("StarForce.MenuForm")) as Component;
                    if (menu == null) return;
                    if (menu.GetComponent<CanvasGroup>().alpha < .99f) return;
                    var builtin = UnityEngine.Object.FindFirstObjectByType(Type("StarForce.BuiltinDataComponent")) as Component;
                    Require(builtin != null && builtin.GetType().GetProperty("BuildInfo").GetValue(builtin) != null, "BuildInfo TOML did not load");
                    var builtinFields = new SerializedObject(builtin);
                    foreach (string field in new[] { "m_BuildInfoTextAsset", "m_DefaultDictionaryTextAsset" })
                    {
                        var asset = builtinFields.FindProperty(field).objectReferenceValue;
                        Require(asset != null && AssetDatabase.GetAssetPath(asset).EndsWith(".toml", StringComparison.Ordinal), "TOML migration broke serialized reference: " + field);
                    }
                    var eventSystemType = Type("UnityEngine.EventSystems.EventSystem");
                    var eventSystem = eventSystemType.GetProperty("current").GetValue(null);
                    var module = eventSystemType.GetProperty("currentInputModule").GetValue(eventSystem);
                    Require(module.GetType().FullName == "UnityEngine.EventSystems.StandaloneInputModule" &&
                        module.GetType().GetProperty("inputOverride").GetValue(module)?.GetType().FullName == "StarForce.StarForceInput", "UI input bridge missing");
                    s_State.menuOpened = true;
                    if (!CaptureFrame("urp-menu")) return;
                    s_State.menuVisible = true;
                    var start = menu.GetComponentsInChildren(Type("StarForce.CommonButton"), true).First(b => b.name == "Start");
                    if (!start.gameObject.activeInHierarchy) return;
                    var pointer = Activator.CreateInstance(Type("UnityEngine.EventSystems.PointerEventData"), eventSystem);
                    start.GetType().GetMethod("OnPointerDown").Invoke(start, new[] { pointer });
                    start.GetType().GetMethod("OnPointerUp").Invoke(start, new[] { pointer });
                    Stage("battle");
                    break;
                case "battle":
                    var plane = UnityEngine.Object.FindFirstObjectByType(Type("StarForce.MyAircraft")) as Component;
                    if (plane == null) return;
                    Require(plane.gameObject.layer == 0, "Player still requires a custom Layer");
                    if (!s_State.battleStarted)
                    {
                        s_State.battleStarted = true;
                        s_State.damageTime = Time.time;
                        return;
                    }
                    if (Time.time - s_State.damageTime < .25f) return;
                    var materials = AssetDatabase.FindAssets("t:Material", new[] { s_State.sample + "/GameMain/Materials" }).Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
                    Require(materials.Length == 21 && materials.All(m => m.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal) && m.shader.isSupported), "Sample must use supported URP shaders");
                    Require(Directory.GetFiles(s_State.sample, "*.shader", SearchOption.AllDirectories).Length == 0, "Custom sample shaders remain");
                    var textures = Directory.GetFiles(s_State.sample + "/GameMain/Textures", "*.tif");
                    Require(textures.Length == 21, "Sample TIFF payloads are missing");
                    foreach (var path in textures)
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace('\\', '/'));
                        Require(asset != null && asset.width >= 32 && asset.height >= 32, "TIFF did not import: " + path);
                    }
                    Require(materials.All(m => m.GetTexture("_BaseMap") != null), "A sample material lost its base texture");
                    s_State.tiffTextures = textures.Length;
                    foreach (var shader in materials.Select(m => m.shader).Distinct()) Require(!ShaderUtil.GetShaderMessages(shader).Any(m => m.severity.ToString() == "Error"), "Shader compilation failed");
                    if (!CaptureFrame("urp-battle")) return;
                    s_State.battleVisible = true;
                    Require(s_Errors == 0, "Runtime errors occurred: " + s_Errors);
                    s_State.urpShaders = true;
                    s_State.materialCount = materials.Length;
                    plane.GetType().GetMethod("ApplyDamage").Invoke(plane, new object[] { null, 1 });
                    Require(ActiveHPBar(plane) != null, "Damage did not show a player HP bar");
                    s_State.hpBarShown = true;
                    s_State.damageTime = Time.time;
                    Stage("damage");
                    break;
                case "damage":
                    if (Time.time - s_State.damageTime < 2f) return;
                    var damagedPlane = UnityEngine.Object.FindFirstObjectByType(Type("StarForce.MyAircraft")) as Component;
                    Require(damagedPlane != null, "Player disappeared during HP bar validation");
                    Require(ActiveHPBar(damagedPlane) == null, "HP bar did not fade and recycle");
                    s_State.hpBarRecycled = true;
                    damagedPlane.GetType().GetMethod("ApplyDamage").Invoke(damagedPlane, new object[] { null, 1 });
                    Require(ActiveHPBar(damagedPlane) != null, "Pooled HP bar could not be reused");
                    s_State.hpBarReused = true;
                    Require(s_Errors == 0, "Runtime errors occurred: " + s_Errors);
                    Stage("stop");
                    EditorApplication.isPlaying = false;
                    break;
                case "stop":
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    CheckSettings();
                    var settings = Type("UnityQGameFramework.Editor.LubanSettings").GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
                    var profileProperty = settings.GetType().GetProperty("Profile");
                    profileProperty.SetValue(settings, AssetDatabase.LoadAssetAtPath(s_State.sample + "/UnityQGameFramework/Config/LubanProfile.asset", Type("UnityQGameFramework.Editor.LubanProfile")));
                    Call("UnityQGameFramework.Editor.QgfProjectAssets", "RemoveSample");
                    Require(!Directory.Exists(s_State.sample), "Sample uninstall left its files");
                    Require(AssetDatabase.GetAssetPath((UnityEngine.Object)profileProperty.GetValue(settings)) == Workspace + "/LubanProfile.asset", "Sample uninstall left a broken global Profile");
                    CheckSettings();
                    s_State.sampleRemoved = true;
                    Stage("reimported");
                    Require(Sample.FindByPackage(Package, PackageVersion).Single(s => s.displayName == "22 · StarForce").Import(Sample.ImportOptions.OverridePreviousImports), "Sample reimport failed");
                    break;
                case "reimported":
                    if (Type("StarForce.MenuForm") == null || !Directory.Exists(s_State.sample)) return;
                    CheckSettings();
                    Require(string.IsNullOrEmpty(AssetDatabase.MoveAsset(Workspace, "Assets/MovedConfig")), "Could not move the managed workspace");
                    // Give the import postprocessor a frame to record the sample folder GUID.
                    Stage("remove");
                    break;
                case "remove":
                    Stage("uninstalling");
                    Client.Remove(Package);
                    break;
                case "uninstalling":
                    if (UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Any(p => p.name == Package)) return;
                    Require(!Directory.Exists(Workspace) && !Directory.Exists("Assets/MovedConfig") && !Directory.Exists("Assets/UnityQGameFramework/Configs") && !Directory.Exists(s_State.sample), "Package removal left managed Assets");
                    Require(!File.Exists("ProjectSettings/UnityQGameFrameworkSettings.asset"), "Package settings remain");
                    Require(File.ReadAllText("Assets/KeepMe.txt") == "Host-owned sentinel", "Unrelated assets were removed");
                    Require(Directory.GetFiles("QgfBackup~", "Item.xlsx", SearchOption.AllDirectories).Length > 0, "Excel backup missing");
                    CheckSettings();
                    s_State.packageRemoved = s_State.hostSettingsUnchanged = true;
                    Stage("done");
                    EditorApplication.Exit(0);
                    break;
            }
        }
        catch (Exception exception)
        {
            s_State.failure = exception.ToString();
            Stage("done");
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static Component ActiveHPBar(Component owner) => UnityEngine.Object.FindObjectsByType(Type("StarForce.HPBarItem"), FindObjectsSortMode.None)
        .Cast<Component>().FirstOrDefault(item => item.gameObject.activeInHierarchy && Equals(item.GetType().GetProperty("Owner").GetValue(item), owner));

    private static bool CaptureFrame(string name)
    {
        if (s_Capture == null)
        {
            s_Capture = new GameObject("Sample visual validation").AddComponent<SampleVisualCapture>();
            s_Capture.Capture("Results/" + name + ".png");
            return false;
        }
        if (!s_Capture.Complete) return false;
        Require(s_Capture.WhiteRatio < .9f && s_Capture.BlackRatio < .98f && s_Capture.ColorRatio > .01f,
            name + " has a blank Game view");
        Require(s_Capture.PinkRatio < .001f, name + " contains error-pink rendering");
        UnityEngine.Object.Destroy(s_Capture.gameObject);
        s_Capture = null;
        return true;
    }
}
