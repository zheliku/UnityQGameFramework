using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StarForce;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityQGameFramework.Core;
using Entry = StarForce.GameEntry;
using Object = UnityEngine.Object;

// 隔离工程回归：更新前没有 UI 表，菜单阶段由 UI 模块管理。
public static class ValidateStarForceDialogs
{
    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || !File.Exists("QgfExampleConsumer~.json"))
            throw new InvalidOperationException("Use the isolated Consumer project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play first.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Unsaved scene.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        bool background = Application.runInBackground;
        var input = InputSystem.settings;
        var oldBackground = input.backgroundBehavior;
        var oldEditorInput = input.editorInputBehaviorInPlayMode;
        var disabled = new List<InputDevice>();
        Mouse mouse = null;
        var errors = new List<string>();
        var checks = new List<string>();
        Application.LogCallback capture = (message, stack, kind) =>
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
                errors.Add(message + "\n" + stack);
        };
        void Check(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
            checks.Add(message);
        }
        Application.logMessageReceived += capture;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene("Assets/Examples/02_StarForce/StarForce Launcher.unity");
            await Play(true);
            Application.runInBackground = true;
            input.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            input.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>();
            foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != mouse && device.enabled).ToArray())
            {
                disabled.Add(pointer);
                InputSystem.DisableDevice(pointer);
            }
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            await Until(() => Object.FindFirstObjectByType<MenuForm>() != null);
            await Task.Delay(400);
            for (int mode = 1; mode <= 3; mode++)
            {
                int answers = 0;
                object received = null;
                var data = new DialogParams
                {
                    Mode = mode,
                    Title = "选择操作",
                    Message = "关闭窗口后只执行一次回调。",
                    PauseGame = true,
                    UserData = "managed-" + mode,
                    OnClickConfirm = value => { answers++; received = value; },
                    OnClickCancel = value => { answers++; received = value; },
                    OnClickOther = value => { answers++; received = value; }
                };
                Entry.UI.OpenDialog(data);
                DialogForm form = null;
                await Until(() => (form = Object.FindFirstObjectByType<DialogForm>()) != null && form.Available);
                await Task.Delay(400);
                Check(answers == 0 && form.DialogMode == mode && Entry.Base.IsGamePaused, "Managed dialog waits for a choice: mode " + mode);
                var managedBackground = form.transform.Find("Panel_Mask/Panel_Background").GetComponent<Image>();
                Check(managedBackground.sprite != null && managedBackground.color.a >= 1f, "Managed dialog card is opaque: mode " + mode);
                if (mode == 1)
                {
                    form.OnConfirmButtonClick();
                    form.OnConfirmButtonClick();
                }
                else if (mode == 2)
                {
                    form.OnCancelButtonClick();
                    form.OnConfirmButtonClick();
                }
                else
                {
                    form.OnOtherButtonClick();
                    form.OnCancelButtonClick();
                }
                Check(answers == 1 && Equals(received, data.UserData), "Managed callback and context once: mode " + mode);
                await Until(() => !form.Available);
                Check(!Entry.Base.IsGamePaused, "Managed dialog releases its pause: mode " + mode);
            }
            Entry.Base.PauseGame();
            Entry.UI.OpenDialog(new DialogParams { Mode = 1, Title = "外部暂停", PauseGame = true });
            DialogForm paused = null;
            await Until(() => (paused = Object.FindFirstObjectByType<DialogForm>()) != null && paused.Available);
            paused.OnConfirmButtonClick();
            await Until(() => !paused.Available);
            Check(Entry.Base.IsGamePaused, "Managed dialog preserves an existing pause");
            Entry.Base.ResumeGame();

            // 删除全部表和常规字典，只保留启动场景携带的默认字典。
            Entry.DataTable.UnloadDataTables();
            foreach (var language in new[] { Language.English, Language.ChineseSimplified, Language.ChineseTraditional, Language.Korean })
            {
                Entry.Localization.RemoveAllRawStrings();
                Entry.Localization.Language = language;
                Entry.BuiltinData.InitDefaultDictionary();
                foreach (string key in new[] { "Dialog.ConfirmButton", "Dialog.CancelButton", "ForceUpdate.Title", "ForceUpdate.Message",
                    "UpdateResourceViaCarrierDataNetwork.Title", "UpdateResourceViaCarrierDataNetwork.Message" })
                    Check(!string.IsNullOrEmpty(Entry.Localization.GetRawString(key)), "Startup translation " + language + ": " + key);
                int answers = 0;
                Entry.UI.OpenDialog(new DialogParams
                {
                    Mode = 2,
                    Title = Entry.Localization.GetString("ForceUpdate.Title"),
                    Message = Entry.Localization.GetString("ForceUpdate.Message"),
                    PauseGame = true,
                    OnClickConfirm = _ => answers++,
                    OnClickCancel = _ => answers++
                });
                var form = Object.FindFirstObjectByType<BuiltinDialogForm>();
                Check(form != null && answers == 0 && Entry.Base.IsGamePaused, "Builtin dialog waits without tables: " + language);
                var builtinBackground = form.transform.Find("Panel_Mask/Panel_Background").GetComponent<Image>();
                Check(builtinBackground.sprite != null && builtinBackground.color.a >= 1f, "Builtin dialog card is opaque: " + language);
                Check(form.GetComponentsInChildren<Text>(true).All(text => text.font != null), "Builtin fonts exist: " + language);
                await Task.Delay(200);
                if (language == Language.ChineseSimplified)
                {
                    Directory.CreateDirectory("Artifacts~/StarForceDialogs");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/StarForceDialogs/startup-dialog.png"));
                    await Task.Delay(250);
                    var cancel = form.GetComponentsInChildren<CommonButton>().Single(button => button.name == "Button_Cancel");
                    var label = cancel.GetComponentInChildren<Text>();
                    var canvas = form.GetComponent<Canvas>();
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, label.rectTransform.TransformPoint(label.rectTransform.rect.center));
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
                    Check(hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(cancel.transform), "Builtin Cancel is the first mouse raycast target");
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                    await Task.Delay(100);
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
                    await Task.Delay(100);
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                    await Task.Delay(100);
                    Check(answers == 1, "Native mouse selects Cancel");
                }
                else
                {
                    form.OnCancelButtonClick();
                    form.OnConfirmButtonClick();
                }
                Check(answers == 1 && !Entry.Base.IsGamePaused, "Builtin cancellation once: " + language);
                await Task.Delay(100);
                Check(Object.FindFirstObjectByType<BuiltinDialogForm>() == null, "Builtin instance destroyed: " + language);
            }
            Entry.BuiltinData.OpenDialog(new DialogParams
            {
                Mode = 1,
                Title = "重试",
                OnClickConfirm = _ => Entry.BuiltinData.OpenDialog(new DialogParams { Mode = 1, Title = "再次确认" })
            });
            var firstDialog = Object.FindFirstObjectByType<BuiltinDialogForm>();
            firstDialog.OnConfirmButtonClick();
            var secondDialog = Object.FindFirstObjectByType<BuiltinDialogForm>();
            Check(secondDialog != null && secondDialog != firstDialog, "Builtin callback can open the next dialog in the same frame");
            secondDialog.OnConfirmButtonClick();
            await Task.Delay(100);
            Check(Object.FindFirstObjectByType<BuiltinDialogForm>() == null, "Chained builtin dialogs are destroyed");
            Check(errors.Count == 0, "No runtime errors: " + string.Join("\n", errors));
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                await Play(false);
            if (mouse != null && mouse.added)
                InputSystem.RemoveDevice(mouse);
            foreach (var device in disabled.Where(device => device.added && !device.enabled))
                InputSystem.EnableDevice(device);
            input.backgroundBehavior = oldBackground;
            input.editorInputBehaviorInPlayMode = oldEditorInput;
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            Application.runInBackground = background;
            if (setup.Any(scene => scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        var result = new
        {
            passed = true,
            unity = Application.unityVersion,
            checks,
            interaction = "Direct handlers; Chinese startup Cancel uses native mouse input"
        };
        Directory.CreateDirectory("Artifacts~/StarForceDialogs");
        File.WriteAllText("Artifacts~/StarForceDialogs/check.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return new
        {
            result.passed,
            result.unity,
            assertions = checks.Count
        };
    }

    private static async Task Until(Func<bool> condition)
    {
        double start = EditorApplication.timeSinceStartup;
        while (!condition())
        {
            if (EditorApplication.timeSinceStartup - start > 20)
                throw new TimeoutException("Dialog state");
            await Task.Delay(40);
        }
    }

    private static async Task Play(bool enabled)
    {
        EditorApplication.isPlaying = enabled;
        await Until(() => EditorApplication.isPlaying == enabled && !EditorApplication.isPlayingOrWillChangePlaymode == !enabled);
        await Task.Delay(200);
    }
}
