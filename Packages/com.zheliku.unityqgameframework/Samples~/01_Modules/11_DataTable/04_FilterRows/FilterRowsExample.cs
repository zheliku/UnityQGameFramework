using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.Modules.TableData;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 按条件筛选并排序数据表行。
    /// </summary>
    public sealed class FilterRowsExample : MonoBehaviour
    {
        [SerializeField] private Slider priceSlider;
        [SerializeField] private Text rowsText;
        private TbItem items;
        private readonly List<DRItem> results = new List<DRItem>();
        private static readonly Comparison<DRItem> ComparePrice = (left, right) => left.Price.CompareTo(right.Price);
        private Predicate<DRItem> condition;
        private float maximumPrice;

        private void Start()
        {
            if (!QGF.DataTable.LoadDataTables<Tables>())
            {
                return;
            }
            items = QGF.DataTable.GetDataTable<TbItem>();
            // 容量和委托在启动时准备；之后反复查询复用同一份列表。
            results.Capacity = items.Count;
            condition = item => item.Price <= maximumPrice;
            priceSlider.onValueChanged.AddListener(Filter);
            Filter(priceSlider.value);
        }

        private void Filter(float price)
        {
            maximumPrice = price;
            // NonAlloc 清空并填充列表，不改动原表；GetDataRows 则返回新数组。
            items.GetDataRowsNonAlloc(condition, ComparePrice, results);
            rowsText.text = $"Price at most {price:F0}:";
            foreach (var item in results)
            {
                rowsText.text += $"\n{item.Name}  {item.Price}";
            }
        }

        private void OnDestroy()
        {
            var data = GameEntry.GetComponent<DataTableComponent>();
            if (data != null)
            {
                data.UnloadDataTables();
            }
        }
    }
}
