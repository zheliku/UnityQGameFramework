using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.DataTable;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.TableData;

namespace UnityQGameFramework.Examples
{
    public sealed class NativeTableExample : MonoBehaviour
    {
        [SerializeField] private InputField idInput;
        [SerializeField] private Dropdown nativeMode;
        [SerializeField] private Text resultText, identityText, summaryText;
        private Tables tables;
        private IDataTable<DRItem> view;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                QGF.DataTable.LoadDataTables(loader => new Tables(loader));
                // 原生 Tables 提供 Luban 生成的索引；QGF 视图使用同一批记录。
                tables = QGF.DataTable.GetLubanTables<Tables>();
                view = QGF.DataTable.GetDataTable<DRItem>();
                if (tables == null || view == null) throw new InvalidOperationException("示例表加载失败。");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (tables == null) return;
            summaryText.text = $"DataList {tables.TbItem.DataList.Count} 行    DataMap {tables.TbItem.DataMap.Count} 项\nQGF 视图 {view.Count} 行";
            identityText.text = string.Empty;
            if (!int.TryParse(idInput.text, out int id))
            {
                resultText.text = "请输入整数 Id。";
                return;
            }
            // Get 和索引器要求主键存在；允许缺键时使用 GetOrDefault。
            if (nativeMode.value != 0 && !tables.TbItem.DataMap.ContainsKey(id))
            {
                resultText.text = $"Id {id} 不存在，这个入口需要有效主键。";
                return;
            }
            DRItem row;
            switch (nativeMode.value)
            {
                case 0: row = tables.TbItem.GetOrDefault(id); break;
                case 1: row = tables.TbItem.Get(id); break;
                default: row = tables.TbItem[id]; break;
            }
            resultText.text = row == null ? "原生查询结果：null" : $"{row.Id}  {row.Name}  价格 {row.Price}";
            if (row != null)
                identityText.text = ReferenceEquals(row, view.GetDataRow(id))
                    ? "原生表与 QGF 查询共用同一记录" : "原生表与 QGF 查询返回不同记录";
        }
    }
}
