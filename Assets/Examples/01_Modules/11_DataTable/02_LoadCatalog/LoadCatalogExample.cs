using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.Modules.TableData;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 通过资源地址异步加载 Catalog，并在完成事件后查询表。
    /// </summary>
    public sealed class LoadCatalogExample : MonoBehaviour
    {
        private const string CatalogAddress = "Tables_Catalog";

        [SerializeField] private Button loadButton, cancelButton, unloadButton;
        [SerializeField] private Text stateText, rowsText;

        private readonly StringBuilder rows = new StringBuilder();
        private bool ownsTables;

        private void Start()
        {
            // 组件已自动启动，无需 Initialize。先订阅结果，再由按钮发起异步加载。
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
            stateText.text = "点击加载：从资源包读取 Tables_Catalog，完成后再查询表；不读取全局生成 Profile。";
        }

        private void Load()
        {
            // 带地址的重载从 Resource 加载 Catalog，不使用组件上直接指定的 Catalog。
            // 调用返回时数据可能尚未就绪；收到成功事件后才能 GetDataTable。全局生成 Profile 不参与运行。
            ownsTables = true;
            QGF.DataTable.LoadDataTables<Tables>(CatalogAddress, this);
            stateText.text = "正在异步加载 Catalog 和表数据…";
        }

        private void OnLoaded(object sender, GameEventArgs args)
        {
            var result = (LoadDataTableSuccessEventArgs)args;
            if (result.DataTableAssetName != CatalogAddress || !ReferenceEquals(result.UserData, this))
            {
                return;
            }

            ownsTables = true;
            // 完成事件表示 bytes 解析、跨表引用和整组登记均已完成；后续查询只读取内存中的表。
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
            if (result.DataTableAssetName != CatalogAddress || !ReferenceEquals(result.UserData, this))
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
