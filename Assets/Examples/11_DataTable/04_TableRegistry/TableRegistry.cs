using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.DataTable;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class TableRegistry : MonoBehaviour
    {
        [SerializeField] private InputField nameInput;
        [SerializeField] private Toggle namedToggle;
        [SerializeField] private Dropdown apiMode, destroyMode, queryMode;
        [SerializeField] private Button createButton, registerButton, destroyButton;
        [SerializeField] private Text stateText, selectedText, tablesText, rowsText;
        private readonly List<DataTableBase> tables = new List<DataTableBase>();
        private readonly StringBuilder display = new StringBuilder();
        private IDataTable<PreviewRow> selected;
        private string status = "创建空表，或登记已有的三条记录。";
        private bool ready;

        // 表视图按 Id 建索引，构造后保持记录和主键不变。
        public sealed class PreviewRow : IDataRow
        {
            public int Id { get; }
            public string Name { get; }
            public PreviewRow(int id, string name) { Id = id; Name = name; }
        }

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                createButton.onClick.AddListener(() =>
                {
                    // CreateDataTable 建立空的只读视图，按行类型与名称区分。
                    if (apiMode.value == 0)
                    {
                        if (namedToggle.isOn) QGF.DataTable.CreateDataTable<PreviewRow>(nameInput.text);
                        else QGF.DataTable.CreateDataTable<PreviewRow>();
                    }
                    else
                    {
                        if (namedToggle.isOn) QGF.DataTable.CreateDataTable(typeof(PreviewRow), nameInput.text);
                        else QGF.DataTable.CreateDataTable(typeof(PreviewRow));
                    }
                    status = "空表已创建，行数为 0。";
                });
                registerButton.onClick.AddListener(() =>
                {
                    var rows = new Dictionary<int, PreviewRow>
                    {
                        { 1, new PreviewRow(1, "药水") },
                        { 2, new PreviewRow(2, "护盾") },
                        { 3, new PreviewRow(3, "推进器") }
                    };
                    // Core 接口可登记已构造的视图；Luban Tables 发布视图也经过这个入口。
                    var view = new DataTable<PreviewRow>(namedToggle.isOn ? nameInput.text : string.Empty, rows);
                    FrameworkEntry.GetModule<IDataTableManager>().RegisterDataTables(new[] { view });
                    status = "已登记三条内存记录。";
                });
                destroyButton.onClick.AddListener(() =>
                {
                    bool removed;
                    if (destroyMode.value == 1) removed = QGF.DataTable.DestroyDataTable<PreviewRow>(selected);
                    else if (destroyMode.value == 2) removed = QGF.DataTable.DestroyDataTable((DataTableBase)selected);
                    else if (apiMode.value == 0)
                        removed = namedToggle.isOn
                            ? QGF.DataTable.DestroyDataTable<PreviewRow>(nameInput.text)
                            : QGF.DataTable.DestroyDataTable<PreviewRow>();
                    else
                        removed = namedToggle.isOn
                            ? QGF.DataTable.DestroyDataTable(typeof(PreviewRow), nameInput.text)
                            : QGF.DataTable.DestroyDataTable(typeof(PreviewRow));
                    selected = null; // 销毁后不再使用旧视图。
                    status = removed ? "表已销毁。" : "没有找到这张表。";
                });
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!ready) return;
            var component = QGF.DataTable;
            bool valid = !namedToggle.isOn || !string.IsNullOrWhiteSpace(nameInput.text);
            selected = null;
            if (valid && apiMode.value == 0)
            {
                bool exists = namedToggle.isOn ? component.HasDataTable<PreviewRow>(nameInput.text) : component.HasDataTable<PreviewRow>();
                if (exists) selected = namedToggle.isOn ? component.GetDataTable<PreviewRow>(nameInput.text) : component.GetDataTable<PreviewRow>();
            }
            else if (valid)
            {
                bool exists = namedToggle.isOn ? component.HasDataTable(typeof(PreviewRow), nameInput.text) : component.HasDataTable(typeof(PreviewRow));
                if (exists)
                {
                    selected = (IDataTable<PreviewRow>)(namedToggle.isOn
                        ? component.GetDataTable(typeof(PreviewRow), nameInput.text)
                        : component.GetDataTable(typeof(PreviewRow)));
                }
            }
            createButton.interactable = registerButton.interactable = valid && selected == null;
            destroyButton.interactable = selected != null;
            nameInput.interactable = namedToggle.isOn;
            stateText.text = $"{status}\n已登记 {component.Count} 张表";
            selectedText.text = selected == null ? "尚未选中已登记的表。"
                : $"当前表：{selected.Type.Name} / {(selected.Name.Length == 0 ? "未命名" : selected.Name)}";
            // 两个集合入口返回同一批表，List 重载会清空原有结果。
            if (queryMode.value == 0) component.GetAllDataTables(tables);
            else { tables.Clear(); tables.AddRange(component.GetAllDataTables()); }
            display.Clear();
            foreach (var table in tables)
                display.AppendLine($"{table.Type.Name} / {(table.Name.Length == 0 ? "未命名" : table.Name)} / {table.Count} 行");
            tablesText.text = display.ToString();
            display.Clear();
            if (selected != null)
                foreach (var row in selected) display.AppendLine($"{row.Id}  {row.Name}");
            rowsText.text = display.ToString();
        }
    }
}
