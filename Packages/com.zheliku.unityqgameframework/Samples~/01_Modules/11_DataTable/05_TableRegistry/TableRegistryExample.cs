using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.DataTable;
using UnityQGameFramework.Examples.Modules.TableData;
using UnityQGameFramework.Luban;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 登记、查询和移除同类型的具名数据表。
    /// </summary>
    public sealed class TableRegistryExample : MonoBehaviour
    {
        private const string PreviewName = "Preview";

        [SerializeField] private Button m_RegisterButton, m_RemoveButton, m_ListButton;
        [SerializeField] private Text m_StateText, m_TablesText;

        private readonly List<DataTableBase> m_Tables = new List<DataTableBase>();
        private readonly StringBuilder m_Text = new StringBuilder();
        private TbItem m_Preview;

        private void Start()
        {
            if (!QGF.DataTable.LoadDataTables<Tables>())
            {
                return;
            }

            m_RegisterButton.onClick.AddListener(Register);
            m_RemoveButton.onClick.AddListener(Remove);
            m_ListButton.onClick.AddListener(Refresh);
            m_StateText.text = "Default table loaded. Register another table of the same type named Preview.";
            Refresh();
        }

        private void Register()
        {
            // 本表示例没有跨表引用，可以单独构造；常规启动仍使用 LoadDataTables 整批加载。
            byte[] bytes = QGF.DataTable.Catalog.GetAssets()["tbitem"].bytes;
            m_Preview = new TbItem(new ByteBuf(bytes), PreviewName);

            // 管理器以“表类型 + 名称”区分实例；重复登记会失败，不会覆盖已有表。
            QGF.DataTable.AddDataTables(new[] { m_Preview });
            TbItem table = QGF.DataTable.GetDataTable<TbItem>(PreviewName);
            m_StateText.text = $"Registered {table.Name}; total {table.Count} rows; the default table still exists.";
            Refresh();
        }

        private void Remove()
        {
            // 只移除这个实例的登记，不影响同类型的默认表，也不改写外部持有的记录。
            bool removed = QGF.DataTable.RemoveDataTable(m_Preview);
            m_StateText.text = removed ? "Preview removed. The default table is still available." : "Preview is not registered.";
            m_Preview = null;
            Refresh();
        }

        private void Refresh()
        {
            // 列表重载先清空再填充；复用列表可避免每次刷新都创建结果数组。
            QGF.DataTable.GetAllDataTablesNonAlloc(m_Tables);
            m_Text.Clear();
            foreach (DataTableBase table in m_Tables)
            {
                string name = string.IsNullOrEmpty(table.Name) ? "Default" : table.Name;
                m_Text.AppendLine($"{table.Type.Name} [{name}]  {table.Count} rows  Row type: {table.RowType.Name}");
            }
            m_TablesText.text = m_Text.ToString();
        }

        private void Update()
        {
            bool exists = QGF.DataTable.HasDataTable<TbItem>(PreviewName);
            m_RegisterButton.interactable = QGF.DataTable.IsLoaded && !exists;
            m_RemoveButton.interactable = exists;
        }

        private void OnDestroy()
        {
            DataTableComponent dataTable = GameEntry.GetComponent<DataTableComponent>();
            if (dataTable == null)
            {
                return;
            }

            if (m_Preview != null)
            {
                dataTable.RemoveDataTable(m_Preview);
            }
            dataTable.UnloadDataTables();
        }
    }
}
