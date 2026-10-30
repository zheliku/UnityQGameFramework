using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.Modules;

// 开发验收工具，不随 Sample 导入。按钮和 Toggle 使用输入系统鼠标事件。
public static class ValidateSoundExamples
{
    private static readonly List<string> assertions = new List<string>();

    public static Task<object> Run() => RunOnly(null);

    public static async Task<object> RunOnly(string[] names)
    {
        if (!Application.dataPath.Contains("Artifacts~"))
            throw new Exception("Use the isolated validation project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new Exception("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(index => UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty))
            throw new Exception("Unsaved scene.");
        assertions.Clear();
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var cases = layouts.Properties().Where(pair => new[] { "15_Sound" }
            .Any(folder => ((string)pair.Value["script"]).StartsWith("Assets/Examples/01_Modules/" + folder + "/")))
            .Select(pair => new { name = pair.Name, layout = pair.Value, mode = 0 }).ToList();
        if (names != null)
            cases = cases.Where(test => names.Contains(test.name)).ToList();
        var settings = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .ToDictionary(name => "ProjectSettings/" + name + ".asset", name => Hash("ProjectSettings/" + name + ".asset"));
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
        bool originalBackground = Application.runInBackground;
        var inputSettings = InputSystem.settings;
        var background = inputSettings.backgroundBehavior;
        var editorInput = inputSettings.editorInputBehaviorInPlayMode;
        var disabled = new HashSet<InputDevice>();
        var errors = new List<string>();
        var completed = new List<object>();
        Mouse mouse = null;
        Application.LogCallback capture = (message, stack, kind) =>
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
                errors.Add(message + "\n" + stack);
        };
        Application.logMessageReceived += capture;
        try
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>();
            foreach (var test in cases)
            {
                string sourcePath = (string)test.layout["script"];
                string anchor = AssetDatabase.GUIDToAssetPath("2f45ea37b22b5d17a52e6d6baac64b5e");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal))
                    throw new Exception("Imported Sound sample not found.");
                string moduleRoot = Path.GetDirectoryName(anchor).Replace('\\', '/');
                string script = moduleRoot + sourcePath.Substring("Assets/Examples/01_Modules".Length);
                EditorSceneManager.OpenScene(script.Substring(0, script.Length - 3) + ".unity");
                await Play(true);
                Application.runInBackground = true;
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != mouse && device.enabled).ToArray())
                {
                    disabled.Add(pointer);
                    InputSystem.DisableDevice(pointer);
                }
                await Task.Delay(550);
                var type = typeof(OneShotSoundExample).Assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)
                    ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)).First(candidate => candidate != null);
                var example = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(type);
                Require(example != null, test.name + " component");
                Require(EventSystem.current?.currentInputModule != null, test.name + " input module");
                var data = new SerializedObject(example);
                foreach (var field in ((JArray)test.layout["controls"]).OfType<JObject>().Select(control => (string)control["field"]).Where(field => field != null))
                    Require(data.FindProperty(field)?.objectReferenceValue != null, test.name + "." + field + " serialized reference");
                int start = assertions.Count;
                await Check(test.name, test.mode, example, data, mouse);
                await Play(false);
                Require(errors.Count == 0, test.name + " runtime errors: " + string.Join("\n", errors));
                completed.Add(new
                {
                    example = test.name,
                    scene = script.Substring(0, script.Length - 3) + ".unity",
                    startMode = test.mode,
                    assertions = assertions.Skip(start).ToArray()
                });
            }
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                await Play(false);
            if (mouse != null && mouse.added)
                InputSystem.RemoveDevice(mouse);
            foreach (var device in disabled.Where(device => device.added && !device.enabled))
                InputSystem.EnableDevice(device);
            inputSettings.backgroundBehavior = background;
            inputSettings.editorInputBehaviorInPlayMode = editorInput;
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            Application.runInBackground = originalBackground;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Require(settings.All(item => Hash(item.Key) == item.Value), "Host settings unchanged");
        var result = new
        {
            passed = true,
            unity = Application.unityVersion,
            nativeButtonsAndToggles = true,
            modeAndTextSetup = "Dropdown.value and InputField.text set by the test; actions use native mouse clicks",
            settingsUnchanged = true,
            scenes = completed,
            assertionCount = assertions.Count
        };
        Directory.CreateDirectory("Artifacts~/SoundExamples");
        string report = names == null ? "sound-examples-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/SoundExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
        return new
        {
            result.passed,
            result.unity,
            scenes = completed.Count,
            result.assertionCount,
            result.settingsUnchanged
        };
    }



    private static async Task Check(string name, int mode, MonoBehaviour example, SerializedObject data, Mouse mouse)
    {
        Func<string, Button> button = key => (Button)data.FindProperty(key).objectReferenceValue;
        Func<string, TMP_Text> text = key => (TMP_Text)data.FindProperty(key).objectReferenceValue;
        Func<string, TMP_Dropdown> dropdown = key => (TMP_Dropdown)data.FindProperty(key).objectReferenceValue;
        Func<string, Toggle> toggle = key => (Toggle)data.FindProperty(key).objectReferenceValue;
        Func<string, Slider> slider = key => (Slider)data.FindProperty(key).objectReferenceValue;
        Func<int> serial = () => Field<int>(example, "serialId");
        Func<ISoundAgent> agent = () => Field<ISoundAgent>(example, "agent");
        if (name == "OneShotSoundExample")
        {
            Require(dropdown("playModeDropdown").options.Count == 6, "Six meaningful playback options");
            Require(QGF.Sound.IsLoadingSound(serial()), "Sound loading is asynchronous");
            Require(QGF.Sound.GetAllLoadingSoundSerialIds().Contains(serial()), "Loading array contains request");
            await Click(button("cancelButton"), mouse);
            await Until(() => !QGF.Sound.IsLoadingSound(serial()), 5000, "Cancelled load finishes cleanup");
            await Task.Delay(2300);
            Require(agent() == null, "Cancelled request never starts an agent");
            await Click(button("playButton"), mouse);
            await Click(button("cancelAllButton"), mouse);
            await Until(() => !QGF.Sound.IsLoadingSound(serial()), 5000, "Cancel-all finishes cleanup");
            await Task.Delay(2300);
            Require(agent() == null, "Cancel-all never starts an agent");
            for (int index = 0; index < 6; index++)
            {
                dropdown("playModeDropdown").value = index;
                await Click(button("playButton"), mouse);
                int current = serial();
                await Until(() => agent() != null && agent().IsPlaying, 5000, "Play overload " + index);
                await Task.Delay(220);
                Require(agent().SerialId == current && agent().Length > 3, "Real sound agent and clip " + index);
                Require(slider("progressSlider").value > 0, "Live playback progress " + index);
                Require(text("outputText").text.Contains(index == 3 || index == 5 ? "UserData：当前对象" : "UserData：未设置"), "User data routing " + index);
                await Until(() => agent().Length == 0, 5000, "Natural completion resets agent " + index);
            }
            await Screenshot("playback");
        }
        else if (name == "LoopSoundExample")
        {
            await Until(() => agent() != null && agent().IsPlaying, 5000, "Loop started");
            Require(dropdown("fadeDropdown").options.Count == 2, "Two meaningful fade options");
            Require(dropdown("controlDropdown").options.Count == 2, "Serial and direct-agent control options");
            Require(!button("replayButton").interactable && !button("resetButton").interactable, "Agent-only operations disabled in serial mode");
            var active = agent();
            var source = ((SoundAgentHelperBase)active.Helper).GetComponent<AudioSource>();
            for (int route = 0; route < 2; route++)
            {
                dropdown("controlDropdown").value = route;
                for (int fade = 0; fade < 2; fade++)
                {
                    dropdown("fadeDropdown").value = fade;
                    int current = serial();
                    await Click(button("pauseButton"), mouse);
                    await Task.Delay(850);
                    Require(active.Length > 0 && active.SerialId == current, "Pause retains clip and serial " + fade);
                    Require(!source.isPlaying, "AudioSource paused " + fade);
                    float position = active.Time;
                    await Task.Delay(250);
                    Require(Mathf.Abs(active.Time - position) < .06f, "Paused position stays fixed " + fade);
                    Require(button("resumeButton").interactable, "Resume stays available " + fade);
                    // Duplicate pause must preserve the remembered volume.
                    QGF.Sound.PauseSound(current);
                    slider("seekSlider").value = .4f;
                    Require(Mathf.Abs(active.Time - active.Length * .4f) < .1f, "Seek while paused " + fade);
                    if (route == 1)
                    {
                        var clip = source.clip;
                        await Click(button("replayButton"), mouse);
                        await Task.Delay(750);
                        Require(source.isPlaying && active.Volume > .28f, "Direct replay restores paused volume " + fade);
                        Require(serial() == current && active.SerialId == current && ReferenceEquals(clip, source.clip)
                            && QGF.Sound.GetAllLoadingSoundSerialIds().Length == 0, "Direct replay reuses clip and request " + fade);
                        await Click(button("pauseButton"), mouse);
                        await Task.Delay(850);
                        Require(!source.isPlaying, "Direct pause after replay " + fade);
                    }
                    await Screenshot("paused-" + fade);
                    await Click(button("resumeButton"), mouse);
                    await Task.Delay(850);
                    Require(source.isPlaying && active.Volume > .28f, "Resume restores playback and volume " + fade);
                    QGF.Sound.ResumeSound(current);
                    Require(active.Volume > .28f, "Duplicate resume does not silence playback " + fade);
                    slider("pitchSlider").value = 1.5f;
                    Require(Mathf.Approximately(active.Pitch, 1.5f), "Live pitch " + fade);
                    await Click(button("stopButton"), mouse);
                    await Until(() => active.Length == 0, 4000, "Stop releases clip " + fade);
                    await Click(button("playButton"), mouse);
                    await Until(() => agent() != null && agent().Length > 0, 4000, "Restart " + fade);
                    active = agent();
                    source = ((SoundAgentHelperBase)active.Helper).GetComponent<AudioSource>();
                }
            }
            await Click(button("pauseButton"), mouse);
            await Task.Delay(850);
            await Click(button("resetButton"), mouse);
            Require(active.Length == 0 && source.clip == null && !source.isPlaying, "Reset releases paused clip");
            Require(!active.Loop && active.Pitch == 1 && active.SpatialBlend == 0, "Reset restores agent defaults");
            Require(button("playButton").interactable && !button("resetButton").interactable, "Reset returns UI to loadable state");
            await Click(button("playButton"), mouse);
            await Until(() => agent() != null && agent().Length > 0, 4000, "Component loads again after direct reset");
            Require(ReferenceEquals(active, agent()), "Reset keeps reusable proxy");
            source = ((SoundAgentHelperBase)agent().Helper).GetComponent<AudioSource>();
            await Click(toggle("loopToggle"), mouse);
            Require(!active.Loop, "Loop toggled off");
            await Until(() => active.Length == 0, 5000, "Non-looping sound completes");
            await Click(button("playButton"), mouse);
            await Until(() => agent() != null && agent().Length > 0, 4000, "Play for paused stop-all");
            await Click(button("pauseButton"), mouse);
            await Task.Delay(850);
            QGF.Sound.StopAllLoadedSounds();
            await Until(() => agent().Length == 0, 4000, "Stop-all also releases paused clip");
        }
        else if (name == "SoundGroupVolumeExample")
        {
            await Until(() => agent() != null && agent().Length > 0, 4000, "Music group starts");
            Require(ReferenceEquals(agent().SoundGroup, QGF.Sound.GetSoundGroup("Music")), "Agent belongs to displayed group");
            Require(text("outputText").text.Contains("最终音量：0.20 = 组 0.40 × 单声道 0.50")
                && text("outputText").text.Contains("最终状态：有声"), "Group and voice levels are shown separately");
            slider("groupVolumeSlider").value = .6f;
            slider("voiceVolumeSlider").value = .3f;
            Require(Mathf.Abs(agent().Volume - .18f) < .01f, "Group times local volume");
            await Click(toggle("groupMuteToggle"), mouse);
            Require(agent().Mute, "Group mute reaches agent");
            await Click(toggle("voiceMuteToggle"), mouse);
            await Click(toggle("groupMuteToggle"), mouse);
            Require(agent().Mute && !agent().SoundGroup.Mute, "Local mute survives group unmute");
            await Click(toggle("voiceMuteToggle"), mouse);
            Require(!agent().Mute, "Both unmuted");
            await Click(button("stopButton"), mouse);
            await Until(() => agent().Length == 0, 4000, "Stop Music group");
            await Click(button("playButton"), mouse);
            await Until(() => agent() != null && agent().Length > 0, 4000, "Replay Music group");
            await Screenshot("groups");
        }
        else if (name == "SpatialSoundExample")
        {
            Require(dropdown("playMode").options.Count == 3, "Three world-position overloads");
            var listener = Field<Transform>(example, "listenerMarker");
            var marker = Field<Transform>(example, "sourceMarker");
            await Until(() => agent() != null && agent().Length > 0, 4000, "Spatial sound started");
            Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l => l.enabled) == 1, "One enabled listener");
            object context = null;
            EventHandler<UnityQGameFramework.Core.GameEventArgs> success = (sender, args) =>
            {
                var e = (UnityQGameFramework.Runtime.PlaySoundSuccessEventArgs)args;
                if (e.SoundAgent.SoundGroup.Name == "World")
                    context = e.UserData;
            };
            QGF.Event.Subscribe(UnityQGameFramework.Runtime.PlaySoundSuccessEventArgs.EventId, success);
            try
            {
                for (int index = 0; index < 3; index++)
                {
                    dropdown("playMode").value = index;
                    slider("positionSlider").value = index % 2 == 0 ? -3 : 3;
                    await Click(button("playButton"), mouse);
                    await Until(() => agent() != null && agent().Length > 0, 4000, "Position overload " + index);
                    var source = ((SoundAgentHelperBase)agent().Helper).GetComponent<AudioSource>();
                    Require(Vector3.Distance(source.transform.position, marker.position) < .01f, "World position applied " + index);
                    Require(index == 2 ? ReferenceEquals(context, example) : context == null, "Spatial user data " + index);
                    Require(source.rolloffMode == AudioRolloffMode.Linear && source.minDistance == 1, "Linear distance attenuation " + index);
                    var original = source.transform.position;
                    slider("positionSlider").value = 0;
                    await Task.Delay(120);
                    Require(source.transform.position == original && marker.position != original, "Fixed position does not follow marker " + index);
                    slider("distanceSlider").value = 4;
                    slider("blendSlider").value = .5f;
                    slider("panSlider").value = -.75f;
                    Require(agent().MaxDistance == 4 && agent().SpatialBlend == .5f && agent().PanStereo == -.75f, "Live spatial properties " + index);
                    Require(text("stateText").text.Contains("点击播放"), "Pending marker position is visible " + index);
                }
                slider("blendSlider").value = 0;
                Require(agent().SpatialBlend == 0, "2D ignores spatial position");
                slider("positionSlider").value = 3;
                await Click(button("playButton"), mouse);
                await Until(() => agent() != null && agent().Length > 0, 4000, "Apply final visible position");
                await Task.Delay(150);
                var scroll = button("playButton").GetComponentInParent<ScrollRect>();
                Canvas.ForceUpdateCanvases();
                Require(scroll.content.rect.height <= scroll.viewport.rect.height + 2, "Spatial controls fit with live state visible: " + scroll.content.rect.height + "/" + scroll.viewport.rect.height);
                await Screenshot("spatial-position");
                await Click(button("stopButton"), mouse);
                await Until(() => agent().Length == 0, 3000, "Spatial stop releases clip");
            }
            finally { QGF.Event.Unsubscribe(UnityQGameFramework.Runtime.PlaySoundSuccessEventArgs.EventId, success); }
        }
        else if (name == "FollowEntitySound")
        {
            Require(dropdown("playMode").options.Count == 3, "Three entity-binding overloads");
            await Until(() => QGF.Entity.HasEntity(1), 4000, "Emitter entity displayed");
            object context = null;
            Entity binding = null;
            EventHandler<UnityQGameFramework.Core.GameEventArgs> success = (sender, args) =>
            {
                var e = (UnityQGameFramework.Runtime.PlaySoundSuccessEventArgs)args;
                if (e.SoundAgent.SoundGroup.Name == "World")
                {
                    context = e.UserData;
                    binding = e.BindingEntity;
                }
            };
            QGF.Event.Subscribe(UnityQGameFramework.Runtime.PlaySoundSuccessEventArgs.EventId, success);
            try
            {
                for (int index = 0; index < 3; index++)
                {
                    dropdown("playMode").value = index;
                    await Click(button("playButton"), mouse);
                    await Until(() => agent() != null && agent().Length > 0, 4000, "Entity overload " + index);
                    var entity = QGF.Entity.GetEntity(1);
                    var source = ((SoundAgentHelperBase)agent().Helper).GetComponent<AudioSource>();
                    Require(binding == entity, "Binding event carries entity " + index);
                    Require(index == 2 ? ReferenceEquals(context, example) : context == null, "Binding user data " + index);
                    Require(agent().SpatialBlend == 1 && agent().Loop, "Spatial looping emitter " + index);
                    var original = source.transform.position;
                    await Task.Delay(650);
                    Require(Vector3.Distance(source.transform.position, entity.transform.position) < .35f, "Agent follows live entity position " + index);
                    Require(Vector3.Distance(source.transform.position, original) > .2f, "Emitter actually moves " + index);
                    slider("dopplerSlider").value = 2.5f;
                    Require(agent().DopplerLevel == 2.5f, "Live Doppler " + index);
                    await Screenshot("entity-follow-" + index);
                    var active = agent();
                    await Click(button("hideButton"), mouse);
                    await Until(() => !QGF.Entity.HasEntity(1) && active.Length == 0, 4000, "Hidden entity releases bound sound " + index);
                    Require(!button("playButton").interactable && button("showButton").interactable, "Hidden controls " + index);
                    await Click(button("showButton"), mouse);
                    await Until(() => QGF.Entity.HasEntity(1), 4000, "Emitter shown again " + index);
                }
            }
            finally { QGF.Event.Unsubscribe(UnityQGameFramework.Runtime.PlaySoundSuccessEventArgs.EventId, success); }
        }
        else if (name == "ReplaceSoundAgents")
        {
            Require(dropdown("priorityDropdown").options.Count == 3, "Three meaningful priority options");
            Func<List<ISoundAgent>> slots = () => Field<List<ISoundAgent>>(example, "agents");
            for (int index = 0; index < 2; index++)
            {
                await Click(button("playButton"), mouse);
                await Until(() => slots().Count == index + 1, 4000, "Fill slot " + index);
            }
            int old = slots()[0].SerialId;
            await Click(button("playButton"), mouse);
            await Task.Delay(400);
            Require(slots().Count == 2 && slots()[0].SerialId != old, "Same priority replaces oldest");
            await Click(toggle("protectToggle"), mouse);
            Require(text("outputText").text.Contains("保护同优先级：开启"), "Protection toggle state is readable");
            var ids = slots().Select(a => a.SerialId).ToArray();
            await Click(button("playButton"), mouse);
            await Task.Delay(400);
            Require(slots().Select(a => a.SerialId).SequenceEqual(ids), "Protected equal priority preserves both slots");
            Require(text("outputText").text.Contains("IgnoredDueToLowPriority"), "Expected rejection is visible");
            dropdown("priorityDropdown").value = 2;
            await Click(button("playButton"), mouse);
            await Task.Delay(400);
            Require(slots().Count(a => a.Priority == 20) == 1, "High priority replaces low despite protection");
            await Click(button("playButton"), mouse);
            await Task.Delay(400);
            Require(slots().All(a => a.Priority == 20), "High priority fills both slots");
            dropdown("priorityDropdown").value = 0;
            ids = slots().Select(a => a.SerialId).ToArray();
            await Click(button("playButton"), mouse);
            await Task.Delay(400);
            Require(slots().Select(a => a.SerialId).SequenceEqual(ids), "Low priority cannot replace high");
            await Screenshot("priority");
            await Click(button("clearButton"), mouse);
            await Until(() => slots().All(a => a.Length == 0), 3000, "Clear releases both agents");
        }
    }

    private static T Field<T>(object instance, string name) => (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance);

    private static async Task Until(Func<bool> condition, int timeout, string message)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(25);
        Require(condition(), message);
    }

    private static async Task Click(Selectable target, Mouse mouse)
    {
        await Until(() => target.interactable, 4000, "Enabled control " + target.name);
        Canvas.ForceUpdateCanvases();
        await Task.Delay(150);
        var rect = (RectTransform)target.transform;
        var scroll = target.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            scroll.StopMovement();
            Canvas.ForceUpdateCanvases();
            var local = scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            float overflow = scroll.content.rect.height - scroll.viewport.rect.height;
            float offset = scroll.content.anchoredPosition.y + scroll.viewport.rect.center.y - local.y;
            if (overflow > 0)
                scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(offset / overflow);
            Canvas.ForceUpdateCanvases();
            await Task.Delay(150);
        }
        bool received = false;
        UnityEngine.Events.UnityAction observed = () => received = true;
        var button = target as Button;
        var toggle = target as Toggle;
        bool oldValue = toggle != null && toggle.isOn;
        if (button != null)
            button.onClick.AddListener(observed);
        var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        try
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 0 });
            await Task.Delay(80);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
            await Task.Delay(80);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 0 });
            await Task.Delay(150);
            if (!(button != null ? received : toggle != null && toggle.isOn != oldValue))
            {
                await Screenshot("failed-click");
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
                throw new Exception("Native click " + target.name + " at " + position + "; raycast " + string.Join(", ", hits.Select(hit => hit.gameObject.name)));
            }
            Require(true, "Native click " + target.name);
        }
        finally { if (button != null) button.onClick.RemoveListener(observed); }
    }

    private static async Task Screenshot(string name)
    {
        Directory.CreateDirectory("Artifacts~/SoundExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/SoundExamples/" + name + ".png"));
        await Task.Delay(160);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
        assertions.Add(message);
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create())
            return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }

    private static async Task Play(bool enabled)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state =>
        {
            if (state == (enabled ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode))
                completion.TrySetResult(true);
        };
        EditorApplication.playModeStateChanged += handler;
        try
        {
            EditorApplication.isPlaying = enabled;
            if (await Task.WhenAny(completion.Task, Task.Delay(15000)) != completion.Task)
                throw new TimeoutException("Play transition");
            await Task.Delay(150);
        }
        finally { EditorApplication.playModeStateChanged -= handler; }
    }
}
