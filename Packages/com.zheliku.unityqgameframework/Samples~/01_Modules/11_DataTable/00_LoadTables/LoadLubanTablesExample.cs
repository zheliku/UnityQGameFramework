using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.Modules.TableData;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class LoadLubanTablesExample : MonoBehaviour
    {
        [SerializeField] private Text rowsText;

        private void Start()
        {
            // Inspector 中的 Catalog 引用生成的 bytes；Tables 负责解析并解析跨表引用。
            var tables = QGF.DataTable.LoadDataTables(loader => new Tables(loader));
            if (tables == null)
            {
                return;
            }
            // GetDataTable 返回这些数据的 QGF 只读视图，不会再解析或复制一份。
            var items = QGF.DataTable.GetDataTable<TbItem>();
            rowsText.text = "";
            foreach (var item in items)
                rowsText.text += $"{item.Id}  {item.Name}  价格 {item.Price}\n";
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
