using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.Modules.TableData;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 加载数据表并遍历记录。
    /// </summary>
    public sealed class LoadLubanTablesExample : MonoBehaviour
    {
        [SerializeField] private Text rowsText;

        private void Start()
        {
            // QGF 组件会先于普通业务脚本完成 Awake/Start，无需手动 Initialize；这里从 Start 开始使用。
            // 全局 Profile 只决定编辑器生成哪份表；本场景 QGF/DataTable 上的 Catalog 决定运行时读取哪组 bytes。
            // Tables 是生成的解析代码。同步加载成功后整组表已就绪，一次启动只需加载一次。
            if (!QGF.DataTable.LoadDataTables<Tables>())
            {
                rowsText.text = "加载失败，请查看 Console，并检查 DataTableComponent 的 Catalog 与生成代码是否匹配。";
                return;
            }
            // 返回已登记的生成表本身，不再解析或复制数据。
            var items = QGF.DataTable.GetDataTable<TbItem>();
            rowsText.text = $"已从 {QGF.DataTable.Catalog.name} 加载 {QGF.DataTable.Count} 张表。\nTbItem 的记录：\n";
            foreach (var item in items)
            {
                rowsText.text += $"{item.Id}  {item.Name}  价格 {item.Price}\n";
            }
        }

        private void OnDestroy()
        {
            // 本场景独占这批表，离开时卸载；实际游戏通常由启动流程加载，由整个游戏会话持有。
            var data = GameEntry.GetComponent<DataTableComponent>();
            if (data != null)
            {
                data.UnloadDataTables();
            }
        }
    }
}
