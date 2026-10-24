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
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Network;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.Modules;

// 开发验收工具，不随 Sample 导入。按钮和 Toggle 使用输入系统鼠标事件。
public static class ValidateNetworkExamples
{
    private static readonly List<string> assertions = new List<string>();

    public static Task<object> Run() => RunOnly(null);

    public static async Task<object> RunOnly(string[] names)
    {
        if (!Application.dataPath.Contains("Artifacts~")) throw new Exception("Use the isolated validation project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(index => UnityEngine.SceneManagement.SceneManager.GetSceneAt(index).isDirty))
            throw new Exception("Unsaved scene.");
        assertions.Clear();
        var layouts = JObject.Parse(File.ReadAllText("Tools/Documentation/ugui-layouts.json"));
        var cases = layouts.Properties().Where(pair => new[] { "20_Network" }
            .Any(folder => ((string)pair.Value["script"]).StartsWith("Assets/Examples/01_Modules/" + folder + "/")))
            .Select(pair => new { name = pair.Name, layout = pair.Value, mode = 0 }).ToList();
        if (names != null) cases = cases.Where(test => names.Contains(test.name)).ToList();
        var settings = new[] { "ProjectSettings", "EditorBuildSettings", "TagManager", "GraphicsSettings", "QualitySettings" }
            .ToDictionary(name => "ProjectSettings/" + name + ".asset", name => Hash("ProjectSettings/" + name + ".asset"));
        bool optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        var options = EditorSettings.enterPlayModeOptions;
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
                string anchor = AssetDatabase.GUIDToAssetPath("8d5ecf40d41d5cbbb278514669ef85eb");
                if (!anchor.StartsWith("Assets/", StringComparison.Ordinal)) throw new Exception("Imported Network sample not found.");
                string moduleRoot = Path.GetDirectoryName(anchor).Replace('\\', '/');
                string script = moduleRoot + sourcePath.Substring("Assets/Examples/01_Modules".Length);
                EditorSceneManager.OpenScene(script.Substring(0, script.Length - 3) + ".unity");
                await Play(true);
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                foreach (var pointer in InputSystem.devices.OfType<UnityEngine.InputSystem.Pointer>().Where(device => device != mouse && device.enabled).ToArray())
                {
                    disabled.Add(pointer);
                    InputSystem.DisableDevice(pointer);
                }
                await Task.Delay(550);
                var type = typeof(ConnectNetworkExample).Assembly.GetType("UnityQGameFramework.Examples.Modules." + test.name)
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
                Require(GameEntry.GetComponent<NetworkComponent>() == null, "No retained network component after stop");
                Require(errors.Count == 0, test.name + " runtime errors: " + string.Join("\n", errors));
                completed.Add(new { example = test.name, scene = script.Substring(0, script.Length - 3) + ".unity", startMode = test.mode, assertions = assertions.Skip(start).ToArray() });
            }
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) await Play(false);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            foreach (var device in disabled.Where(device => device.added && !device.enabled)) InputSystem.EnableDevice(device);
            inputSettings.backgroundBehavior = background;
            inputSettings.editorInputBehaviorInPlayMode = editorInput;
            Application.logMessageReceived -= capture;
            EditorSettings.enterPlayModeOptionsEnabled = optionsEnabled;
            EditorSettings.enterPlayModeOptions = options;
            if (setup.Any(scene => scene.isActive && scene.isLoaded && !string.IsNullOrEmpty(scene.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        Require(settings.All(item => Hash(item.Key) == item.Value), "Host settings unchanged");
        var result = new { passed = true, unity = Application.unityVersion, nativeButtonsAndToggles = true,
            modeAndTextSetup = "Dropdown.value and InputField.text set by the test; actions use native mouse clicks",
            settingsUnchanged = true, scenes = completed, assertionCount = assertions.Count };
        Directory.CreateDirectory("Artifacts~/NetworkExamples");
        string report = names == null ? "network-check.json" : string.Join("-", names) + "-check.json";
        File.WriteAllText("Artifacts~/NetworkExamples/" + report, JsonConvert.SerializeObject(result, Formatting.Indented));
        return new { result.passed, result.unity, scenes = completed.Count, result.assertionCount, result.settingsUnchanged };
    }


    private static async Task Check(string name, int mode, MonoBehaviour example, SerializedObject data, Mouse mouse)
    {
        Func<string, Button> button = key => (Button)data.FindProperty(key).objectReferenceValue;
        Func<string, Dropdown> dropdown = key => (Dropdown)data.FindProperty(key).objectReferenceValue;
        Func<string, Toggle> toggle = key => (Toggle)data.FindProperty(key).objectReferenceValue;
        Func<string, Text> text = key => (Text)data.FindProperty(key).objectReferenceValue;
        var server = Field<NetworkEchoServer>(example, "server");
        Require(QGF.Network.NetworkChannelCount == 0, "No implicit connections or registered channels");
        if (name == "ConnectNetworkExample")
        {
            for (int service = 0; service < 2; service++)
            {
                dropdown("serviceMode").value = service;
                await Click(button("createButton"), mouse);
                var channel = Field<INetworkChannel>(example, "channel");
                Require(channel != null && !channel.Connected && channel.Socket == null, "Create registers without socket " + service);
                await Until(() => text("stateText").text.Contains("主通道：未连接")
                    && text("stateText").text.Contains("Socket：未创建"), 1000, "Created channel state is readable");
                Require(ReferenceEquals(channel,QGF.Network.GetNetworkChannel("Example.Main")), "Name query is same instance");
                if (service == 0) await Click(button("spareButton"), mouse);
                Require(QGF.Network.NetworkChannelCount == 2 && !QGF.Network.GetNetworkChannel("Example.Spare").Connected, "Spare counted despite no connection");
                for (int i = 0; i < 3; i++)
                {
                    dropdown("queryMode").value = i;
                    await Task.Delay(80);
                    Require(Field<List<INetworkChannel>>(example,"channels").Count == (i == 0 ? 1 : 2), "Name/array/List query " + i);
                }
                dropdown("connectMode").value = 0;
                await Click(button("connectButton"),mouse);
                await Until(() => channel.Connected && text("stateText").text.Contains("上下文：未传入"),4000,"Connect without context event");
                Require(channel.ServiceType == (service == 0 ? ServiceType.Tcp : ServiceType.TcpWithSyncReceive) && channel.AddressFamily == UnityQGameFramework.Core.Network.AddressFamily.IPv4, "Actual service and IPv4 socket " + service);
                await Click(button("closeButton"),mouse);
                await Until(() => !channel.Connected,3000,"Manual close");
                Require(QGF.Network.HasNetworkChannel("Example.Main") && channel.Socket == null,"Close leaves registration, disposes socket");
                dropdown("connectMode").value = 1;
                await Click(button("connectButton"),mouse);
                await Until(() => channel.Connected && text("stateText").text.Contains("上下文：当前组件"),4000,"Reconnect same channel with context");
                int oldClosed = Field<int>(example,"closedEvents");
                server.DisconnectClients();
                await Until(() => !channel.Connected && Field<int>(example,"closedEvents") > oldClosed,3000,"Remote FIN recognized " + service);
                await Click(button("connectButton"),mouse);
                await Until(() => channel.Connected,4000,"Reconnect after peer disconnect");
                if (service == 0) await Screenshot("network-lifecycle");
                await Click(button("destroyButton"),mouse);
                Require(!QGF.Network.HasNetworkChannel("Example.Main") && QGF.Network.NetworkChannelCount == 1 && !channel.Connected,"Destroy removes main, retains spare " + service);
            }
        }
        else if (name == "EchoNetworkExample")
        {
            var first = (InputField)data.FindProperty("firstInput").objectReferenceValue;
            var second = (InputField)data.FindProperty("secondInput").objectReferenceValue;
            for (int service = 0; service < 2; service++)
            {
                dropdown("serviceMode").value = service;
                await Click(button("connectButton"),mouse);
                var channel = Field<INetworkChannel>(example,"channel");
                await Until(() => channel.Connected && !Field<bool>(example,"connecting"),4000,"Echo connected " + service);
                foreach (bool split in new[] { true,false })
                {
                    if (toggle("splitToggle").isOn != split) await Click(toggle("splitToggle"),mouse);
                    first.text = "你好 TCP + &= ?";
                    second.text = split ? "第二份独立消息" : "";
                    int sent = channel.SentPacketCount;
                    int received = channel.ReceivedPacketCount;
                    await Click(button("sendButton"),mouse);
                    await Until(() => Field<int>(example,"received") == 2,4000,"Two real text responses " + service + "/" + split);
                    Require(Field<List<string>>(example,"responses").SequenceEqual(new[] {first.text,second.text}),"UTF-8 and empty messages preserved across TCP reads");
                    await Until(() => channel.SentPacketCount == sent + 2 && channel.ReceivedPacketCount == received + 2,3000,"Counters track messages, not batched send callbacks");
                    Require(channel.SendPacketCount == 0 && channel.ReceivePacketCount == 0,"Queues drain after dispatch");
                }
                await Click(button("noticeButton"),mouse);
                await Until(() => Field<int>(example,"notices") == 1,3000,"Unregistered Id routes to default handler");
                Require(Field<List<string>>(example,"responses").Last().Contains("默认处理器"),"Default notification shown");
                Require(UsedPackets() == 0,"Received packets returned to reference pool");
                if (service == 0) await Screenshot("network-echo");
            }
        }
        else if (name == "NetworkHeartbeat")
        {
            var interval = (Slider)data.FindProperty("intervalSlider").objectReferenceValue;
            interval.value = .4f;
            await Click(button("connectButton"),mouse);
            var channel = Field<INetworkChannel>(example,"channel");
            var protocol = Field<NetworkTextProtocol>(example,"protocol");
            await Until(() => protocol.HeartBeatsReceived >= 2,4000,"Two genuine heartbeat responses");
            await Until(() => text("counterText").text.Contains("连接：已连接"), 1000, "Heartbeat connection state is readable");
            Require(channel.Connected && Field<int>(example,"misses") == 0
                && channel.ResetHeartBeatElapseSecondsWhenReceivePacket,
                "Healthy connection resets elapsed time when packets arrive");
            await Click(toggle("replyToggle"),mouse);
            Require(!server.ReplyHeartBeats,"Server stops replying, TCP remains established");
            await Until(() => Field<int>(example,"misses") >= 2,5000,"Real missed responses produce notifications");
            await Screenshot("network-heartbeat");
            await Until(() => !channel.Connected && Field<int>(example,"misses") >= 3,5000,
                "Business closes after three consecutive misses");
            await Click(toggle("replyToggle"),mouse);
            await Click(button("connectButton"),mouse);
            await Until(() => channel.Connected && protocol.HeartBeatsReceived >= 1,3000,"Same channel reconnects after heartbeat closure");
            Require(UsedPackets() == 0,"Heartbeat responses do not retain pooled text packets");
        }
        else if (name == "NetworkFailure")
        {
            for (int service = 0; service < 2; service++)
            {
                dropdown("serviceMode").value = service;
                for (int failure = 0; failure < 6; failure++)
                {
                    dropdown("failureMode").value = failure;
                    await Click(button("connectButton"),mouse);
                    var channel = Field<INetworkChannel>(example,"channel");
                    await Until(() => failure < 3 ? Field<int>(example,"errors") > 0 : failure == 3 ? Field<int>(example,"customErrors") > 0 : failure == 4 ? Field<int>(example,"closes") > 0 : Field<string>(example,"state").Contains("恢复正常"),5000,"Failure or recovery " + service + "/" + failure);
                    string connectionState = failure == 3 || failure == 5 ? "连接：已连接" : "连接：已断开";
                    await Until(() => text("stateText").text.Contains(connectionState), 1000, "Failure connection state is readable " + service + "/" + failure);
                    if (failure < 3)
                    {
                        string expected = failure == 0 ? "ConnectError" : failure == 1 ? "DeserializePacketHeaderError" : "DeserializePacketError";
                        Require(Field<string>(example,"detail").Contains(expected) && !channel.Connected,"Correct fatal error and explicit close: " + expected);
                    }
                    if (failure == 3) Require(channel.Connected && Field<int>(example,"errors") == 0 && Field<string>(example,"detail").Contains("服务器拒绝"),"Protocol custom error is separate and retains connection");
                    if (failure == 4) Require(!channel.Connected && Field<int>(example,"errors") == 0,"Peer close is Closed, not protocol error " + service);
                    if (failure == 5) Require(channel.Connected && Field<string>(example,"detail") == "恢复后的业务消息","Normal retry recovers exact data " + service);
                    if (failure == 5)
                    {
                        await Click(button("connectButton"),mouse);
                        await Until(() => Field<string>(example,"state").Contains("恢复正常"),4000,"Repeat normal reconnect");
                        Require(ReferenceEquals(channel,Field<INetworkChannel>(example,"channel")) && Field<int>(example,"closes") == 0,"Old manual Close notification does not overwrite new attempt");
                    }
                    Require(UsedPackets() == 0,"No received packet retained after outcome " + failure);
                    if (service == 0 && failure == 2) await Screenshot("network-failure");
                }
            }
        }
    }

    private static int UsedPackets() => ReferencePool.GetAllReferencePoolInfos().Where(p => p.Type == typeof(NetworkTextPacket)).Sum(p => p.UsingReferenceCount);

    private static T Field<T>(object instance, string name) => (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance);

    private static async Task Until(Func<bool> condition, int timeout, string message)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(25);
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
            if (overflow > 0) scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(offset / overflow);
            Canvas.ForceUpdateCanvases();
            await Task.Delay(150);
        }
        bool received = false;
        UnityEngine.Events.UnityAction observed = () => received = true;
        var button = target as Button;
        var toggle = target as Toggle;
        bool oldValue = toggle != null && toggle.isOn;
        if (button != null) button.onClick.AddListener(observed);
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
        Directory.CreateDirectory("Artifacts~/NetworkExamples");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Artifacts~/NetworkExamples/" + name + ".png"));
        await Task.Delay(160);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        assertions.Add(message);
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }

    private static async Task Play(bool enabled)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state =>
        {
            if (state == (enabled ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode)) completion.TrySetResult(true);
        };
        EditorApplication.playModeStateChanged += handler;
        try
        {
            EditorApplication.isPlaying = enabled;
            if (await Task.WhenAny(completion.Task, Task.Delay(15000)) != completion.Task) throw new TimeoutException("Play transition");
            await Task.Delay(150);
        }
        finally { EditorApplication.playModeStateChanged -= handler; }
    }
}
