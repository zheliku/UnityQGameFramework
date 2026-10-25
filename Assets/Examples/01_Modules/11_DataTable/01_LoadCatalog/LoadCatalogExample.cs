using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.Modules.TableData;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 通过 QGF.DataTable 加载 Luban 数据表目录。
    /// </summary>
    public sealed class LoadCatalogExample : MonoBehaviour
    {
        private const string CatalogAssetGuid = "b60d82a09e25580ca731c520aef667d6";

        [SerializeField] private Button loadButton, cancelButton, unloadButton;
        [SerializeField] private Text stateText, rowsText;

        private readonly StringBuilder rows = new StringBuilder();
        private string catalogName;
        private bool ownsTables;

        private void Start()
        {
            catalogName = CatalogAssetGuid;
            QGF.Event.Subscribe(LoadDataTableSuccessEventArgs.EventId, OnLoaded);
            QGF.Event.Subscribe(LoadDataTableFailureEventArgs.EventId, OnFailed);
            loadButton.onClick.AddListener(Load);
            cancelButton.onClick.AddListener(() =>
            {
                QGF.DataTable.CancelLoadDataTables();
                stateText.text = "已取消；迟到的资源回调不会发布这批表。";
            });
            unloadButton.onClick.AddListener(() =>
            {
                QGF.DataTable.UnloadDataTables();
                ownsTables = false;
                rowsText.text = "尚未加载数据。";
                stateText.text = "表已卸载。";
            });
        }

        private void Load()
        {
            // Resource 先加载 Catalog，完成后再由生成的 Tables 解析 bytes。
            ownsTables = true;
            QGF.DataTable.LoadDataTables(catalogName, loader => new Tables(loader), this);
            stateText.text = "正在异步加载 Catalog 和表数据…";
        }

        private void OnLoaded(object sender, GameEventArgs args)
        {
            var result = (LoadDataTableSuccessEventArgs)args;
            if (result.DataTableAssetName != catalogName || !ReferenceEquals(result.UserData, this))
            {
                return;
            }

            ownsTables = true;
            var items = QGF.DataTable.GetDataTable<TbItem>();
            rows.Clear();
            foreach (var item in items)
            {
                rows.AppendLine($"{item.Id}  {item.Name}  价格 {item.Price}");
            }
            rowsText.text = rows.ToString();
            stateText.text = $"加载完成，用时 {result.Duration:F2} 秒。";
        }

        private void OnFailed(object sender, GameEventArgs args)
        {
            var result = (LoadDataTableFailureEventArgs)args;
            if (result.DataTableAssetName != catalogName || !ReferenceEquals(result.UserData, this))
            {
                return;
            }
            stateText.text = "加载失败：" + result.ErrorMessage;
        }

        private void Update()
        {
            var tables = QGF.DataTable;
            loadButton.interactable = !tables.IsLoaded && !tables.IsLoading;
            cancelButton.interactable = tables.IsLoading;
            unloadButton.interactable = tables.IsLoaded || tables.IsLoading;
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                if (events.Check(LoadDataTableSuccessEventArgs.EventId, OnLoaded))
                {
                    events.Unsubscribe(LoadDataTableSuccessEventArgs.EventId, OnLoaded);
                }
                if (events.Check(LoadDataTableFailureEventArgs.EventId, OnFailed))
                {
                    events.Unsubscribe(LoadDataTableFailureEventArgs.EventId, OnFailed);
                }
            }
            if (ownsTables)
            {
                GameEntry.GetComponent<DataTableComponent>()?.UnloadDataTables();
            }
        }
    }
}
