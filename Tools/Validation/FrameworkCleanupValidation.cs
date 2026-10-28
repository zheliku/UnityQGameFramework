using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Core;
using UnityQGameFramework.Examples.Modules.TableData;
using UnityQGameFramework.Luban;
using UnityQGameFramework.Runtime;

/// <summary>
/// 验证精简后的框架生成与编辑器入口。
/// </summary>
public static class FrameworkCleanupValidation
{
    /// <summary>
    /// 在 IMGUI 回调中绘制输入页面，检查仅启用 Input System 时的调用。
    /// </summary>
    /// <returns>输入页面的绘制结果。</returns>
    public static async Task<object> DrawInputWindows()
    {
        var window = ScriptableObject.CreateInstance<InputWindowProbe>();
        window.titleContent = new GUIContent("QGF Input Validation");
        try
        {
            window.Show();
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);
            while (!window.Drawn && window.Failure == null && DateTime.UtcNow < deadline)
            {
                window.Repaint();
                await Task.Delay(50);
            }

            Require(window.Failure == null, window.Failure);
            Require(window.Drawn, "The input pages did not receive an IMGUI repaint.");
            return new
            {
                passed = true,
                summary = true,
                touch = true,
                unityVersion = Application.unityVersion
            };
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// 提供输入页面所需的 IMGUI 绘制回调。
    /// </summary>
    private sealed class InputWindowProbe : EditorWindow
    {
        private IDebuggerWindow[] m_Windows;

        /// <summary>
        /// 获取输入页面是否已完成绘制。
        /// </summary>
        internal bool Drawn
        {
            get; private set;
        }

        /// <summary>
        /// 获取输入页面抛出的异常。
        /// </summary>
        internal string Failure
        {
            get; private set;
        }

        /// <summary>
        /// 在真实 IMGUI 上下文中调用输入页面。
        /// </summary>
        private void OnGUI()
        {
            if (Failure != null)
            {
                return;
            }

            try
            {
                if (m_Windows == null)
                {
                    m_Windows = new[] { "InputSummaryInformationWindow", "InputTouchInformationWindow" }
                        .Select(name => (IDebuggerWindow)Activator.CreateInstance(typeof(DebuggerComponent)
                            .GetNestedType(name, System.Reflection.BindingFlags.NonPublic), true)).ToArray();
                }

                foreach (IDebuggerWindow debuggerWindow in m_Windows)
                {
                    debuggerWindow.OnDraw();
                }

                if (Event.current.type == EventType.Repaint)
                {
                    Drawn = true;
                }
            }
            catch (Exception exception)
            {
                Failure = exception.ToString();
            }
        }
    }

    /// <summary>
    /// 检查实际 Luban 表的索引器、查询结果与运行时分配。
    /// </summary>
    /// <returns>查询结果、每条查询路径的分配字节数及失败信息。</returns>
    public static object QueryTables()
    {
        const string path = "Assets/Examples/01_Modules/UnityQGameFramework/Config/Data/Catalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<LubanDataTableCatalog>(path);
        var assets = catalog.GetAssets();
        var tables = ((IDataTableLoader)new Tables()).CreateTables(name => new ByteBuf(assets[name].bytes));
        TbItem items = tables.OfType<TbItem>().Single();
        TbItemAlias aliases = tables.OfType<TbItemAlias>().Single();
        TbStage stages = tables.OfType<TbStage>().Single();
        TbRules rules = tables.OfType<TbRules>().Single();
        TbMessage messages = tables.OfType<TbMessage>().Single();
        Require(ReferenceEquals(items[items.DataList[0].Id], items.DataList[0]), "Integer indexer must return the original row.");
        Require(ReferenceEquals(aliases[aliases.DataList[0].Key], aliases.DataList[0]), "String indexer must return the original row.");
        DRStage stage = stages.DataList[0];
        Require(ReferenceEquals(stages[(stage.Level, stage.Wave)], stage), "Composite indexer must return the original row.");
        bool missing = false;
        try
        {
            DRItem ignored = items[int.MinValue];
        }
        catch (KeyNotFoundException)
        {
            missing = true;
        }

        Require(missing && items.GetDataRow(int.MinValue) == null, "Indexer and optional query must retain their distinct missing-key behavior.");
        var manager = (IDataTableManager)Activator.CreateInstance(typeof(IDataTableManager).Assembly.GetType("UnityQGameFramework.Core.DataTableManager", true), true);
        manager.AddDataTables(tables);
        var rows = new List<DRItem>(items.Count);
        var registrations = new List<DataTableBase>(tables.Count);
        var oneRows = new List<DRRules>(1);
        var listRows = new List<DRMessage>(messages.Count);
        Predicate<DRItem> predicate = row => row.Price >= 0;
        Comparison<DRItem> comparison = (left, right) => left.Price.CompareTo(right.Price);
        Action[] queries =
        {
            () => items.GetDataRowsNonAlloc(predicate, rows),
            () => items.GetDataRowsNonAlloc(comparison, rows),
            () => items.GetDataRowsNonAlloc(predicate, comparison, rows),
            () => items.GetAllDataRowsNonAlloc(rows),
            () => manager.GetAllDataTablesNonAlloc(registrations),
            () => rules.GetAllDataRowsNonAlloc(oneRows),
            () => messages.GetAllDataRowsNonAlloc(listRows)
        };
        var allocated = new long[queries.Length];
        for (int query = 0; query < queries.Length; query++)
        {
            for (int warmup = 0; warmup < 100; warmup++)
            {
                queries[query]();
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int repeat = 0; repeat < 10000; repeat++)
            {
                queries[query]();
            }

            allocated[query] = GC.GetAllocatedBytesForCurrentThread() - before;
        }

        Require(allocated.All(bytes => bytes == 0), "A warmed NonAlloc query allocated memory: " + string.Join(", ", allocated));
        Require(rows.Count == items.Count && registrations.Count == tables.Count && oneRows.Count == 1 && listRows.Count == messages.Count,
            "NonAlloc queries must fill each output list.");
        rows.Add(items.DataList[0]);
        items.GetAllDataRowsNonAlloc(rows);
        Require(rows.Count == items.Count, "A reused output list must be cleared before filling.");
        DRItem[] snapshot = items.GetAllDataRows();
        Require(!ReferenceEquals(snapshot, items.GetAllDataRows()) && ReferenceEquals(snapshot[0], items.DataList[0]),
            "Array queries must return independent result arrays containing the original rows.");
        var result = new
        {
            passed = true,
            unityVersion = Application.unityVersion,
            intKey = true,
            stringKey = true,
            compositeKey = true,
            missingKey = true,
            reusedResults = true,
            queryCallsPerPath = 10000,
            allocatedBytes = allocated,
            tables = tables.Count
        };
        Directory.CreateDirectory("Artifacts~/FrameworkCleanup");
        File.WriteAllText("Artifacts~/FrameworkCleanup/queries.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    /// <summary>
    /// 检查验证条件。
    /// </summary>
    /// <param name="condition">需要满足的条件。</param>
    /// <param name="message">失败原因。</param>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// 使用当前模板重新生成隔离工程中的所有配置表。
    /// </summary>
    /// <returns>已生成的配置文件路径。</returns>
    public static async Task<object> GenerateTables()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use an isolated project in Edit Mode.");
        }

        var generated = new List<string>();
        EditorApplication.LockReloadAssemblies();
        try
        {
            foreach (string path in AssetDatabase.FindAssets("t:LubanProfile", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                await LubanGenerator.GenerateAsync(AssetDatabase.LoadAssetAtPath<LubanProfile>(path));
                generated.Add(path);
            }

            return new
            {
                passed = true,
                profiles = generated
            };
        }
        finally
        {
            EditorApplication.UnlockReloadAssemblies();
        }
    }
}
