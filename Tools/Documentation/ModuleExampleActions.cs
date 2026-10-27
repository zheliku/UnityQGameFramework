using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.FileSystem;
using UnityQGameFramework.Core.Network;
using UnityQGameFramework.Core.Sound;
using UnityQGameFramework.Examples.Modules;
using UnityQGameFramework.Examples.Modules.TableData;
using UnityQGameFramework.Runtime;
using YooAsset;

/// <summary>
/// 检查模块示例的操作结果。
/// </summary>
public static class ModuleExampleActions
{
    private static MonoBehaviour example;

    private static T Field<T>(string name)
    {
        return (T)example.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(example);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static async Task Until(Func<bool> condition, string message, int milliseconds = 10000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(milliseconds);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(30);
        Require(condition(), message);
        await Task.Delay(80);
    }
    private static async Task Click(string field)
    {
        var button = Field<Button>(field);
        await Until(() => button.interactable, "Enabled button: " + field);
        button.onClick.Invoke();
        await Task.Delay(100);
    }

    /// <summary>
    /// 操作指定示例并检查模块状态。
    /// </summary>
    /// <param name="name">示例名称。</param>
    /// <param name="owner">示例控制器。</param>
    /// <returns>检查任务。</returns>
    public static async Task Exercise(string name, MonoBehaviour owner)
    {
        example = owner;
        switch (name)
        {
            case "LoadAddressRules":
                for (int mode = 0; mode < 3; mode++)
                {
                    Field<Dropdown>("ruleDropdown").value = mode;
                    await Click("loadButton");
                    await Until(() => Field<GameObject>("asset") != null, "String address resolves under each rule");
                    Require(Field<GameObject>("instance") != null, "Address load creates a scene instance");
                    await Click("releaseButton");
                    await Until(() => Field<GameObject>("asset") == null, "Address borrow returned to its original package");
                }
                break;
            case "LoadConfigAsset":
                await Click("loadButton");
                await Until(() => QGF.Config.HasConfig("Level.Name"), "TOML asset loaded");
                Require(Field<Text>("contextText").text.Contains("当前对象"), "Config UserData identifies the request owner");
                break;
            case "LoadCatalogExample":
                Field<Button>("loadButton").onClick.Invoke();
                Field<Button>("cancelButton").onClick.Invoke();
                await Task.Delay(500);
                Require(!QGF.DataTable.IsLoaded && !QGF.DataTable.IsLoading, "Cancelled request does not publish late tables");
                await Click("loadButton");
                await Until(() => QGF.DataTable.IsLoaded, "Catalog retry publishes tables");
                Require(QGF.DataTable.GetDataTable<TbItem>().Count > 0, "Generated table is available after load");
                await Click("unloadButton");
                Require(!QGF.DataTable.IsLoaded && QGF.DataTable.Count == 0, "Whole table set unloaded");
                break;
            case "FilterRowsExample":
                Field<Slider>("priceSlider").value = 0;
                Require(Field<Text>("rowsText").text.Split('\n').Length == 1, "Price filter excludes positive-price items");
                Field<Slider>("priceSlider").value = 100;
                Require(Field<Text>("rowsText").text.Split('\n').Length > 1, "Price filter includes matching items");
                break;
            case "TableRegistryExample":
                int count = QGF.DataTable.Count;
                TbItem originalTable = QGF.DataTable.GetDataTable<TbItem>();
                await Click("m_RegisterButton");
                Require(QGF.DataTable.Count == count + 1 && QGF.DataTable.HasDataTable<TbItem>("Preview"), "Named generated table registered");
                Require(ReferenceEquals(originalTable, QGF.DataTable.GetDataTable<TbItem>()), "Default table survives named registration");
                Require(ReferenceEquals(QGF.DataTable.GetDataTable<TbItem>("Preview"), QGF.DataTable.GetDataTable(typeof(TbItem), "Preview")), "Typed and untyped named lookups agree");
                await Click("m_ListButton");
                Require(Field<Text>("m_TablesText").text.Contains("Preview"), "Table metadata lists the named table");
                await Click("m_RemoveButton");
                Require(QGF.DataTable.Count == count && !QGF.DataTable.HasDataTable<TbItem>("Preview"), "Named removal leaves the default table");
                break;
            case "TableModesExample":
                await Click("m_AliasesButton");
                DRItemAlias alias = QGF.DataTable.GetDataTable<TbItemAlias>().GetDataRow("Potion");
                Require(ReferenceEquals(alias.ItemId_Ref, QGF.DataTable.GetDataTable<TbItem>().GetDataRow(1)), "Cross-table reference uses the registered row");
                await Click("m_IndexesButton");
                Require(QGF.DataTable.GetDataTable<TbStage>().GetDataRow(1, 2).RewardItemId_Ref.Id == 2, "Compound key and generated reference");
                Require(QGF.DataTable.GetDataTable<TbItemIndex>().GetByPrice(50).Id == 2, "Independent generated index");
                await Click("m_ModesButton");
                Require(QGF.DataTable.GetDataTable<TbRules>().Count == 1 && QGF.DataTable.GetDataTable<TbMessage>().Count == 2, "Singleton and unkeyed list");
                break;
            case "LoadResourcePackage":
                await Click("initializeButton");
                await Until(() => Field<bool>("initialized"), "Package initialized");
                await Click("loadButton");
                await Until(() => Field<GameObject>("instance") != null, "Package prefab instantiated");
                Require(Field<AssetHandle>("handle") != null && Field<AssetHandle>("handle").IsValid, "Package owns the explicit asset handle");
                await Click("releaseButton");
                await Until(() => Field<AssetHandle>("handle") == null, "Asset handle returned after instance destruction");
                Require(Field<GameObject>("instance") == null, "Scene instance destroyed");
                break;
            case "ReadBinaryResource":
                await Click("initializeButton");
                await Until(() => Field<bool>("initialized"), "Binary package initialized");
                await Click("readButton");
                byte[] original = File.ReadAllBytes("Assets/Examples/01_Modules/12_Resource/Common/Art/Archive.bytes");
                Require(Field<byte[]>("result").SequenceEqual(original.Skip(8).Take(16)), "Exact binary source segment");
                break;
            case "SoundGroupVolumeExample":
                await Until(() => Field<ISoundAgent>("agent") != null, "Music begins");
                Field<Slider>("groupVolumeSlider").value = .6f;
                Field<Slider>("voiceVolumeSlider").value = .3f;
                Require(Mathf.Approximately(Field<ISoundAgent>("agent").Volume, .18f), "Group volume multiplies voice volume");
                Field<Toggle>("groupMuteToggle").isOn = true;
                Require(Field<ISoundAgent>("agent").Mute, "Group mute reaches the voice");
                Field<Toggle>("groupMuteToggle").isOn = false;
                await Click("stopButton");
                Require(Field<ISoundAgent>("agent").Length == 0, "Music group stopped");
                await Click("playButton");
                await Until(() => Field<ISoundAgent>("agent")?.Length > 0, "Music can be replayed");
                break;
            case "CancelWebRequestExample":
                await Click("enqueueButton");
                await Until(() => QGF.WebRequest.WaitingTaskCount == 3, "Requests queue behind hold");
                await Click("cancelButton");
                await Until(() => QGF.WebRequest.WaitingTaskCount + QGF.WebRequest.WorkingAgentCount == 0, "Remaining requests complete");
                Require(string.Join(",", Field<List<string>>("order")) == "hold,high,other,low" && Field<int>("callbacks") == 3, "Cancel emits no result and preserves waiting priority");
                break;
            case "NetworkHeartbeat":
                Field<Slider>("intervalSlider").value = .4f;
                await Click("connectButton");
                await Until(() => Field<NetworkTextProtocol>("protocol").HeartBeatsReceived >= 2, "Heartbeat replies received");
                Field<Toggle>("replyToggle").isOn = false;
                await Until(() => !Field<INetworkChannel>("channel").Connected && Field<int>("misses") >= 3, "Business closes after three missed heartbeats");
                Field<Toggle>("replyToggle").isOn = true;
                int received = Field<NetworkTextProtocol>("protocol").HeartBeatsReceived;
                await Click("connectButton");
                await Until(() => Field<INetworkChannel>("channel").Connected && Field<NetworkTextProtocol>("protocol").HeartBeatsReceived > received, "Reconnect receives a fresh heartbeat");
                break;
            case "ObjectPoolPolicy":
                Field<Toggle>("protectKeep").isOn = true;
                await Click("createButton");
                await Click("releaseButton");
                Require(Field<UnityQGameFramework.Core.ObjectPool.IObjectPool<PolicyCubeItem>>("pool").Count == 1, "Locked Keep survives recycling");
                Field<Toggle>("protectKeep").isOn = false;
                await Click("releaseButton");
                Require(Field<UnityQGameFramework.Core.ObjectPool.IObjectPool<PolicyCubeItem>>("pool").Count == 0, "Unlocked Keep can be recycled");
                await Click("createButton");
                Require(Field<UnityQGameFramework.Core.ObjectPool.IObjectPool<PolicyCubeItem>>("pool").Count == 3, "Recycling example can be repeated");
                break;
            case "FsmStateData":
                Field<InputField>("jobsInput").text = "1";
                await Click("loadButton");
                await Click("runButton");
                await Until(() => Field<int>("lastBatch") == 1 && Field<Text>("jobsText").text.Contains("移除"), "State reads shared data, completes and removes it");
                break;
            case "NamedMachines":
                var right = Field<object>("right");
                await Click("destroyButton");
                Require(ReferenceEquals(right, Field<object>("right")) && Field<object>("left") == null, "Destroying Left preserves Right");
                await Click("recreateButton");
                Require(Field<object>("left") != null, "Named state machine recreated");
                break;
            case "ManageNodeChildren":
                QGF.DataNode.Clear();
                await Click("addButton");
                var inventory = QGF.DataNode.GetNode("Example/Inventory");
                Require(inventory != null && inventory.ChildCount == 1, "Add creates a child under Inventory");
                await Click("removeNameButton");
                Require(inventory.ChildCount == 0, "Name removal keeps the parent node");
                await Click("addButton");
                await Click("removeIndexButton");
                Require(inventory.ChildCount == 0, "Index removal removes the first child");
                await Click("addButton");
                await Click("clearNodeButton");
                Require(QGF.DataNode.GetNode("Example/Inventory") == inventory && inventory.ChildCount == 0,
                    "Clear removes child data but preserves Inventory");
                await Click("clearAllButton");
                Require(QGF.DataNode.Root.ChildCount == 0 && QGF.DataNode.GetNode("Example/Inventory") == null,
                    "Clear removes all root branches");
                await Click("addButton");
                Require(QGF.DataNode.GetNode("Example/Inventory")?.ChildCount == 1, "Adding after Clear recreates the path");
                break;
            case "EventDispatchOrder":
                await Click("compareButton");
                Require(Field<int>("immediate") == 1 && Field<int>("afterImmediate") == 2 && Field<int>("afterQueued") == 3,
                    "FireNow completes before returning and Fire returns before dispatch");
                await Until(() => Field<int>("queued") == 4, "Queued event is dispatched after the current call");
                Require(Field<int>("waiting") > 0, "The example keeps the submitted queue count as a snapshot");
                break;
            case "ProcedureEntryExample":
                await Until(() => QGF.Procedure.CurrentProcedure?.GetType().Name == "ExampleLaunch01", "Configured entry procedure starts");
                Require(Field<int>("Entered") == 1, "Configured procedure receives OnEnter once");
                float procedureTime = QGF.Procedure.CurrentProcedureTime;
                await Task.Delay(150);
                Require(QGF.Procedure.CurrentProcedureTime > procedureTime, "Current procedure time advances");
                break;
            case "ProcedureDataExample":
                Field<Dropdown>("levelChoice").value = 1;
                await Click("confirmButton");
                await Until(() => QGF.Procedure.CurrentProcedure?.GetType().Name == "ExampleLoad03", "Procedure receives the selected level");
                Require(Field<string>("LoadedLevel") == "沙漠" && Field<bool>("HasLevelData"), "Temporary data survives the procedure change");
                await Click("finishButton");
                await Until(() => QGF.Procedure.CurrentProcedure?.GetType().Name == "ExampleSelect03" && !Field<bool>("HasLevelData"),
                    "Removing the temporary value returns to selection");
                Require(Field<string>("LoadedLevel") == "沙漠", "Copied business data remains after removing its pooled wrapper");
                break;
            case "ReferenceOwnership":
                await Click("acquireButton");
                Require(Field<VarInt32>("local")?.Value == 7, "Local code owns the acquired wrapper");
                await Click("transferButton");
                Require(Field<VarInt32>("local") == null && QGF.DataNode.GetData<VarInt32>("Example/Owned")?.Value == 7,
                    "DataNode takes ownership when the local reference is cleared");
                await Click("removeButton");
                Require(QGF.DataNode.GetNode("Example/Owned") == null, "Removing the node releases its owned wrapper");
                await Click("acquireButton");
                await Click("releaseButton");
                Require(Field<VarInt32>("local") == null, "The local owner can release its wrapper directly");
                break;
            case "SaveSettingObject":
                bool hadPilot = QGF.Setting.HasSetting("Pilot");
                SaveSettingObject.Pilot savedPilot = hadPilot
                    ? QGF.Setting.GetObject("Pilot", new SaveSettingObject.Pilot()) : null;
                try
                {
                    QGF.Setting.RemoveSetting("Pilot");
                    QGF.Setting.Save();
                    Field<InputField>("nameInput").text = "QGF验证";
                    Field<Slider>("levelSlider").value = 7;
                    await Click("saveButton");
                    SaveSettingObject.Pilot storedPilot = QGF.Setting.GetObject("Pilot", new SaveSettingObject.Pilot());
                    Require(storedPilot.Name == "QGF验证" && storedPilot.Level == 7, "Save serializes the object and flushes the setting");
                    await Click("readButton");
                    var loadedPilot = Field<SaveSettingObject.Pilot>("loaded");
                    Require(loadedPilot.Name == "QGF验证" && !ReferenceEquals(loadedPilot, Field<SaveSettingObject.Pilot>("fallback")),
                        "Reading reconstructs a new object");
                    await Click("removeButton");
                    await Click("readButton");
                    Require(ReferenceEquals(Field<SaveSettingObject.Pilot>("loaded"), Field<SaveSettingObject.Pilot>("fallback")),
                        "A missing key returns the supplied fallback instance");
                }
                finally
                {
                    if (hadPilot)
                    {
                        QGF.Setting.SetObject("Pilot", savedPilot);
                    }
                    else
                    {
                        QGF.Setting.RemoveSetting("Pilot");
                    }
                    QGF.Setting.Save();
                }
                break;
            case "ReuseCubeEntity":
                await Until(() => QGF.Entity.GetEntity(1) != null && Field<int>("currentInstance") != 0, "Initial entity is shown");
                Field<Slider>("idleSlider").value = 0;
                Field<Toggle>("lockedToggle").isOn = false;
                int instanceId = Field<int>("currentInstance");
                await Click("recycleButton");
                await Until(() => !Field<bool>("recycling") && QGF.Entity.GetEntity(1) != null && Field<int>("previousInstance") == instanceId,
                    "Entity is hidden and shown again");
                Require(Field<int>("currentInstance") == instanceId, "Immediate re-show reuses the pooled GameObject");
                Field<Slider>("idleSlider").value = 1.5f;
                await Click("recycleButton");
                await Until(() => !Field<bool>("recycling") && QGF.Entity.GetEntity(1) != null && Field<int>("currentInstance") != instanceId,
                    "An unlocked idle entity expires before the next show", 5000);
                instanceId = Field<int>("currentInstance");
                Field<Toggle>("lockedToggle").isOn = true;
                await Click("recycleButton");
                await Until(() => !Field<bool>("recycling") && QGF.Entity.GetEntity(1) != null && Field<int>("previousInstance") == instanceId,
                    "Locked entity returns from the pool", 5000);
                Require(Field<int>("currentInstance") == instanceId, "An instance lock prevents idle expiry");
                break;
            case "ReopenWindowExample":
                await Until(() => QGF.UI.GetUIForm(Field<int>("serialId")) != null, "Initial window is open");
                Field<Slider>("idleSlider").value = 0;
                Field<Toggle>("lockedToggle").isOn = false;
                int serialId = Field<int>("serialId");
                int windowId = QGF.UI.GetUIForm(serialId).gameObject.GetInstanceID();
                await Click("reopenButton");
                await Until(() => Field<int>("serialId") != serialId && QGF.UI.GetUIForm(Field<int>("serialId")) != null,
                    "A new open request completes");
                Require(QGF.UI.GetUIForm(Field<int>("serialId")).gameObject.GetInstanceID() == windowId,
                    "Immediate reopen reuses the cached window instance");
                Field<Slider>("idleSlider").value = 1.2f;
                serialId = Field<int>("serialId");
                windowId = QGF.UI.GetUIForm(serialId).gameObject.GetInstanceID();
                await Click("reopenButton");
                await Until(() => Field<int>("serialId") != serialId && QGF.UI.GetUIForm(Field<int>("serialId")) != null,
                    "Window reopens after waiting", 5000);
                Require(QGF.UI.GetUIForm(Field<int>("serialId")).gameObject.GetInstanceID() != windowId,
                    "An unlocked expired window is recreated");
                Field<Toggle>("lockedToggle").isOn = true;
                Field<Slider>("idleSlider").value = 1.2f;
                serialId = Field<int>("serialId");
                windowId = QGF.UI.GetUIForm(serialId).gameObject.GetInstanceID();
                await Click("reopenButton");
                await Until(() => Field<int>("serialId") != serialId && QGF.UI.GetUIForm(Field<int>("serialId")) != null,
                    "Locked window reopens", 5000);
                Require(QGF.UI.GetUIForm(Field<int>("serialId")).gameObject.GetInstanceID() == windowId,
                    "An instance lock preserves an expired window");
                break;
            case "CoverWindowExample":
                int bottom = Field<int>("bottom");
                await Until(() => QGF.UI.GetUIForm(bottom) != null, "Bottom window is open");
                Field<Toggle>("pauseToggle").isOn = true;
                await Click("openButton");
                int top = Field<int>("top");
                await Until(() => QGF.UI.GetUIForm(top) != null, "Top window covers the bottom");
                Require(QGF.UI.GetUIGroup("Example").CurrentUIForm.SerialId == top, "The new form becomes the group's top window");
                await Click("refocusButton");
                Require(QGF.UI.GetUIGroup("Example").CurrentUIForm.SerialId == bottom, "Refocus brings the bottom window forward");
                await Click("closeButton");
                await Until(() => QGF.UI.GetUIForm(top) == null && QGF.UI.GetUIForm(bottom) != null, "Closing the top leaves the bottom window open");
                break;
            case "SwitchRoomsExample":
                await Until(() => Field<string>("current") == "Scene_SwitchRooms_BlueRoom", "Initial blue room is loaded");
                await Click("switchButton");
                await Until(() => Field<string>("current") == "Scene_SwitchRooms_OrangeRoom", "Old room unloads before the orange room loads", 15000);
                Require(QGF.Scene.SceneIsLoaded("Scene_SwitchRooms_OrangeRoom")
                    && !QGF.Scene.SceneIsLoaded("Scene_SwitchRooms_BlueRoom"), "Only the destination room remains loaded");
                break;
            case "DownloadProgressExample":
                await Click("downloadButton");
                await Until(() => Field<int>("serial") == 0, "Progress download finishes", 30000);
                string progressPath = Field<string>("path");
                Require(Field<bool>("matched") && File.ReadAllBytes(progressPath).SequenceEqual(Field<TextAsset>("payload").bytes),
                    "The final file matches the downloaded payload");
                Require(Field<long>("received") == Field<int>("totalLength"), "Progress reports cumulative bytes");
                break;
            case "CancelDownloadExample":
                Field<Dropdown>("cancelMode").value = 0;
                await Click("downloadButton");
                await Until(() => Field<long>("received") > 0, "Download has received bytes");
                await Click("cancelButton");
                string cancelPath = Field<string>("path");
                Require(File.Exists(cancelPath + ".download"), "Cancel keeps the partial file for resuming");
                await Click("downloadButton");
                await Until(() => Field<int>("serial") == 0, "Resumed download finishes", 30000);
                long resumedFrom = Field<long>("resumedFrom");
                object downloadServer = Field<object>("server");
                long lastRange = (long)downloadServer.GetType().GetProperty("LastRange").GetValue(downloadServer);
                Require(resumedFrom > 0 && lastRange == resumedFrom,
                    "The next request resumes from the partial file length");
                Require(Field<bool>("matched"), "Resumed output matches the source payload");
                Field<Dropdown>("cancelMode").value = 1;
                await Click("downloadButton");
                await Until(() => Field<long>("received") > 0 && Field<int>("serial") != 0, "Second download starts");
                await Click("cancelButton");
                Require(!File.Exists(cancelPath + ".download"), "Cancel-and-clear removes the partial file");
                break;
            case "EchoNetworkExample":
                await Click("connectButton");
                await Until(() => Field<INetworkChannel>("channel")?.Connected == true, "Loopback channel connects", 15000);
                await Click("sendButton");
                await Until(() => Field<int>("received") == 2, "Both messages receive replies");
                var responses = Field<List<string>>("responses");
                Require(responses[0] == Field<InputField>("firstInput").text
                    && responses[1] == Field<InputField>("secondInput").text, "TCP framing preserves both message boundaries");
                await Click("noticeButton");
                await Until(() => Field<int>("notices") == 1, "Unregistered message reaches the default handler");
                break;
            case "SpatialSoundExample":
                await Until(() => Field<ISoundAgent>("agent")?.Length > 0, "Spatial sound begins", 15000);
                ISoundAgent soundAgent = Field<ISoundAgent>("agent");
                Field<Slider>("distanceSlider").value = 10;
                Field<Slider>("blendSlider").value = .75f;
                Field<Slider>("panSlider").value = -.5f;
                Require(Mathf.Approximately(soundAgent.MaxDistance, 10)
                    && Mathf.Approximately(soundAgent.SpatialBlend, .75f)
                    && Mathf.Approximately(soundAgent.PanStereo, -.5f), "Distance, spatial blend and pan update the active agent");
                Vector3 previousPosition = ((SoundAgentHelperBase)soundAgent.Helper).transform.position;
                Field<Slider>("positionSlider").value = .8f;
                Require(((SoundAgentHelperBase)soundAgent.Helper).transform.position == previousPosition,
                    "Moving the source marker does not move an already submitted sound");
                int previousSound = Field<int>("serialId");
                await Click("playButton");
                await Until(() => Field<int>("serialId") != previousSound && Field<ISoundAgent>("agent")?.SerialId == Field<int>("serialId"),
                    "Replaying applies the new source position", 15000);
                Require(((SoundAgentHelperBase)Field<ISoundAgent>("agent").Helper).transform.position
                    == Field<Transform>("sourceMarker").position, "The new sound uses the moved marker position");
                break;
            case "DebuggerWindowLifecycle":
                Field<Slider>("initialValueSlider").value = 5;
                await Click("registerButton");
                object counterWindow = Field<object>("window");
                Require(ReferenceEquals(QGF.Debugger.GetDebuggerWindow("Examples/Counter"), counterWindow)
                    && (int)counterWindow.GetType().GetProperty("Count").GetValue(counterWindow) == 5,
                    "Registration passes the initial value and returns the same window");
                await Click("incrementButton");
                Require((int)counterWindow.GetType().GetProperty("Count").GetValue(counterWindow) == 6, "The example button mutates the registered window");
                Require(QGF.Debugger.SelectDebuggerWindow("Information/Other/Time"), "Selecting the built-in time debugger window");
                float stoppedTime = (float)counterWindow.GetType().GetProperty("ActiveSeconds").GetValue(counterWindow);
                await Task.Delay(250);
                Require((float)counterWindow.GetType().GetProperty("ActiveSeconds").GetValue(counterWindow) - stoppedTime < .1f,
                    "An unselected debugger window is no longer updated");
                Require(QGF.Debugger.SelectDebuggerWindow("Examples/Counter"), "Selecting the registered debugger window");
                await Click("unregisterButton");
                Require(QGF.Debugger.GetDebuggerWindow("Examples/Counter") == null
                    && (int)counterWindow.GetType().GetProperty("ShutdownCalls").GetValue(counterWindow) == 1,
                    "Unregister calls Shutdown and removes the path");
                break;
            case "UpdateRemoteResources":
                ResourceFixtureManifest fixture = ResourceExamplePaths.Fixture;
                Require(fixture != null && Directory.Exists(fixture.RemoteFolder), "Build the remote resource fixture from the UpdateRemoteResources Inspector before the remote resource example");
                await Click("versionButton");
                await Until(() => Field<ResourcePackage>("package")?.PackageValid == true, "Host package initializes", 15000);
                await Click("updateButton");
                await Until(() => Field<ResourceDownloaderOperation>("downloader")?.IsDone == true, "Host bundle download finishes", 30000);
                Require(Field<ResourceDownloaderOperation>("downloader").Status == EOperationStatus.Succeeded,
                    "The selected asset tags are downloaded");
                await Click("loadButton");
                await Until(() => Field<GameObject>("asset") != null, "Address loads from the Host package");
                Require(Field<GameObject>("instance") != null, "Loaded remote asset creates an instance");
                await Click("checkButton");
                await Until(() => Field<GameObject>("asset") == null, "The asset borrow is returned");
                break;
            case "TextAndPaths":
                Field<InputField>("nameInput").text = "QGF";
                Field<InputField>("countInput").text = "3";
                Field<InputField>("pathInput").text = "folder\\item";
                await Click("formatButton");
                Require(Field<Text>("resultText").text.Contains("QGF") && Field<Text>("resultText").text.Contains("30")
                    && Field<Text>("resultText").text.Contains("folder/item"), "Formatting and path normalization use the supplied values");
                await Click("directoryButton");
                string ownedDirectory = Field<string>("directory");
                Require(!Directory.Exists(Path.Combine(ownedDirectory, "Empty"))
                    && File.Exists(Field<string>("ownedFile")), "Empty folders are removed while folders with files remain");
                await Click("cleanupButton");
                Require(!Directory.Exists(ownedDirectory), "Cleanup removes only the example's temporary files");
                break;
            case "ConvertBytes":
                Field<InputField>("valueInput").text = "-12345";
                await Click("convertButton");
                Require(Field<Text>("resultText").text.Contains("51-47") && Field<Text>("resultText").text.Contains("-12345"),
                    "Converter writes a header and restores the Int32 at the matching offset");
                break;
            case "DrawRandom":
                Field<InputField>("seedInput").text = "1729";
                await Click("seedButton");
                string firstDraw = Field<Text>("resultText").text;
                await Click("seedButton");
                Require(Field<Text>("resultText").text == firstDraw, "Applying the same seed reproduces the same random draw");
                break;
            case "ConvertStructure":
                Field<InputField>("idInput").text = "17";
                Field<InputField>("speedInput").text = "1.25";
                await Click("convertButton");
                Require(Field<Text>("resultText").text.Contains("Id 17") && Field<Text>("resultText").text.Contains("Speed 1.25"),
                    "Structure serialization round-trips both fields");
                await Click("reserveButton");
                Require(Utility.Marshal.CachedHGlobalSize >= 8192, "Reserve grows the shared marshal cache");
                await Click("freeButton");
                Require(Utility.Marshal.CachedHGlobalSize == 0, "Free releases the shared marshal cache");
                break;
            case "ReadFileSegments":
                Field<Slider>("offsetSlider").value = 14;
                Field<Slider>("lengthSlider").value = 8;
                await Click("readButton");
                Require(Field<Text>("resultText").text.EndsWith("EF"), "File segment clips at source end");
                break;
            default:
                throw new NotSupportedException("No behavior check is registered for module example: " + name);

        }
    }
}
