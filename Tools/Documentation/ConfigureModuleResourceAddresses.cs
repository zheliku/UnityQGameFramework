using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityQGameFramework.Editor;
using YooAsset.Editor;

/// <summary>
/// 为模块示例配置可读的资源地址。
/// </summary>
public static class ConfigureModuleResourceAddresses
{
    /// <summary>
    /// 更新隔离工程中的官方收集配置。
    /// </summary>
    /// <returns>生成的分组名称。</returns>
    public static object Run()
    {
        if (!UnityEngine.Application.dataPath.Contains("Artifacts~"))
        {
            throw new InvalidOperationException("Use the isolated authoring project.");
        }
        const string root = "Assets/Examples/01_Modules/UnityQGameFramework/Resource/Editor/";
        var source = AssetDatabase.LoadAssetAtPath<BundleCollectorSetting>(root + "YooAssetCollector.asset");
        BundleCollectorPackage package = source.GetPackage("QgfExamples.Modules");
        var groups = new List<BundleCollectorGroup>();
        foreach (BundleCollector collector in package.Groups.SelectMany(group => group.Collectors))
        {
            string path = AssetDatabase.GUIDToAssetPath(collector.CollectorGUID);
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Missing collector: " + collector.CollectPath);
            }
            collector.CollectPath = path;
            string name = GetGroup(path);
            collector.AddressRuleName = nameof(AddressByGroupAndFileName);
            collector.FilterRuleName = path.Contains("/13_Entity/") || path.Contains("/14_UI/") || path.Contains("/08_ObjectPool/") || path.Contains("/12_Resource/")
                ? nameof(CollectPrefab) : path.Contains("/16_Scene/") ? nameof(CollectScene) : nameof(CollectAll);
            groups.Add(new BundleCollectorGroup
            {
                GroupName = name,
                AssetTags = name,
                Collectors = new List<BundleCollector> { collector }
            });
        }
        package.EnableAddressable = true;
        package.Groups = groups;
        var raw = source.GetPackage("QgfExamples.Modules.Raw");
        raw.EnableAddressable = true;
        foreach (BundleCollector collector in raw.Groups.SelectMany(group => group.Collectors))
        {
            collector.AddressRuleName = nameof(AddressByFileName);
        }
        EditorUtility.SetDirty(source);
        AssetDatabase.SaveAssetIfDirty(source);
        return new { groups = groups.Select(group => group.GroupName).ToArray() };
    }

    private static string GetGroup(string path)
    {
        var names = new Dictionary<string, string>
        {
            { "/08_ObjectPool/", "ObjectPool" }, { "/12_Resource/", "Resource" },
            { "/13_Entity/01_ShowHide/", "Entity_ShowHide" }, { "/13_Entity/02_Reuse/", "Entity_Reuse" },
            { "/14_UI/01_OpenWindow/", "UI_OpenWindow" }, { "/14_UI/02_Reopen/", "UI_Reopen" }, { "/14_UI/03_Cover/", "UI_Cover" },
            { "/15_Sound/01_OneShot/", "Sound_OneShot" }, { "/15_Sound/02_GroupVolume/", "Sound_GroupVolume" }, { "/15_Sound/03_Spatial/", "Sound_Spatial" },
            { "/16_Scene/01_LoadRoom/", "Scene_LoadRoom" }, { "/16_Scene/02_SwitchRooms/", "Scene_SwitchRooms" },
            { "/Common/UI", "ExampleUI" }, { "/11_DataTable/", "Tables" }, { "/10_Localization/", "Localization" },
            { "/02_Config/01_ReadValues/", "Config_Values" }, { "/02_Config/02_LoadAsset/", "Config_Asset" }
        };
        return names.Single(entry => path.Contains(entry.Key)).Value;
    }
}
