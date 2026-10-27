// Concatenate after Artifacts~/Documentation/CompleteExamples.cs and the
// generated Item fixture, then invoke DocumentationExamplesSmoke.Run with Unity CLI.
// This runs outside Play Mode without importing scripts or changing a scene.
namespace DocumentationVerification
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;
    using UnityQGameFramework.Core;
    using UnityQGameFramework.Core.Config;
    using UnityQGameFramework.Core.DataNode;
    using UnityQGameFramework.Core.DataTable;
    using UnityQGameFramework.Core.Event;
    using UnityQGameFramework.Core.FileSystem;
    using UnityQGameFramework.Core.Fsm;
    using UnityQGameFramework.Core.Localization;
    using UnityQGameFramework.Core.Network;
    using UnityQGameFramework.Core.ObjectPool;
    using UnityQGameFramework.Core.Procedure;
    using UnityQGameFramework.Luban;
    using UnityQGameFramework.Runtime;

    public static class DocumentationExamplesSmoke
    {
        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static Type Example(string name) => Assembly.GetExecutingAssembly().GetTypes()
            .Single(t => t.Name == name);

        public static object Run()
        {
            Require(!Application.isPlaying, "Run this check outside Play Mode.");
            var cache = (IDictionary)typeof(FrameworkEntry).GetField("s_ModuleCache",
                BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Require(cache.Count == 0, "An existing Core runtime must not be replaced.");
            var checks = new List<string>();
            try
            {
                var events = FrameworkEntry.GetModule<IEventManager>();
                Require(ReferenceEquals(events, FrameworkEntry.GetModule<IEventManager>()), "Module identity");
                var argsType = Example("GoldAddedEventArgs");
                int eventId = (int)argsType.GetField("EventId").GetValue(null);
                var create = argsType.GetMethod("Create");
                int sum = 0;
                EventHandler<GameEventArgs> handler = (sender, e) =>
                    sum += (int)argsType.GetProperty("Amount").GetValue(e);
                events.Subscribe(eventId, handler);
                var queued = (GameEventArgs)create.Invoke(null, new object[] { 20 });
                events.Fire(null, queued);
                Require(sum == 0, "Fire must queue");
                FrameworkEntry.Update(0f, 0f);
                Require(sum == 20 && (int)argsType.GetProperty("Amount").GetValue(queued) == 0,
                    "Queued event delivery and reference clear");
                events.FireNow(null, (GameEventArgs)create.Invoke(null, new object[] { 5 }));
                Require(sum == 25, "FireNow must be synchronous");
                events.Unsubscribe(eventId, handler);
                checks.Add("Module identity; documented event Create/Fire/FireNow/clear");

                var fsm = FrameworkEntry.GetModule<IFsmManager>();
                var procedures = FrameworkEntry.GetModule<IProcedureManager>();
                Type startup = Example("GuideStartup"), menu = Example("GuideMenu");
                procedures.Initialize(fsm, (ProcedureBase)Activator.CreateInstance(startup),
                    (ProcedureBase)Activator.CreateInstance(menu));
                procedures.StartProcedure(startup);
                FrameworkEntry.Update(1f, 1f);
                FrameworkEntry.Update(1f, 1f);
                Require(procedures.CurrentProcedure.GetType() == startup, "Procedure changed too soon");
                FrameworkEntry.Update(1f, 1f);
                Require(procedures.CurrentProcedure.GetType() == menu, "Documented three-second transition");
                checks.Add("Actual GuideStartup/GuideMenu and Fsm-driven transition");

                var nodes = FrameworkEntry.GetModule<IDataNodeManager>();
                nodes.SetData<VarInt32>("Guide/Battle/Score", 42);
                Require(nodes.GetData<VarInt32>("Guide.Battle.Score").Value == 42, "DataNode paths");
                nodes.RemoveNode("Guide");
                Require(nodes.GetNode("Guide/Battle") == null, "DataNode subtree removal");
                checks.Add("DataNode paths, variable ownership and subtree removal");

                var config = FrameworkEntry.GetModule<IConfigManager>();
                TomlConfigParser.ParseAndAdd(config, "[Guide]\nCount=3\nEnabled=true\nName=\"Pilot\"\nSpeed=1.5\n");
                Require(config.GetInt("Guide.Count") == 3 && config.GetBool("Guide.Enabled")
                    && config.GetString("Guide.Name") == "Pilot" && config.GetFloat("Guide.Speed") == 1.5f,
                    "TOML scalar flattening");
                try
                {
                    TomlConfigParser.ParseAndAdd(config, "[Guide]\nFresh=5\nCount=9\n");
                    throw new Exception("Duplicate config key was accepted");
                }
                catch (InvalidOperationException) { }
                Require(!config.HasConfig("Guide.Fresh") && config.GetInt("Guide.Count") == 3,
                    "Failed config parse left partial values");
                checks.Add("TOML Config scalar types and duplicate rollback");

                var localization = FrameworkEntry.GetModule<ILocalizationManager>();
                string dictionary = "[ChineseSimplified.Game]\nName=\"我的游戏\"\n"
                    + "[ChineseSimplified.Reward]\nGold=\"获得 {0} 金币\"\n"
                    + "[English.Game]\nName=\"My Game\"\n";
                localization.Language = Language.ChineseSimplified;
                TomlLocalizationParser.ParseAndAdd(localization, dictionary);
                Require(localization.GetString("Game.Name") == "我的游戏"
                    && localization.GetString("Reward.Gold", 25) == "获得 25 金币", "Localization format");
                localization.RemoveAllRawStrings();
                localization.Language = Language.English;
                TomlLocalizationParser.ParseAndAdd(localization, dictionary);
                Require(localization.GetString("Game.Name") == "My Game"
                    && localization.GetString("missing") == "<NoKey>missing", "Language switch/missing key");
                checks.Add("TOML language selection, formatting and reload");

                var settings = new DefaultSetting();
                settings.SetInt("Guide.LaunchCount", 7);
                settings.SetString("Guide.Name", "Pilot");
                var serializer = new DefaultSettingSerializer();
                serializer.RegisterSerializeCallback(0, (stream, value) =>
                {
                    value.Serialize(stream);
                    return true;
                });
                serializer.RegisterDeserializeCallback(0, stream =>
                {
                    var restored = new DefaultSetting();
                    restored.Deserialize(stream);
                    return restored;
                });
                byte[] savedSettings;
                using (var stream = new MemoryStream())
                {
                    Require(serializer.Serialize(stream, settings), "Setting serialize");
                    savedSettings = stream.ToArray();
                }
                using (var stream = new MemoryStream(savedSettings))
                {
                    var restored = serializer.Deserialize(stream);
                    Require(restored.GetInt("Guide.LaunchCount") == 7
                        && restored.GetString("Guide.Name") == "Pilot", "Setting roundtrip");
                }
                checks.Add("Default Setting serializer roundtrip (memory; no player preferences written)");

                var pools = FrameworkEntry.GetModule<IObjectPoolManager>();
                var pool = pools.CreateSingleSpawnObjectPool<ProbePoolItem>("Guide.Probe");
                var item = ReferencePool.Acquire<ProbePoolItem>();
                item.Setup();
                pool.Register(item, false);
                Require(ReferenceEquals(pool.Spawn(), item) && item.Spawned == 1, "Pool spawn");
                pool.Unspawn(item);
                Require(item.Unspawned == 1, "Pool unspawn");
                pool.ReleaseAllUnused();
                Require(pool.Count == 0 && ProbePoolItem.Released == 1, "Pool final release");
                pools.DestroyObjectPool(pool);
                checks.Add("ObjectPool register/spawn/unspawn/final release");

                var files = FrameworkEntry.GetModule<IFileSystemManager>();
                files.SetFileSystemHelper(new DiskHelper());
                string path = Path.GetFullPath("Artifacts~/Documentation/files-" + Guid.NewGuid().ToString("N") + ".dat");
                var container = files.CreateFileSystem(path, FileSystemAccess.ReadWrite, 16, 64);
                Require(container.WriteFile("player/name.txt", System.Text.Encoding.UTF8.GetBytes("QGF Player")),
                    "FileSystem write");
                files.DestroyFileSystem(container, false);
                container = files.LoadFileSystem(path, FileSystemAccess.Read);
                Require(System.Text.Encoding.UTF8.GetString(container.ReadFile("player/name.txt")) == "QGF Player",
                    "FileSystem reopen");
                files.DestroyFileSystem(container, false);
                checks.Add("FileSystem container write/close/reopen/read");

                var tables = ((UnityQGameFramework.Runtime.IDataTableLoader)new UnityQGameFramework.Config.Tables()).Load(name => new ByteBuf(
                    File.ReadAllBytes("Artifacts~/Documentation/ItemFixture/Data/" + name + ".bytes")));
                var dataTables = FrameworkEntry.GetModule<IDataTableManager>();
                dataTables.RegisterDataTables(tables);
                var view = dataTables.GetDataTable<UnityQGameFramework.Config.TbItem>();
                Require(view.Count == 3 && view.GetDataRow(1).Price == 20
                    && view.GetDataRow(1).Name == "回复药" && !view.GetDataRow(1).IsFree, "Excel example data");
                Require(view.GetDataRows(row => row.Price <= 20).Length == 2
                    && view.GetDataRows((a, b) => a.Price.CompareTo(b.Price))[0].Id == 3, "Table filtering/sorting");
                Require(ReferenceEquals(view.GetDataRow(1), view.DataMap[1]), "Shared row identity");
                Require(view.GetDataRow(999) == null && view.GetOrDefault(999) == null, "Missing row semantics");
                checks.Add("Official Excel generation; Price partial; native/view identity, filters and sorting");

                var networkHelper = (INetworkChannelHelper)Activator.CreateInstance(Example("TextChannelHelper"));
                var sent = (Packet)Activator.CreateInstance(Example("TextPacket"));
                sent.GetType().GetProperty("Text").SetValue(sent, "hello QGF 中文");
                using (var stream = new MemoryStream())
                {
                    Require(networkHelper.Serialize(sent, stream), "Packet serialization");
                    stream.Position = 0;
                    var header = networkHelper.DeserializePacketHeader(stream, out _);
                    var received = networkHelper.DeserializePacket(header, stream, out _);
                    Require((string)received.GetType().GetProperty("Text").GetValue(received) == "hello QGF 中文"
                        && stream.Position == stream.Length, "Packet header/body roundtrip");
                    ReferencePool.Release(received);
                }
                using (var stream = new MemoryStream(BitConverter.GetBytes(70000)))
                {
                    try
                    {
                        networkHelper.DeserializePacketHeader(stream, out _);
                        throw new Exception("Oversized packet header accepted");
                    }
                    catch (InvalidDataException) { }
                }
                checks.Add("Actual documented TCP Helper framing, UTF-8 and length rejection");
            }
            finally
            {
                FrameworkEntry.Shutdown();
            }
            Require(cache.Count == 0, "Core shutdown cleanup");
            var result = new { passed = true, checks = checks.ToArray(), unityVersion = Application.unityVersion,
                completeExamplesExecuted = new[] { "GoldAddedEventArgs", "GuideStartup", "GuideMenu",
                    "TextPacket", "TextChannelHelper", "DRItem partial extension" },
                scope = "Core operations and protocol framing; no live HTTP/TCP server or Player deployment" };
            File.WriteAllText("Artifacts~/Documentation/examples-smoke.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }

        public sealed class DiskHelper : IFileSystemHelper
        {
            public FileSystemStream CreateFileSystemStream(string path, FileSystemAccess access, bool create)
                => new CommonFileSystemStream(path, access, create);
        }

        public sealed class ProbePoolItem : ObjectBase
        {
            public static int Released;
            public int Spawned, Unspawned;
            public void Setup() => Initialize(new object());
            protected override void OnSpawn() => Spawned++;
            protected override void OnUnspawn() => Unspawned++;
            protected override void Release(bool isShutdown) => Released++;
            public override void Clear()
            {
                base.Clear();
                Spawned = Unspawned = 0;
            }
        }
    }
}
