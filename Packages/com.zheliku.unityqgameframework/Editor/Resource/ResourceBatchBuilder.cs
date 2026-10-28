using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using UnityQGameFramework.Runtime;
using YooAsset.Editor;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 为原生 YooAsset Builder 提供包多选构建入口。
    /// </summary>
    internal static class ResourceBatchBuilder
    {
        private const string ElementName = "QgfBatchBuild";

        /// <summary>
        /// 绘制当前收集配置的包选择与批量构建按钮。
        /// </summary>
        /// <param name="window">原生构建窗口。</param>
        /// <param name="collector">当前收集配置。</param>
        internal static void Draw(BundleBuilderWindow window, BundleCollectorSetting collector)
        {
            window.rootVisualElement.Q(ElementName)?.RemoveFromHierarchy();
            if (collector == null)
            {
                return;
            }
            string key = "QGF.Resource.Batch." + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(collector));
            string[] names = collector.Packages.Select(package => package.PackageName).ToArray();
            var selected = new HashSet<string>(SessionState.GetString(key, string.Empty).Split('\n').Where(names.Contains), StringComparer.Ordinal);
            var section = new Foldout { name = ElementName, text = "Batch Build", value = SessionState.GetBool(key + ".Open", false) };
            section.RegisterValueChangedCallback(change =>
            {
                if (ReferenceEquals(change.target, section))
                {
                    SessionState.SetBool(key + ".Open", change.newValue);
                }
            });
            section.Add(new Label("Uses the pipeline, compression and copy options saved for each package in this Builder."));
            var toggles = new List<Toggle>();
            Button build = null;
            foreach (string name in names)
            {
                var toggle = new Toggle(name) { name = "QgfBuildPackage." + name, value = selected.Contains(name) };
                toggle.RegisterValueChangedCallback(change =>
                {
                    if (change.newValue)
                    {
                        selected.Add(name);
                    }
                    else
                    {
                        selected.Remove(name);
                    }
                    SessionState.SetString(key, string.Join("\n", names.Where(selected.Contains)));
                    build?.SetEnabled(selected.Count > 0 && !ResourceBuildService.IsBuilding && !EditorApplication.isPlayingOrWillChangePlaymode);
                });
                toggles.Add(toggle);
                section.Add(toggle);
            }
            var controls = new VisualElement();
            controls.style.flexDirection = FlexDirection.Row;
            controls.Add(new Button(() => toggles.ForEach(toggle => toggle.value = true)) { text = "Select All" });
            controls.Add(new Button(() => toggles.ForEach(toggle => toggle.value = false)) { text = "Clear" });
            var result = new Label(SessionState.GetString(key + ".Result", string.Empty)) { name = "QgfBatchResult" };
            build = new Button(() =>
            {
                try
                {
                    string[] packages = names.Where(selected.Contains).ToArray();
                    if (packages.Length == 0)
                    {
                        result.text = "Select at least one package.";
                        return;
                    }
                    Dictionary<string, string> outputs = ResourceBuildService.BuildPackages(collector, packages);
                    result.text = "Built " + outputs.Count + " packages. See Console for output paths.";
                    foreach (var output in outputs)
                    {
                        Log.Info("Built resource package '{0}': {1}", output.Key, output.Value);
                    }
                }
                catch (OperationCanceledException)
                {
                    result.text = "Cancelled. Completed package builds are kept.";
                }
                catch (Exception exception)
                {
                    result.text = "Build failed: " + exception.Message;
                    Log.Error(exception);
                }
                finally
                {
                    // 构建产物导入会刷新原生窗口，保留本次结果以便刷新后继续查看。
                    SessionState.SetString(key + ".Result", result.text);
                }
            })
            {
                text = "Build Selected",
                name = "QgfBuildSelected"
            };
            build.SetEnabled(selected.Count > 0 && !ResourceBuildService.IsBuilding && !EditorApplication.isPlayingOrWillChangePlaymode);
            controls.Add(build);
            section.Add(controls);
            section.Add(result);
            section.schedule.Execute(() =>
            {
                build.SetEnabled(selected.Count > 0 && !ResourceBuildService.IsBuilding && !EditorApplication.isPlayingOrWillChangePlaymode);
            }).Every(200);
            window.rootVisualElement.Insert(1, section);
        }
    }
}
