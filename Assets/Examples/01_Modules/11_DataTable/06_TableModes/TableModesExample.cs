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
            DRItemAlias alias = aliases.GetDataRow("Potion");

            // ItemId 的类型定义为 int#ref=TbItem，生成的 ItemId_Ref 在加载时自动解析。
            DRItem item = alias.ItemId_Ref;
            bool shared = ReferenceEquals(item, QGF.DataTable.GetDataTable<TbItem>().GetDataRow(alias.ItemId));
            m_StateText.text = "map 表的键可以是 string，不限于整数 Id。";
            m_DetailsText.text = $"字符串键：{alias.Key}\n关联道具：{item.Name}\n价格：{item.Price}\n引用与 TbItem 的记录相同：{shared}";
        }

        private void ShowIndexes()
        {
            // __tables__.xlsx 中的 Level+Wave 生成复合键签名，两个值共同定位一行。
            DRStage stage = QGF.DataTable.GetDataTable<TbStage>().GetDataRow(1, 2);

            // Name,Price 表示两个独立索引；同一 DRItem 行结构也可用于不同的表。
            TbItemIndex items = QGF.DataTable.GetDataTable<TbItemIndex>();
            DRItem byName = items.GetByName("回复药");
            DRItem byPrice = items.GetByPrice(50);
            m_StateText.text = "索引由 Excel schema 决定，查询不需要遍历表。";
            m_DetailsText.text = $"复合键 (1, 2)：奖励 {stage.RewardItemId_Ref.Name}\n按 Name：{byName.Name} / {byName.Price}\n按 Price=50：{byPrice.Name}";
        }

        private void ShowModes()
        {
            // one 表只有一行，通过 Data 取得；表自身的 Count 仍表示行数。
            TbRules rules = QGF.DataTable.GetDataTable<TbRules>();
            TbMessage messages = QGF.DataTable.GetDataTable<TbMessage>();
            m_Text.Clear();
            m_Text.AppendLine($"one：{rules.Data.Title}，玩家上限 {rules.Data.MaxPlayers}，行数 {rules.Count}");
            m_Text.AppendLine($"list：{messages.Count} 条提示");
            foreach (DRMessage message in messages)
            {
                m_Text.AppendLine($"  {message.Priority}  {message.Text}");
            }

            // 无主键 list 不生成虚构 Id，仍可遍历或使用 QGF 的条件查询。
            DRMessage selected = messages.GetDataRow(message => message.Priority == 2);
            m_Text.AppendLine("条件查询：" + selected.Text);
            m_StateText.text = "表模式可以不同，获取表的入口相同。";
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
