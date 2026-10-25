using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarForce;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Localization;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;
using Entry = StarForce.GameEntry;

// 仅复制到隔离验收工程，通过命令行启用，不随 Sample 发布。
public sealed class StarForcePlayerProbe : MonoBehaviour
{
    private static string resultPath, run, expectedLanguage;
    private readonly Result result = new Result();
#if ENABLE_INPUT_SYSTEM
    private Mouse mouse;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-qgf-starforce-result") resultPath = args[++i];
            else if (args[i] == "-qgf-starforce-run") run = args[++i];
            else if (args[i] == "-qgf-starforce-language") expectedLanguage = args[++i];
        }
        if (!string.IsNullOrEmpty(resultPath))
        {
            var probe = new GameObject("StarForce Player Probe");
            DontDestroyOnLoad(probe);
            probe.AddComponent<StarForcePlayerProbe>();
        }
    }

    private async void Start()
    {
        Application.runInBackground = true;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        Application.logMessageReceived += OnLog;
        try
        {
            await Until(() => Find<MenuForm>() != null && Entry.Procedure?.CurrentProcedure is ProcedureMenu, "Package boot");
            result.jsonChecks = StarForceJsonChecks.Run();
            Require(!Application.isEditor && Entry.Resource.ResourceMode == ResourceMode.Package, "Actual Package backend");
            Require(Entry.Resource.Settings.GetPackage().PlayMode == ResourcePlayMode.Offline
                && Entry.Resource.Settings.DefaultPackageName == "QgfExamples.StarForce", "Sample-owned YooAsset package");
            Require(Entry.Config.GetInt("Scene.Menu") == 1 && Entry.DataTable.GetDataTable<TbScene>().GetDataRow(2) != null, "TOML and Luban data");
            result.packageBoot = true;
            if (run == "second")
            {
                Require(Entry.Localization.Language.ToString() == expectedLanguage
                    && Entry.Setting.GetString(Constant.Setting.Language) == expectedLanguage, "Language persisted across Player processes");
                result.persistedAcrossProcesses = true;
            }
            Entry.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnEntity);
            Find<MenuForm>().OnSettingButtonClick();
            await Until(() => Find<SettingForm>() != null, "Settings form");
            Find<SettingForm>().OnMusicMuteChanged(false);
            Require(Entry.Sound.IsMuted("Music"), "Music mute");
            Find<SettingForm>().OnMusicMuteChanged(true);
            Find<SettingForm>().OnMusicVolumeChanged(0.45f);
            Require(Math.Abs(Entry.Sound.GetVolume("Music") - 0.45f) < 0.01f, "Music volume");
            Find<SettingForm>().Close(true);
            Find<MenuForm>().OnAboutButtonClick();
            await Until(() => Find<AboutForm>() != null, "About form");
            Find<AboutForm>().Close(true);
            Find<MenuForm>().OnQuitButtonClick();
            await Until(() => Find<DialogForm>() != null, "Quit dialog");
            Find<DialogForm>().OnCancelButtonClick();
            await Until(() => Find<DialogForm>() == null, "Cancel quit");
            result.menuForms = true;
#if ENABLE_INPUT_SYSTEM
            mouse = InputSystem.AddDevice<Mouse>();
            result.inputMode = "InputSystem virtual mouse";
#else
            result.inputMode = "Legacy backend; movement target injected and Weapon.TryAttack invoked";
#endif
            for (int round = 0; round < 2; round++)
            {
                Find<MenuForm>().OnStartButtonClick();
                await Until(() => Find<MyAircraft>() != null && ((IList)Field(Find<MyAircraft>(), "m_Weapons")).Count > 0, "Aircraft attachments");
                var aircraft = Find<MyAircraft>();
                Vector3 initial = aircraft.transform.position;
                await Task.Delay(180);
                Require((aircraft.transform.position - initial).sqrMagnitude < 0.01f, "Fresh movement target on reuse");
                int bulletsBefore = result.bullets;
                Vector3 target = initial + new Vector3(2, 0, 2);
#if ENABLE_INPUT_SYSTEM
                Vector2 position = Camera.main.WorldToScreenPoint(target);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
#else
                // 旧 Input 不能注入虚拟鼠标，直接设置业务目标并触发武器，验证同一套移动与实体逻辑。
                aircraft.GetType().GetField("m_TargetPosition", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(aircraft, target);
                foreach (Weapon weapon in (IList)Field(aircraft, "m_Weapons")) weapon.TryAttack();
#endif
                await Until(() => (aircraft.transform.position - initial).sqrMagnitude > 0.25f && result.bullets > bulletsBefore, "Mouse movement and fire");
#if ENABLE_INPUT_SYSTEM
                InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 0 });
#endif
                aircraft.ApplyDamage(null, 1);
                Require(((IList)Field(Entry.HPBar, "m_ActiveHPBarItems")).Count > 0, "HP bar shown");
                aircraft.ApplyDamage(null, 1000000);
                await Until(() => Find<MenuForm>() != null && Entry.Procedure.CurrentProcedure is ProcedureMenu, "Death to menu");
                Require(Find<MyAircraft>() == null && Entry.Scene.GetLoadedSceneAssetNames().Length == 1, "Battle cleanup");
                result.rounds++;
            }
            Entry.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnEntity);
            result.battle = true;
            if (run == "first")
            {
                var next = Entry.Localization.Language == Language.Korean ? Language.English : Language.Korean;
                Find<MenuForm>().OnSettingButtonClick();
                await Until(() => Find<SettingForm>() != null, "Language form");
                if (next == Language.Korean) Find<SettingForm>().OnKoreanSelected(true);
                else Find<SettingForm>().OnEnglishSelected(true);
                Find<SettingForm>().OnSubmitButtonClick();
                await Until(() => Find<MenuForm>() != null && Entry.Localization.Language == next, "Language restart");
                result.languageRestart = true;
            }
            result.language = Entry.Localization.Language.ToString();
            // 保持关于页打开后退出，检查关闭框架时不会再次请求音乐资源。
            Find<MenuForm>().OnAboutButtonClick();
            await Until(() => Find<AboutForm>() != null, "About form before quit");
            result.aboutOpenAtQuit = true;
            Require(result.errors == 0, "Player runtime errors");
            result.passed = true;
        }
        catch (Exception exception) { result.failure = exception.ToString(); }
        finally
        {
#if ENABLE_INPUT_SYSTEM
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
#endif
            Application.logMessageReceived -= OnLog;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            result.unity = Application.unityVersion;
            result.run = run;
            File.WriteAllText(resultPath, JsonUtility.ToJson(result, true));
            Application.Quit(result.passed ? 0 : 1);
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // 语言切换会重建 Base；无窗口的验证进程需要继续在后台运行。
        Application.runInBackground = true;
    }

    private void OnLog(string message, string stack, LogType kind)
    {
        if (kind != LogType.Error && kind != LogType.Exception && kind != LogType.Assert) return;
        result.errors++;
        result.lastError = message + "\n" + stack;
    }
    private void OnEntity(object sender, GameEventArgs args)
    {
        if (((ShowEntitySuccessEventArgs)args).EntityLogicType == typeof(Bullet)) result.bullets++;
    }
    private static T Find<T>() where T : UnityEngine.Object
    {
        // 从框架登记的活动实例查询，排除 Bundle 中加载的预制体资产。
        if (typeof(UIFormLogic).IsAssignableFrom(typeof(T)))
            return Entry.UI == null ? null : Entry.UI.GetAllLoadedUIForms().Select(form => form.Logic).OfType<T>().FirstOrDefault();
        if (typeof(EntityLogic).IsAssignableFrom(typeof(T)))
            return Entry.Entity == null ? null : Entry.Entity.GetAllLoadedEntities().Select(entity => entity.Logic).OfType<T>().FirstOrDefault();
        return UnityEngine.Object.FindFirstObjectByType<T>();
    }
    private static object Field(object instance, string name)
    {
        for (var type = instance.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null) return field.GetValue(instance);
        }
        throw new Exception("Missing field " + name);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static async Task Until(Func<bool> condition, string message)
    {
        double start = Time.realtimeSinceStartupAsDouble;
        while (!condition())
        {
            if (Time.realtimeSinceStartupAsDouble - start > 20)
            {
                var procedure = Entry.Procedure?.CurrentProcedure;
                var game = procedure is ProcedureMain ? Field(procedure, "m_CurrentGame") as GameBase : null;
                var aircraft = game == null ? null : Field(game, "m_MyAircraft") as MyAircraft;
                throw new TimeoutException(message + "; procedure=" + procedure?.GetType().Name + "; gameOver=" + game?.GameOver
                    + "; gameAircraft=" + aircraft?.GetInstanceID() + "; dead=" + aircraft?.IsDead + "; activeAircraft=" + Find<MyAircraft>()?.GetInstanceID()
                    + "; timeScale=" + Time.timeScale + "; frame=" + Time.frameCount);
            }
            await Task.Delay(25);
        }
    }

    [Serializable]
    private sealed class Result
    {
        public bool passed, packageBoot, menuForms, battle, languageRestart, persistedAcrossProcesses, aboutOpenAtQuit;
        public int errors, bullets, rounds, jsonChecks;
        public string unity, run, language, inputMode, failure, lastError;
    }
}
