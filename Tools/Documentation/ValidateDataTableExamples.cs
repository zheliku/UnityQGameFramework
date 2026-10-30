using TMPro;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Examples.Modules.TableData;
using UnityQGameFramework.Runtime;

/// <summary>
/// 检查隔离工程中的数据表示例。
/// </summary>
public static class ValidateDataTableExamples
{
    /// <summary>
    /// 运行数据表示例。
    /// </summary>
    /// <returns>场景检查结果。</returns>
    public static async Task<object> Run()
    {
        var names = new[] { "LoadLubanTablesExample", "QueryRowsExample", "LoadCatalogExample",
            "FilterRowsExample", "TableRegistryExample", "TableModesExample" };
        var result = await ValidateModuleExamples.Run("Assets/Examples", names, Exercise);
        Directory.CreateDirectory("Artifacts~/Luban");
        File.WriteAllText("Artifacts~/Luban/examples-unified.json", JsonConvert.SerializeObject(result, Formatting.Indented));
        return result;
    }

    private static async Task Exercise(string name, MonoBehaviour example)
    {
        if (name == "LoadLubanTablesExample" || name == "QueryRowsExample")
        {
            var items = QGF.DataTable.GetDataTable<TbItem>();
            if (!QGF.DataTable.IsLoaded || items == null || items.Count == 0)
                throw new Exception("Generated table not loaded");
            if (!ReferenceEquals(items, QGF.DataTable.GetDataTable(typeof(TbItem))))
                throw new Exception("Typed/untyped lookup differ");
            if (name == "QueryRowsExample")
            {
                var field = example.GetType().GetField("idInput", BindingFlags.NonPublic | BindingFlags.Instance);
                ((TMP_InputField)field.GetValue(example)).text = "999999";
                if (items.GetDataRow(999999) != null)
                    throw new Exception("Missing key did not return null");
            }
        }
        else
            await ModuleExampleActions.Exercise(name, example);
    }
}
