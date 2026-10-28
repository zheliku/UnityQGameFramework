using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.DataTable;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.Modules.TableData;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 按主键查询数据表行。
    /// </summary>
    public sealed class QueryRowsExample : MonoBehaviour
    {
        [SerializeField] private InputField idInput;
        [SerializeField] private Text selectedText;
        private TbItem items;

        private void Start()
        {
            if (!QGF.DataTable.LoadDataTables<Tables>())
            {
                return;
            }
            items = QGF.DataTable.GetDataTable<TbItem>();
            idInput.onValueChanged.AddListener(Query);
            Query(idInput.text);
        }

        private void Query(string value)
        {
            if (!int.TryParse(value, out int id))
            {
                selectedText.text = "Enter an integer ID.";
                return;
            }
            // 按整数主键查询；找不到时返回 null。
            var item = items.GetDataRow(id);
            selectedText.text = item == null ? "ID not found." : $"{item.Name}  Price {item.Price}";
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
