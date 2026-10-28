using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityQGameFramework.Examples.Modules;

/// <summary>
/// 为寻址规则示例复用现有的场景和下拉框样式。
/// </summary>
public static class BuildResourceAddressScene
{
    /// <summary>
    /// 在隔离工程生成场景。
    /// </summary>
    /// <returns>生成的场景路径。</returns>
    public static object Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use the isolated authoring project in Edit Mode.");
        }
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (setup.Any(item => item.isLoaded && SceneManager.GetSceneByPath(item.path).isDirty))
        {
            throw new InvalidOperationException("Save scenes first.");
        }
        const string root = "Assets/Examples/01_Modules/12_Resource/";
        const string output = root + "02_AddressRules/LoadAddressRules.unity";
        try
        {
            if (!File.Exists(output) && !AssetDatabase.CopyAsset(root + "03_Package/LoadResourcePackage.unity", output))
            {
                throw new IOException("Cannot copy the resource scene.");
            }
            var scene = EditorSceneManager.OpenScene(output);
            var previous = UnityEngine.Object.FindFirstObjectByType<LoadResourcePackage>();
            if (previous == null)
            {
                return new { scene = output, alreadyCreated = true };
            }
            var oldFields = new SerializedObject(previous);
            var state = oldFields.FindProperty("stateText").objectReferenceValue;
            var rules = oldFields.FindProperty("groupsText").objectReferenceValue;
            var help = (Text)oldFields.FindProperty("assetText").objectReferenceValue;
            var load = (Button)oldFields.FindProperty("loadButton").objectReferenceValue;
            var release = (Button)oldFields.FindProperty("releaseButton").objectReferenceValue;
            var initialize = (Button)oldFields.FindProperty("initializeButton").objectReferenceValue;
            Transform parent = initialize.transform.parent;
            int sibling = initialize.transform.GetSiblingIndex();
            var sourceScene = EditorSceneManager.OpenScene(root + "05_Update/UpdateRemoteResources.unity", OpenSceneMode.Additive);
            var source = sourceScene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<Dropdown>(true)).First();
            var dropdown = UnityEngine.Object.Instantiate(source.gameObject).GetComponent<Dropdown>();
            SceneManager.MoveGameObjectToScene(dropdown.gameObject, scene);
            dropdown.transform.SetParent(parent, false);
            dropdown.transform.SetSiblingIndex(sibling);
            dropdown.name = "Dropdown_Rule";
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string> { "Filename → Cube", "Folder and Filename → Art_Cube", "Group and Filename → Demo_Cube" });
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            EditorSceneManager.CloseScene(sourceScene, true);
            UnityEngine.Object.DestroyImmediate(initialize.gameObject);
            GameObject owner = previous.gameObject;
            UnityEngine.Object.DestroyImmediate(previous);
            owner.name = "LoadAddressRules";
            var example = owner.AddComponent<LoadAddressRules>();
            var fields = new SerializedObject(example);
            fields.FindProperty("stateText").objectReferenceValue = state;
            fields.FindProperty("ruleText").objectReferenceValue = rules;
            fields.FindProperty("ruleDropdown").objectReferenceValue = dropdown;
            fields.FindProperty("loadButton").objectReferenceValue = load;
            fields.FindProperty("releaseButton").objectReferenceValue = release;
            fields.ApplyModifiedPropertiesWithoutUndo();
            load.GetComponentInChildren<Text>().text = "Load Cube by Address";
            release.GetComponentInChildren<Text>().text = "Destroy Instance and Release Asset";
            help.text = "Three packages collect the same prefab. Release it, then switch rules to compare addresses.";
            foreach (Text text in scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<Text>(true)))
            {
                if (text.name == "Text_Title")
                {
                    text.text = "02  Addressing Rules and String Addresses";
                }
            }
            EditorSceneManager.SaveScene(scene);
            return new { scene = output, dropdowns = 1 };
        }
        finally
        {
            if (setup.Any(item => item.isActive && item.isLoaded && !string.IsNullOrEmpty(item.path)))
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
