using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.DataTable;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.TableData;

namespace UnityQGameFramework.Examples
{
    public sealed class QueryRowsExample : MonoBehaviour
    {
        [SerializeField] private InputField idInput;
        [SerializeField] private Slider priceSlider;
        [SerializeField] private Dropdown queryMode;
        [SerializeField] private Toggle indexerToggle, descendingToggle;
        [SerializeField] private Text selectedText, conditionText, rowsText, tableText;
        private readonly List<DRItem> results = new List<DRItem>();
        private readonly StringBuilder rows = new StringBuilder();
        private IDataTable<DRItem> items;
        private Predicate<DRItem> affordable;
        private Comparison<DRItem> byPrice;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                priceSlider.wholeNumbers = true;
                // 构造生成的 Tables 会解析 Luban 数据并登记 QGF 查询视图。
                QGF.DataTable.LoadDataTables(loader => new Tables(loader));
                items = QGF.DataTable.GetDataTable<DRItem>();
                if (items == null) throw new InvalidOperationException("示例表加载失败。");
                affordable = item => item.Price <= priceSlider.value;
                byPrice = (a, b) => descendingToggle.isOn ? b.Price.CompareTo(a.Price) : a.Price.CompareTo(b.Price);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (items == null) return;
            // 两种主键入口都允许缺键；HasDataRow 可以先判断是否存在。
            bool valid = int.TryParse(idInput.text, out int id);
            var selected = valid ? (indexerToggle.isOn ? items[id] : items.GetDataRow(id)) : null;
            selectedText.text = !valid ? "请输入整数 Id。" : $"Id {id}：{(items.HasDataRow(id) ? "存在" : "不存在")}\n"
                + (selected == null ? "查询结果：无" : $"{selected.Name}，价格 {selected.Price}");
            var first = items.GetDataRow(affordable);
            conditionText.text = $"价格不超过 {priceSlider.value:F0}：{(items.HasDataRow(affordable) ? "有匹配记录" : "无匹配记录")}\n"
                + (first == null ? "没有匹配记录。" : "首个匹配：" + first.Name);
            // 数组入口返回新结果；List 入口清空并填充传入容器。
            switch (queryMode.value)
            {
                case 0: results.Clear(); results.AddRange(items.GetAllDataRows()); break;
                case 1: items.GetAllDataRows(results); break;
                case 2: results.Clear(); results.AddRange(items.GetDataRows(affordable)); break;
                case 3: items.GetDataRows(affordable, results); break;
                case 4: results.Clear(); results.AddRange(items.GetDataRows(byPrice)); break;
                case 5: items.GetDataRows(byPrice, results); break;
                case 6: results.Clear(); results.AddRange(items.GetDataRows(affordable, byPrice)); break;
                default: items.GetDataRows(affordable, byPrice, results); break;
            }
            rows.Clear();
            foreach (var item in results) rows.AppendLine($"{item.Id}  {item.Name}  价格 {item.Price}");
            rowsText.text = results.Count == 0 ? "当前查询没有结果。" : rows.ToString();
            tableText.text = $"{items.Type.Name}  共 {items.Count} 行，结果 {results.Count} 行\n"
                + $"最小 Id {items.MinIdDataRow.Id}    最大 Id {items.MaxIdDataRow.Id}";
        }
    }
}
