using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;
using YooAsset;
using YooAsset.Editor;

/// <summary>
/// 检查资源配置同步和原生窗口批量构建。
/// </summary>
public static class ResourceEditorRedesignValidation
{
    private const string Root = "Assets/ResourceEditorRedesign";

    /// <summary>
    /// 在隔离工程验证配置切换、包增删和真实批量构建。
    /// </summary>
    /// <returns>编辑器交互与构建检查结果。</returns>
    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use the isolated Consumer in Edit Mode.");
        }
        if (AssetDatabase.IsValidFolder(Root))
        {
            throw new InvalidOperationException("The validation fixture already exists.");
        }
        AssetDatabase.CreateFolder("Assets", "ResourceEditorRedesign");
        var moduleSettings = AssetDatabase.LoadAssetAtPath<ResourceSettings>("Assets/Examples/01_Modules/UnityQGameFramework/Resource/ResourceSettings.asset");
        var pointSettings = AssetDatabase.LoadAssetAtPath<ResourceSettings>("Assets/Examples/00_PointPointPoint/UnityQGameFramework/Resource/ResourceSettings.asset");
        var originalSelection = Selection.activeObject;
        BundleCollectorSetting originalSource = BundleCollectorSettingData.Setting;
        string[] names = { "QgfExamples.Modules.Raw", "QgfExamples.Modules.ByFileName" };
        var copyOptions = new Dictionary<string, (string Pipeline, EBundledCopyOption Option)>();
        var builtinFiles = new Dictionary<string, Dictionary<string, byte[]>>();
        var builtinMeta = new Dictionary<string, byte[]>();
        try
        {
            var settings = UnityEngine.Object.Instantiate(moduleSettings);
            AssetDatabase.CreateAsset(settings, Root + "/Settings.asset");
            var collector = UnityEngine.Object.Instantiate(ResourceBuildService.GetCollector(moduleSettings));
            AssetDatabase.CreateAsset(collector, Root + "/Collector.asset");
            BundleCollectorWindow.OpenWindow();
            BundleBuilderWindow.OpenWindow();
            await Task.Delay(800);
            ResourceSettingsEditor.SetCollector(settings, collector);
            await Task.Delay(800);
            var builder = Resources.FindObjectsOfTypeAll<BundleBuilderWindow>().Single();
            var window = Resources.FindObjectsOfTypeAll<BundleCollectorWindow>().Single();
            Require(ReferenceEquals(BundleCollectorSettingData.Setting, collector), "Inspector assignment did not update the native data source.");
            Require(ReferenceEquals(builder.rootVisualElement.Q<ObjectField>("QgfResourceSource").value, collector), "Builder did not follow the assigned Collector.");
            Require(ReferenceEquals(window.rootVisualElement.Q<ListView>("PackageListView").itemsSource, collector.Packages), "Collector window kept its previous package list.");
            Require(settings.Packages.Select(package => package.Name).SequenceEqual(collector.Packages.Select(package => package.PackageName)), "Runtime packages differ from Collector packages.");
            Require(builder.rootVisualElement.Q<Foldout>("QgfBatchBuild") != null, "Native Builder has no batch controls.");
            Require(builder.rootVisualElement.Query<Toggle>().ToList().Count(toggle => toggle.name.StartsWith("QgfBuildPackage.")) == collector.Packages.Count, "Batch package choices are incomplete.");

            string preservedName = settings.DefaultPackageName;
            var serialized = new SerializedObject(settings);
            int preservedIndex = settings.Packages.ToList().FindIndex(package => package.Name == preservedName);
            serialized.FindProperty("m_Packages").GetArrayElementAtIndex(preservedIndex).FindPropertyRelative("m_HostServer").stringValue = "https://example.invalid/content";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(settings.GetPackage(preservedName).HostServer == "https://example.invalid/content", "The package option was not assigned before synchronization.");
            collector.Packages.Add(new BundleCollectorPackage { PackageName = "TemporaryPackage" });
            BundleCollectorSettingData.SaveFile();
            await Task.Delay(1000);
            Require(settings.Packages.Count == collector.Packages.Count && settings.Packages.Any(package => package.Name == "TemporaryPackage"), "Saved package additions were not synchronized.");
            Require(settings.GetPackage(preservedName).HostServer == "https://example.invalid/content", "Synchronization overwrote settings for " + preservedName + ": " + EditorJsonUtility.ToJson(settings));
            Require(builder.rootVisualElement.Q<Toggle>("QgfBuildPackage.TemporaryPackage") != null, "Builder did not show the new package.");
            collector.Packages.RemoveAll(package => package.PackageName == "TemporaryPackage");
            BundleCollectorSettingData.SaveFile();
            await Task.Delay(1000);
            Require(!settings.Packages.Any(package => package.Name == "TemporaryPackage"), "A removed package remained in ResourceSettings.");
            Require(builder.rootVisualElement.Q<Toggle>("QgfBuildPackage.TemporaryPackage") == null, "Batch choices kept a removed package.");

            ResourceSettingsEditor.SetCollector(settings, ResourceBuildService.GetCollector(pointSettings));
            await Task.Delay(500);
            Require(settings.Packages.Count == 1 && settings.DefaultPackageName == pointSettings.DefaultPackageName, "Switching Collector did not replace packages and repair the default.");
            Require(builder.rootVisualElement.Query<Toggle>().ToList().Count(toggle => toggle.name.StartsWith("QgfBuildPackage.")) == 1, "Batch choices retained packages from another Collector.");
            ResourceSettingsEditor.SetCollector(settings, collector);
            await Task.Delay(500);
            string before = EditorJsonUtility.ToJson(collector);
            foreach (string name in names)
            {
                string pipeline = BundleBuilderSetting.GetPackageBuildPipeline(name);
                copyOptions.Add(name, (pipeline, BundleBuilderSetting.GetPackageBundledCopyOption(name, pipeline)));
                BundleBuilderSetting.SetPackageBundledCopyOption(name, pipeline, EBundledCopyOption.ClearAndCopyAll);
                string builtin = Path.GetFullPath(Path.Combine(BundleBuilderHelper.GetStreamingAssetsRoot(), name));
                builtinFiles.Add(builtin, Directory.Exists(builtin)
                    ? Directory.GetFiles(builtin, "*", SearchOption.AllDirectories).ToDictionary(file => Path.GetRelativePath(builtin, file), File.ReadAllBytes)
                    : null);
                builtinMeta.Add(builtin, File.Exists(builtin + ".meta") ? File.ReadAllBytes(builtin + ".meta") : null);
            }
            bool invalidRejected = false;
            try
            {
                ResourceBuildService.BuildPackages(collector, new[] { names[0], "MissingPackage" });
            }
            catch (ArgumentException)
            {
                invalidRejected = true;
            }
            Require(invalidRejected, "Batch build did not validate every package before starting.");
            var outputs = ResourceBuildService.BuildPackages(collector, names);
            Require(outputs.Keys.SequenceEqual(names) && outputs.Values.All(Directory.Exists), "The native batch pipeline did not build both selected packages.");
            Require(builtinFiles.Keys.All(path => File.Exists(Path.Combine(path, "BuiltinCatalog.bytes"))),
                "Batch builds did not copy built-in files to the native StreamingAssets location.");
            Require(before == EditorJsonUtility.ToJson(collector), "Batch build changed the original collection rules.");
            await Task.Delay(700);
            builder.Focus();
            builder.Repaint();
            var batch = builder.rootVisualElement.Q<Foldout>("QgfBatchBuild");
            batch.value = true;
            foreach (var toggle in batch.Query<Toggle>().ToList().Where(toggle => toggle.name.StartsWith("QgfBuildPackage.")))
            {
                toggle.value = names.Any(name => toggle.name == "QgfBuildPackage." + name);
            }
            foreach (string name in names)
            {
                batch.Q<Toggle>("QgfBuildPackage." + name).value = true;
            }
            await Task.Delay(250);
            var buildButton = batch.Q<Button>("QgfBuildSelected");
            Require(buildButton.enabledInHierarchy, "The batch button remained disabled after selecting packages.");
            buildButton.Focus();
            await Task.Delay(150);
            // Batch Mode 不驱动 EditorWindow 的排版，先用窗口尺寸完成布局，再发送原生鼠标事件。
            var panel = buildButton.panel;
            panel.visualTree.style.width = 1200;
            panel.visualTree.style.height = 1000;
            panel.GetType().GetMethod("ValidateLayout", System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Invoke(panel, null);
            Require(!float.IsNaN(buildButton.worldBound.width), "The headless editor could not lay out the Builder.");
            using (var down = PointerDownEvent.GetPooled(new Event
            {
                type = EventType.MouseDown, mousePosition = buildButton.worldBound.center, button = 0, clickCount = 1
            }))
            {
                buildButton.SendEvent(down);
            }
            using (var up = PointerUpEvent.GetPooled(new Event
            {
                type = EventType.MouseUp, mousePosition = buildButton.worldBound.center, button = 0, clickCount = 1
            }))
            {
                buildButton.SendEvent(up);
            }
            await Task.Delay(350);
            Require(batch.Query<Label>().ToList().Any(label => label.text.StartsWith("Built 2 packages.")),
                "Build Selected did not execute both selected packages. Panel: " + (buildButton.panel != null)
                + "; bounds: " + buildButton.worldBound + "; display: " + buildButton.resolvedStyle.display
                + "; labels: " + string.Join(" | ", batch.Query<Label>().ToList().Select(label => label.text)));
            await Task.Delay(700);
            Require(builder.rootVisualElement.Q<Label>("QgfBatchResult").text.StartsWith("Built 2 packages."),
                "The visible batch result was lost when the native window refreshed.");
            Require(settings.GetPackage().PlayMode.GetType() == typeof(EPlayMode), "ResourceSettings does not expose YooAsset's native mode type.");
            return new { passed = true, inspectorAssignment = true, nativeWindowsSynchronized = true, addedAndRemovedPackages = true,
                matchingOptionsPreserved = true, defaultPackageRepaired = true, batchSelection = true, batchButtonInvoked = true, builtPackages = outputs,
                collectorUnchanged = true, nativePlayModes = true, nativeBuiltinOutput = true };
        }
        finally
        {
            foreach (var option in copyOptions)
            {
                BundleBuilderSetting.SetPackageBundledCopyOption(option.Key, option.Value.Pipeline, option.Value.Option);
            }
            foreach (var builtin in builtinFiles)
            {
                Require(Path.GetDirectoryName(builtin.Key) == Path.GetFullPath(BundleBuilderHelper.GetStreamingAssetsRoot()), "Invalid fixture cleanup path.");
                if (Directory.Exists(builtin.Key))
                {
                    Directory.Delete(builtin.Key, true);
                }
                if (builtin.Value != null)
                {
                    foreach (var file in builtin.Value)
                    {
                        string path = Path.Combine(builtin.Key, file.Key);
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        File.WriteAllBytes(path, file.Value);
                    }
                }
                if (builtinMeta[builtin.Key] != null)
                {
                    File.WriteAllBytes(builtin.Key + ".meta", builtinMeta[builtin.Key]);
                }
                else if (File.Exists(builtin.Key + ".meta"))
                {
                    File.Delete(builtin.Key + ".meta");
                }
            }
            Selection.activeObject = originalSource != null ? originalSource : originalSelection;
            await Task.Delay(300);
            Selection.activeObject = originalSelection;
            AssetDatabase.DeleteAsset(Root);
        }
    }

    /// <summary>
    /// 检查验收条件。
    /// </summary>
    /// <param name="condition">验收条件。</param>
    /// <param name="message">失败原因。</param>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
