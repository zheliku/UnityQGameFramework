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
            case "LoadConfigAsset":
                await Click("loadButton");
                await Until(() => QGF.Config.HasConfig("Level.Name"), "TOML asset loaded");
                Require(Field<Text>("contextText").text.Contains("当前对象"), "Config UserData identifies the request owner");
                break;
            case "LoadCatalogExample":
                await Click("loadButton");
                await Click("cancelButton");
                await Task.Delay(2300);
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
            case "LoadResourcePackage":
                await Click("initializeButton");
                await Until(() => Field<bool>("initialized"), "Package initialized");
                await Click("loadButton");
                await Until(() => Field<GameObject>("instance") != null, "Package prefab instantiated");
                Require(Field<GameObject>("asset") != Field<GameObject>("instance"), "Prefab asset differs from scene instance");
                await Click("releaseButton");
                await Until(() => Field<GameObject>("asset") == null, "Asset reference returned after instance destruction");
                Require(Field<GameObject>("instance") == null, "Scene instance destroyed");
                break;
            case "ReadBinaryResource":
                await Click("initializeButton");
                await Until(() => Field<bool>("initialized"), "Binary package initialized");
                await Click("readButton");
                byte[] original = File.ReadAllBytes(ResourceExamplePaths.Fixture.FileSystemAssetName);
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
            case "ReadFileSegments":
                Field<Slider>("offsetSlider").value = 14;
                Field<Slider>("lengthSlider").value = 8;
                await Click("readButton");
                Require(Field<Text>("resultText").text.EndsWith("EF"), "File segment clips at source end");
                break;

        }
    }
}
