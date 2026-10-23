using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Examples
{
    public sealed class FindLoadedTypes : MonoBehaviour
    {
        [SerializeField] private InputField typeInput, filterInput;
        [SerializeField] private Button findButton, listButton;
        [SerializeField] private Text typeText, listText;

        private void Start()
        {
            findButton.onClick.AddListener(Find);
            listButton.onClick.AddListener(List);
            listText.text = "点击列出类型，读取当前已加载程序集中的类型。";
            Find();
        }

        private void Find()
        {
            string name = typeInput.text.Trim();
            if (name.Length == 0)
            {
                typeText.text = "请输入包含命名空间的类型名。";
                return;
            }

            // Base 也用这一入口按 Inspector 保存的类型名查找 Helper。
            // 先按 .NET 规则解析类型名，再遍历已加载程序集；查找本身不创建实例。
            Type type;
            try { type = Utility.Assembly.GetType(name); }
            catch (ArgumentException) { typeText.text = "类型名格式有误。"; return; }
            typeText.text = type == null ? "未找到类型：" + name
                : $"类型：{type.FullName}\n程序集：{type.Assembly.GetName().Name}\n基类：{type.BaseType?.FullName ?? "无"}\n"
                    + $"继承 MonoBehaviour：{(typeof(MonoBehaviour).IsAssignableFrom(type) ? "是" : "否")}";
        }

        private void List()
        {
            var assemblies = Utility.Assembly.GetAssemblies();
            Type[] array = Utility.Assembly.GetTypes();
            // List 重载先清空容器；这里与数组重载并排显示数量。
            var types = new List<Type>();
            Utility.Assembly.GetTypes(types);
            string filter = filterInput.text.Trim();
            var matches = types.Where(type => type.FullName != null
                && type.FullName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            listText.text = $"已加载程序集：{assemblies.Length}\n类型：数组 {array.Length} / List {types.Count}\n"
                + $"筛选匹配 {matches.Length} 个，显示前 6 个：\n"
                + string.Join("\n", matches.Take(6).Select(type => type.FullName));
        }
    }
}
