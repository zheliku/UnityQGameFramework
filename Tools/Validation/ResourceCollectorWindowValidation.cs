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
using YooAsset.Editor;

/// <summary>
/// 验证官方资源窗口切换配置，并检查构建不会修改收集规则。
/// </summary>
public static class ResourceCollectorWindowValidation
{
    /// <summary>
    /// 在隔离工程检查多个原生收集配置。
    /// </summary>
    /// <returns>窗口、保存和构建检查结果。</returns>
    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use the isolated validation project in Edit Mode.");
        }
        BundleCollectorWindow.OpenWindow();
        BundleBuilderWindow.OpenWindow();
        await Task.Delay(1500);
        var collectorWindow = Resources.FindObjectsOfTypeAll<BundleCollectorWindow>().Single();
        var builderWindow = Resources.FindObjectsOfTypeAll<BundleBuilderWindow>().Single();
        Require(builderWindow.rootVisualElement.Q("QgfPlayerBuild") == null, "Player configuration must remain in the Resource Inspector.");
        var sources = AssetDatabase.FindAssets("t:BundleCollectorSetting", new[] { "Assets/Examples" })
            .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<BundleCollectorSetting>).ToArray();
        Require(sources.Length == 3, "Each Sample owns one native YooAsset collector.");
        var results = new List<object>();
        foreach (BundleCollectorSetting source in sources)
        {
            collectorWindow.Focus();
            collectorWindow.Repaint();
            await Task.Delay(500);
            var selector = collectorWindow.rootVisualElement.Q<ObjectField>("QgfResourceSource");
            Require(selector != null, "Native collector includes an asset reference, without nested dropdowns.");
            Selection.activeObject = source;
            await Task.Delay(600);
            Require(BundleCollectorSettingData.Setting == source, "Project selection switches the native collector directly: expected " + AssetDatabase.GetAssetPath(source) + "; actual " + AssetDatabase.GetAssetPath(BundleCollectorSettingData.Setting) + "; panel " + (selector.panel != null));
            var list = collectorWindow.rootVisualElement.Q<ListView>("PackageListView");
            Require(ReferenceEquals(list.itemsSource, source.Packages), "The native list displays the original package list.");
            var packageMenu = builderWindow.rootVisualElement.Q<Toolbar>("Toolbar").Children().OfType<ToolbarMenu>().First();
            string[] packageNames = packageMenu.menu.MenuItems().OfType<DropdownMenuAction>().Select(action => action.name).ToArray();
            Require(packageNames.SequenceEqual(source.Packages.Select(package => package.PackageName)), "The native builder switches its package menu.");
            Require(builderWindow.rootVisualElement.Q("Compression") != null, "The official compression controls remain available.");
            string before = EditorJsonUtility.ToJson(source);
            string output = ResourceBuildService.Build(source, source.Packages[0].PackageName, true);
            Require(before == EditorJsonUtility.ToJson(source), "QGF simulation restores the native collector after building.");
            Require(Directory.Exists(output), "The official simulation pipeline produced its output.");
            results.Add(new { source = AssetDatabase.GetAssetPath(source), packages = packageNames, output });
        }
        var moduleSource = sources.Single(source => source.Packages.Any(package => package.PackageName == "QgfExamples.Modules"));
        collectorWindow.rootVisualElement.Q<ObjectField>("QgfResourceSource").value = moduleSource;
        var selectedPackage = moduleSource.Packages[0];
        string description = selectedPackage.PackageDesc;
        try
        {
            selectedPackage.PackageDesc = "QGF save verification";
            BundleCollectorSettingData.ModifyPackage(selectedPackage);
            BundleCollectorSettingData.SaveFile();
            Require(File.ReadAllText(AssetDatabase.GetAssetPath(moduleSource)).Contains("QGF save verification"), "Native Save writes to the selected Sample asset.");
        }
        finally
        {
            selectedPackage.PackageDesc = description;
            BundleCollectorSettingData.ModifyPackage(selectedPackage);
            BundleCollectorSettingData.SaveFile();
        }
        Require(AssetDatabase.OpenAsset(moduleSource), "Double-click opens the original Collector window.");
        var inspector = UnityEditor.Editor.CreateEditor(moduleSource);
        try
        {
            Require(inspector.GetType().Assembly != typeof(ResourceBuildService).Assembly,
                "QGF must not replace the YooAsset Collector Inspector.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(inspector);
        }
        string temporaryPath = AssetDatabase.GenerateUniqueAssetPath("Assets/QgfCollectorRemovalProbe.asset");
        try
        {
            Require(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(moduleSource), temporaryPath), "Temporary Collector copied for removal check.");
            var temporary = AssetDatabase.LoadAssetAtPath<BundleCollectorSetting>(temporaryPath);
            Selection.activeObject = temporary;
            await Task.Delay(600);
            Require(BundleCollectorSettingData.Setting == temporary, "Temporary Collector selected.");
            Require(AssetDatabase.DeleteAsset(temporaryPath), "Temporary Collector removed.");
            await Task.Delay(1000);
            Require(BundleCollectorSettingData.Setting != null && BundleCollectorSettingData.Setting != temporary,
                "Deleted Collector is replaced by an existing configuration.");
            Require(collectorWindow.rootVisualElement.Q<ObjectField>("QgfResourceSource").value == BundleCollectorSettingData.Setting,
                "The window no longer references the removed asset.");
        }
        finally
        {
            if (AssetDatabase.LoadAssetAtPath<BundleCollectorSetting>(temporaryPath) != null)
            {
                AssetDatabase.DeleteAsset(temporaryPath);
            }
            Selection.activeObject = moduleSource;
        }
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(new { passed = true, sources = results, savedToSelectedSource = true,
            assetSelection = true, deletedSourceRemoved = true }, Newtonsoft.Json.Formatting.Indented);
        Directory.CreateDirectory("Artifacts~/ResourceExamples");
        File.WriteAllText("Artifacts~/ResourceExamples/native-windows.json", json);
        return Newtonsoft.Json.Linq.JObject.Parse(json);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
