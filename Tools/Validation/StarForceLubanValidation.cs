using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Luban;
using UnityQGameFramework.Runtime;

/// <summary>Run through Unity CLI run_script in Play Mode after StarForce preload.</summary>
public static class StarForceLubanValidation
{
    public static async Task<object> Run()
    {
        if (!Application.isPlaying)
            throw new Exception("Start StarForce Launcher first.");
        bool wasPaused = EditorApplication.isPaused;
        EditorApplication.isPaused = true;
        int errors = 0;
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                ++errors;
        };
        Application.logMessageReceived += capture;
        try
        {
            object result = await Validate();
            Require(errors == 0, "The game logged errors during validation.");
            return result;
        }
        finally
        {
            Application.logMessageReceived -= capture;
            EditorApplication.isPaused = wasPaused;
        }
    }

    private static async Task<object> Validate()
    {
        // The launcher intentionally spends frames in splash and preload procedures.
        for (int i = 0; i < 200 && (StarForce.GameEntry.DataTable == null || StarForce.GameEntry.DataTable.Count != 11); ++i)
        {
            EditorApplication.Step();
            await Task.Delay(50);
        }
        var component = StarForce.GameEntry.DataTable;
        // Do not unload global configuration while the preload/change-scene procedures consume it.
        for (int i = 0; i < 200 && UnityEngine.Object.FindFirstObjectByType<StarForce.MenuForm>() == null; ++i)
        {
            EditorApplication.Step();
            await Task.Delay(50);
        }
        Require(UnityEngine.Object.FindFirstObjectByType<StarForce.MenuForm>() != null, "Wait for the stable menu before lifecycle tests.");
        var tables = component;
        Require(tables != null && component.Count == 11, "All 11 tables must be published.");
        int checkedValues = CompareOriginalTables(tables);
        var entities = component.GetDataTable<StarForce.TbEntity>();
        Require(ReferenceEquals(entities.GetDataRow(10000), tables.GetDataTable<StarForce.TbEntity>().DataMap[10000]), "Facade must share native records.");
        Require(entities.GetDataRow(-1) == null && !entities.HasDataRow(-1), "Missing rows preserve UGF null semantics.");
        Require(entities.Min(row => row.Id) == 10000 && entities.Max(row => row.Id) == 70002, "Min/max ID queries.");
        Require(entities.GetDataRows(row => row.Id >= 70000).Length == 3, "Predicate query.");
        var resultList = new List<StarForce.DREntity> { entities.GetDataRow(10000) };
        entities.GetDataRowsNonAlloc(row => row.Id >= 70000, (a, b) => b.Id.CompareTo(a.Id), resultList);
        Require(resultList.Count == 3 && resultList[0].Id == 70002, "List overload clears and sorts.");
        Require(entities.Count() == entities.Count && component.HasDataTable(typeof(StarForce.TbEntity)), "Enumeration and type lookup.");
        var manager = GameFrameworkEntry.GetModule<IDataTableManager>();
        var named = new StarForce.TbEntity(new ByteBuf(component.Catalog.GetAssets()["tbentity"].bytes), "Validation");
        manager.AddDataTables(new[] { named });
        Require(ReferenceEquals(component.GetDataTable<StarForce.TbEntity>("Validation"), named), "Named table lookup.");
        var pending = new StarForce.TbEntity(new ByteBuf(component.Catalog.GetAssets()["tbentity"].bytes), "Pending");
        Expect<GameFrameworkException>(() => manager.AddDataTables(new DataTableBase[] { pending, named }));
        Require(!component.HasDataTable<StarForce.TbEntity>("Pending"), "Duplicate registration must be atomic.");
        component.RemoveDataTable((DataTableBase)named);

        var catalog = component.Catalog;
        string path = AssetDatabase.GetAssetPath(catalog);
        component.UnloadDataTables();
        Require(component.Count == 0 && !component.IsLoaded, "Unload releases the entire set.");
        int successes = 0, failures = 0;
        EventHandler<GameEventArgs> success = (sender, args) => { if (ReferenceEquals(((LoadDataTableSuccessEventArgs)args).UserData, component)) ++successes; };
        EventHandler<GameEventArgs> failure = (sender, args) => { if (ReferenceEquals(((LoadDataTableFailureEventArgs)args).UserData, component)) ++failures; };
        StarForce.GameEntry.Event.Subscribe(LoadDataTableSuccessEventArgs.EventId, success);
        StarForce.GameEntry.Event.Subscribe(LoadDataTableFailureEventArgs.EventId, failure);
        try
        {
            component.LoadDataTables<StarForce.Tables>(path, component);
            component.CancelLoadDataTables();
            for (int i = 0; i < 10; ++i)
            {
                EditorApplication.Step();
                await Task.Delay(30);
            }
            Require(component.Count == 0 && successes == 0 && failures == 0, "Cancelled request must not publish or emit events.");
            component.LoadDataTables<StarForce.Tables>(path, component);
            for (int i = 0; i < 100 && (component.IsLoading || successes == 0); ++i)
            {
                EditorApplication.Step();
                await Task.Delay(30);
            }
            Require(!component.IsLoading && successes == 1 && component.Count == 11, "Resource catalog loading and success event.");
            component.UnloadDataTables();
            component.LoadDataTables<FailingTableLoader>(path, component);
            for (int i = 0; i < 100 && (component.IsLoading || failures == 0); ++i)
            {
                EditorApplication.Step();
                await Task.Delay(30);
            }
            Require(!component.IsLoading && failures == 1 && component.Count == 0, "Load failure must not publish partial tables.");
            // Restore the complete table set for the running sample.
            component.LoadDataTables<StarForce.Tables>();
        }
        finally
        {
            StarForce.GameEntry.Event.Unsubscribe(LoadDataTableSuccessEventArgs.EventId, success);
            StarForce.GameEntry.Event.Unsubscribe(LoadDataTableFailureEventArgs.EventId, failure);
        }
        var result = new
        {
            passed = true,
            tables = component.Count,
            originalValuesCompared = checkedValues,
            sharedRecords = true,
            cancelledLoad = true,
            atomicFailure = true,
            resourceLoad = true,
            unityVersion = Application.unityVersion
        };
        Directory.CreateDirectory("Artifacts~/Luban");
        File.WriteAllText("Artifacts~/Luban/runtime-validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    /// <summary>
    /// 模拟解析失败的数据表加载器。
    /// </summary>
    public sealed class FailingTableLoader : IDataTableLoader
    {
        /// <summary>
        /// 获取生成代码的结构版本。
        /// </summary>
        public string SchemaHash
        {
            get
            {
                return ((IDataTableLoader)new StarForce.Tables()).SchemaHash;
            }
        }

        /// <summary>
        /// 抛出预期的解析异常。
        /// </summary>
        /// <param name="loader">数据文件加载方法。</param>
        /// <returns>本检查始终抛出异常。</returns>
        public IReadOnlyList<DataTableBase> CreateTables(Func<string, ByteBuf> loader)
        {
            throw new InvalidDataException("Expected validation failure");
        }
    }

    private static int CompareOriginalTables(DataTableComponent tables)
    {
        var expected = JArray.Parse(File.ReadAllText("Tools/Validation/StarForceOriginalTables.json"));
        int values = 0;
        foreach (JToken table in expected)
        {
            object native = tables.GetDataTable(typeof(StarForce.TbEntity).Assembly.GetType("StarForce.Tb" + table["name"]));
            var rows = ((IEnumerable)native.GetType().GetProperty("DataList").GetValue(native)).Cast<object>().ToArray();
            JArray fields = (JArray)table["fields"], records = (JArray)table["rows"];
            Require(rows.Length == records.Count, "Row count: " + table["name"]);
            for (int i = 0; i < rows.Length; ++i)
                for (int j = 0; j < fields.Count; ++j)
                {
                    object actual = rows[i].GetType().GetField((string)fields[j]["name"]).GetValue(rows[i]);
                    JToken value = records[i][j];
                    bool equal = actual is float f ? Math.Abs(f - (double)value) < 0.00001 : JToken.DeepEquals(JToken.FromObject(actual), value);
                    Require(equal, "Changed value: " + table["name"] + " row " + i + " field " + fields[j]["name"]);
                    ++values;
                }
        }
        return values;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
    private static void Expect<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
}
