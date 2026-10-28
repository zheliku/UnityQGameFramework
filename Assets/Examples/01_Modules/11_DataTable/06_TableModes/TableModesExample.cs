using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Examples.Modules.TableData;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 查询不同表模式、索引和跨表引用。
    /// </summary>
    public sealed class TableModesExample : MonoBehaviour
    {
        [SerializeField] private Button m_AliasesButton, m_IndexesButton, m_ModesButton;
        [SerializeField] private Text m_StateText, m_DetailsText;

        private readonly StringBuilder m_Text = new StringBuilder();

        private void Start()
        {
            // 仍然只加载一次；字符串键、复合键、list 和 one 表均登记到 QGF.DataTable。
            if (!QGF.DataTable.LoadDataTables<Tables>())
            {
                return;
            }

            m_AliasesButton.onClick.AddListener(ShowAliases);
            m_IndexesButton.onClick.AddListener(ShowIndexes);
            m_ModesButton.onClick.AddListener(ShowModes);
            ShowAliases();
        }

        private void ShowAliases()
        {
            TbItemAlias aliases = QGF.DataTable.GetDataTable<TbItemAlias>();
            // 已有记录的键一定存在，可直接使用索引器；输入可能缺键时用 GetDataRow。
            string key = aliases.DataList[0].Key;
            DRItemAlias alias = aliases[key];

            // ItemId 的类型定义为 int#ref=TbItem，生成的 ItemId_Ref 在加载时自动解析。
            DRItem item = alias.ItemId_Ref;
            bool shared = ReferenceEquals(item, QGF.DataTable.GetDataTable<TbItem>()[alias.ItemId]);
            m_StateText.text = "Map keys can be strings; integer IDs are not required.";
            m_DetailsText.text = $"String key: {alias.Key}\nRelated item: {item.Name}\nPrice: {item.Price}\nSame reference as the TbItem row: {shared}";
        }

        private void ShowIndexes()
        {
            // __tables__.xlsx 中的 Level+Wave 生成元组索引器，两个值共同定位一行。
            DRStage stage = QGF.DataTable.GetDataTable<TbStage>()[(1, 2)];

            // Name,Price 表示两个独立索引；同一 DRItem 行结构也可用于不同的表。
            TbItemIndex items = QGF.DataTable.GetDataTable<TbItemIndex>();
            string name = items.DataList[0].Name;
            DRItem byName = items.GetByName(name);
            DRItem byPrice = items.GetByPrice(50);
            m_StateText.text = "Indexes are defined in the Excel schema. Indexed queries do not scan the table.";
            m_DetailsText.text = $"Composite key (1, 2): Reward {stage.RewardItemId_Ref.Name}\nBy Name: {byName.Name} / {byName.Price}\nBy Price=50: {byPrice.Name}";
        }

        private void ShowModes()
        {
            // one 表只有一行，通过 Data 取得；表自身的 Count 仍表示行数。
            TbRules rules = QGF.DataTable.GetDataTable<TbRules>();
            TbMessage messages = QGF.DataTable.GetDataTable<TbMessage>();
            m_Text.Clear();
            m_Text.AppendLine($"one: {rules.Data.Title}; player limit {rules.Data.MaxPlayers}; rows {rules.Count}");
            m_Text.AppendLine($"list: {messages.Count} notifications");
            foreach (DRMessage message in messages)
            {
                m_Text.AppendLine($"  {message.Priority}  {message.Text}");
            }

            // 无主键 list 不生成虚构 Id，仍可遍历或使用 QGF 的条件查询。
            DRMessage selected = messages.GetDataRow(message => message.Priority == 2);
            m_Text.AppendLine("Predicate query: " + selected.Text);
            m_StateText.text = "Different table modes share the same table access API.";
            m_DetailsText.text = m_Text.ToString();
        }

        private void OnDestroy()
        {
            DataTableComponent dataTable = GameEntry.GetComponent<DataTableComponent>();
            if (dataTable != null)
            {
                dataTable.UnloadDataTables();
            }
        }
    }
}
