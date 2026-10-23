#if UNITY_EDITOR
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;
using UnityQGameFramework.Examples.TableData;

namespace UnityQGameFramework.Examples
{
    public sealed class LoadCatalogExample : MonoBehaviour
    {
        [SerializeField] private LubanDataTableCatalog invalidCatalog;
        [SerializeField] private Dropdown loadMode;
        [SerializeField] private Toggle contextToggle, failureToggle;
        [SerializeField] private Slider prioritySlider;
        [SerializeField] private Button loadButton, cancelButton, unloadButton;
        [SerializeField] private Text stateText, contextText, registryText, rowsText;
        private readonly StringBuilder rows = new StringBuilder();
        private string pendingName;
        private int requestId, callbackCount;
        private bool ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                prioritySlider.wholeNumbers = true;
                // 同时接收成功和失败事件；回调按资源名识别当前请求。
                QGF.Event.Subscribe(LoadDataTableSuccessEventArgs.EventId, OnLoaded);
                QGF.Event.Subscribe(LoadDataTableFailureEventArgs.EventId, OnFailed);
                loadButton.onClick.AddListener(() =>
                {
                    var tables = QGF.DataTable;
                    stateText.text = "加载中…";
                    contextText.text = string.Empty;
                    if (loadMode.value == 0)
                    {
                        // Inspector 引用已包含 bytes，同步构造 Tables；结果事件仍会排队。
                        pendingName = tables.Catalog.name;
                        requestId = 0;
                        if (contextToggle.isOn) tables.LoadDataTables(loader => new Tables(loader), this);
                        else tables.LoadDataTables(loader => new Tables(loader));
                    }
                    else
                    {
                        // 资源入口异步加载 Catalog，完成后才发布整组表。
                        pendingName = UnityEditor.AssetDatabase.GetAssetPath(failureToggle.isOn ? invalidCatalog : tables.Catalog);
                        requestId = tables.LoadDataTables(pendingName, loader => new Tables(loader),
                            contextToggle.isOn ? this : null, (int)prioritySlider.value);
                    }
                });
                cancelButton.onClick.AddListener(() =>
                {
                    // 取消使旧请求失效；迟到的资源回调会释放资产，不再发布表。
                    QGF.DataTable.CancelLoadDataTables();
                    pendingName = null;
                    stateText.text = "已取消，等待中的旧请求不再发布表。";
                });
                unloadButton.onClick.AddListener(() =>
                {
                    // 整组卸载同时清理原生 Tables、QGF 视图及待完成请求。
                    QGF.DataTable.UnloadDataTables();
                    pendingName = null;
                    stateText.text = "已卸载，可以重新加载。";
                });
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void OnLoaded(object sender, GameEventArgs args)
        {
            var e = (LoadDataTableSuccessEventArgs)args;
            if (e.DataTableAssetName != pendingName) return;
            callbackCount++;
            stateText.text = $"加载成功，用时 {e.Duration:F2} 秒。";
            contextText.text = ReferenceEquals(e.UserData, this) ? "UserData：当前对象" : "UserData：未设置";
        }

        private void OnFailed(object sender, GameEventArgs args)
        {
            var e = (LoadDataTableFailureEventArgs)args;
            if (e.DataTableAssetName != pendingName) return;
            callbackCount++;
            stateText.text = "加载失败：" + e.ErrorMessage;
            contextText.text = ReferenceEquals(e.UserData, this) ? "失败回调，UserData：当前对象" : "失败回调，UserData：未设置";
        }

        private void Update()
        {
            if (!ready) return;
            var tables = QGF.DataTable;
            bool empty = tables.LubanTables == null;
            loadButton.interactable = empty && !tables.IsLoading;
            cancelButton.interactable = tables.IsLoading;
            unloadButton.interactable = !empty || tables.IsLoading;
            loadMode.interactable = contextToggle.interactable = !tables.IsLoading;
            failureToggle.interactable = prioritySlider.interactable = loadMode.value == 1 && !tables.IsLoading;
            registryText.text = $"加载状态：{(tables.IsLoading ? "加载中" : "空闲")}    已登记 {tables.Count} 张表\n"
                + $"请求 {requestId}    优先级 {(int)prioritySlider.value}    结果回调 {callbackCount} 次";
            rows.Clear();
            var items = tables.GetDataTable<DRItem>();
            if (items != null)
                foreach (var item in items) rows.AppendLine($"{item.Id}  {item.Name}  价格 {item.Price}");
            rowsText.text = items == null ? "尚未加载数据。" : rows.ToString();
        }

        private void OnDestroy()
        {
            var events = GameEntry.GetComponent<EventComponent>();
            if (events == null) return;
            if (events.Check(LoadDataTableSuccessEventArgs.EventId, OnLoaded)) events.Unsubscribe(LoadDataTableSuccessEventArgs.EventId, OnLoaded);
            if (events.Check(LoadDataTableFailureEventArgs.EventId, OnFailed)) events.Unsubscribe(LoadDataTableFailureEventArgs.EventId, OnFailed);
        }
    }
}
#endif
