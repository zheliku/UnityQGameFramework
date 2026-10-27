using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace Luban.Validation
{
    public sealed class EmptyProcedure : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> owner) { base.OnEnter(owner); }
    }

    public sealed class LubanRuntimeProbe : MonoBehaviour
    {
        private int m_Errors;
        private void Awake() => Application.logMessageReceived += Capture;
        private void OnDestroy() => Application.logMessageReceived -= Capture;
        private void Capture(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) ++m_Errors;
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            string failure = null;
            try
            {
                var component = GameEntry.GetComponent<DataTableComponent>();
                component.LoadDataTables<StarForce.Tables>();
                Verify(component);
                component.UnloadDataTables();
                Require(component.Count == 0 && !component.IsLoaded, "Unload failed.");
                var bundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "luban"));
                Require(bundle != null, "Luban bundle did not load.");
                var catalog = bundle.LoadAsset<LubanDataTableCatalog>("Catalog");
                var config = GameEntry.GetComponent<ConfigComponent>();
                var text = bundle.LoadAsset<TextAsset>("GameConfig");
                Require(text != null && config.ParseData(text.bytes), "TOML Config failed to load from AssetBundle bytes.");
                Require(config.GetString("Game.Id") == "Star Force" && config.GetInt("Scene.Menu") == 1 && config.GetInt("Scene.Main") == 2,
                    "TOML Config changed the original global settings.");
                var assets = catalog.GetAssets();
                var tables = ((IDataTableLoader)new StarForce.Tables()).Load(name => new UnityQGameFramework.Luban.ByteBuf(assets[name].bytes));
                component.RegisterDataTables(tables);
                bundle.Unload(true);
                Require(component.GetDataTable<StarForce.TbEntity>().GetDataRow(10000).AssetName == "PlayerShip", "Parsed rows depended on released Unity assets.");
                Require(m_Errors == 0, "Player logged errors.");
            }
            catch (Exception exception) { failure = exception.ToString(); }
            var arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; ++i)
                if (arguments[i] == "-luban-result")
                    File.WriteAllText(arguments[i + 1], JsonUtility.ToJson(new Result
                    { success = failure == null, unityVersion = Application.unityVersion, tables = 11,
                      assetBundle = true, tomlConfig = failure == null, errors = m_Errors, failure = failure }, true));
            Application.Quit(failure == null ? 0 : 1);
        }

        private static void Verify(DataTableComponent component)
        {
            Require(component.Count == 11, "Expected 11 registered tables.");
            var row = component.GetDataTable<StarForce.TbEntity>().GetDataRow(10000);
            Require(row.AssetName == "PlayerShip", "QGF row value mismatch.");
            Require(ReferenceEquals(row, component.GetDataTable<StarForce.TbEntity>().DataMap[10000]), "Native row identity mismatch.");
        }

        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        [Serializable] private sealed class Result
        {
            public bool success;
            public string unityVersion;
            public int tables;
            public bool assetBundle;
            public bool tomlConfig;
            public int errors;
            public string failure;
        }
    }
}
