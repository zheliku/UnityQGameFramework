// Run in an isolated Unity 6 consumer with only a QGF Git dependency.
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

[InitializeOnLoad]
public static class SampleLifecycleValidation
{
    private const string Package = "com.zheliku.unityqgameframework";
    private const string StatePath = "Results/sample-stage.json";
    private const string Workspace = "Assets/UnityQGameFramework/Config";
    private static double s_Started = EditorApplication.timeSinceStartup;
    private static State s_State;
    private static int s_Errors;

    [Serializable] private sealed class State
    {
        public string stage;
        public string sample;
        public string[] settings;
        public string[] hashes;
        public bool automaticWorkspace, repairedSchemas, importedFromPackage, menuOpened, battleStarted;
        public bool independentShaders, sampleRemoved, packageRemoved, hostSettingsUnchanged;
        public int materialCount;
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
                    if (!File.Exists(Workspace + "/Editor/LubanProfile.asset")) return;
                    Require(UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Single(p => p.name == Package).source == PackageSource.Git, "Not a UPM Git installation");
                    s_State.automaticWorkspace = true;
                    string[] schemas = { "__tables__", "__beans__", "__enums__" };
                    foreach (string schema in schemas) Require(File.Exists(Workspace + "/Source/Datas/" + schema + ".xlsx"), "Missing starter " + schema);
                    string item = Workspace + "/Source/Datas/Item.xlsx";
                    string itemHash = Hash(item);
                    string tableMeta = Hash(Workspace + "/Source/Datas/__tables__.xlsx.meta");
                    foreach (string schema in schemas) File.Delete(Workspace + "/Source/Datas/" + schema + ".xlsx");
                    Call("UnityQGameFramework.Editor.LubanWorkspace", "EnsureInitialized");
                    foreach (string schema in schemas) Require(File.Exists(Workspace + "/Source/Datas/" + schema + ".xlsx"), "Schema repair failed: " + schema);
                    Require(Hash(item) == itemHash && Hash(Workspace + "/Source/Datas/__tables__.xlsx.meta") == tableMeta, "Repair overwrote data or metadata");
                    s_State.repairedSchemas = true;
                    var collectionType = Type("UnityQGameFramework.Editor.ResourceTools.ResourceCollection");
                    var collection = Activator.CreateInstance(collectionType, new object[] { null });
                    Require((bool)collectionType.GetMethod("Save").Invoke(collection, null), "Could not create default resource configuration");
                    Require(File.Exists("Assets/UnityQGameFramework/Configs/ResourceCollection.toml"), "Default resource configuration was not written");
                    File.WriteAllText("Assets/KeepMe.txt", "Host-owned sentinel");
                    s_State.settings = Directory.GetFiles("ProjectSettings").Where(f => Path.GetFileName(f) != "UnityQGameFrameworkSettings.asset").OrderBy(f => f).ToArray();
                    s_State.hashes = s_State.settings.Select(Hash).ToArray();
                    var sample = Sample.FindByPackage(Package, "0.1.0-exp.2").Single();
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
                    EditorSceneManager.OpenScene(s_State.sample + "/StarForce Launcher.unity");
                    Stage("playing");
                    EditorApplication.isPlaying = true;
                    break;
                case "playing":
                    if (!EditorApplication.isPlaying) return;
                    var menu = UnityEngine.Object.FindFirstObjectByType(Type("StarForce.MenuForm")) as Component;
                    if (menu == null) return;
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
                    s_State.battleStarted = true;
                    var materials = AssetDatabase.FindAssets("t:Material", new[] { s_State.sample + "/GameMain/Materials" }).Select(g => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
                    Require(materials.Length == 21 && materials.All(m => m.shader.name.StartsWith("StarForce/") && m.shader.isSupported), "Sample shader dependency remains");
                    foreach (var shader in materials.Select(m => m.shader).Distinct()) Require(!ShaderUtil.GetShaderMessages(shader).Any(m => m.severity.ToString() == "Error"), "Shader compilation failed");
                    var camera = Camera.main;
                    var texture = new RenderTexture(480, 640, 24);
                    var previous = RenderTexture.active;
                    camera.targetTexture = texture;
                    camera.Render();
                    RenderTexture.active = texture;
                    var image = new Texture2D(480, 640, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, 480, 640), 0, 0);
                    image.Apply();
                    File.WriteAllBytes("Results/builtin-battle.png", image.EncodeToPNG());
                    Require(image.GetPixels().Count(c => c.r > .95f && c.g < .05f && c.b > .95f) < 30, "Rendered error-pink pixels");
                    camera.targetTexture = null;
                    RenderTexture.active = previous;
                    UnityEngine.Object.Destroy(texture);
                    UnityEngine.Object.Destroy(image);
                    Require(s_Errors == 0, "Runtime errors occurred: " + s_Errors);
                    s_State.independentShaders = true;
                    s_State.materialCount = materials.Length;
                    Stage("stop");
                    EditorApplication.isPlaying = false;
                    break;
                case "stop":
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    CheckSettings();
                    var settings = Type("UnityQGameFramework.Editor.LubanSettings").GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
                    var profileProperty = settings.GetType().GetProperty("Profile");
                    profileProperty.SetValue(settings, AssetDatabase.LoadAssetAtPath(s_State.sample + "/UnityQGameFramework/Config/Editor/LubanProfile.asset", Type("UnityQGameFramework.Editor.LubanProfile")));
                    Call("UnityQGameFramework.Editor.QgfProjectAssets", "RemoveSample");
                    Require(!Directory.Exists(s_State.sample), "Sample uninstall left its files");
                    Require(AssetDatabase.GetAssetPath((UnityEngine.Object)profileProperty.GetValue(settings)) == Workspace + "/Editor/LubanProfile.asset", "Sample uninstall left a broken global Profile");
                    CheckSettings();
                    s_State.sampleRemoved = true;
                    Stage("reimported");
                    Require(Sample.FindByPackage(Package, "0.1.0-exp.2").Single().Import(Sample.ImportOptions.OverridePreviousImports), "Sample reimport failed");
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
}
