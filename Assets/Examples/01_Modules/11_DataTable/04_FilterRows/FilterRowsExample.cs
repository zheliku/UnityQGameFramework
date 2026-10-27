using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.DataTable;
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

        private void Start()
        {
            if (!QGF.DataTable.LoadDataTables<Tables>())
            {
                return;
            }
            items = QGF.DataTable.GetDataTable<TbItem>();
            priceSlider.onValueChanged.AddListener(Filter);
            Filter(priceSlider.value);
        }

        private void Filter(float price)
        {
            // 先按条件筛选，再按价格升序排列；返回结果数组，不改动原表。
            var rows = items.GetDataRows(item => item.Price <= price, (a, b) => a.Price.CompareTo(b.Price));
            rowsText.text = $"价格不超过 {price:F0}：";
            foreach (var item in rows)
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
