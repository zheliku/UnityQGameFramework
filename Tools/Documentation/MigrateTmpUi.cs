using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityQGameFramework.Runtime;

/// <summary>
/// 为示例生成可独立分发的 TMP 字体和迁移模板。
/// </summary>
public static class MigrateTmpUi
{
    /// <summary>
    /// 完成输入框、下拉框、本地化绑定和序列化引用校验。
    /// </summary>
    /// <returns>已处理的场景和预制体数量。</returns>
    public static int Finish()
    {
        AssetDatabase.Refresh();
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (setup.Any(item => item.isLoaded && UnityEngine.SceneManagement.SceneManager.GetSceneByPath(item.path).isDirty))
        {
            throw new InvalidOperationException("Save the current scenes before migrating UI assets.");
        }
        int count = 0;
        try
        {
            foreach (string id in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Examples" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    FinishRoot(root, path);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    count++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            foreach (string id in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Examples" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                var scene = EditorSceneManager.OpenScene(path);
                if (path.Contains("01_Modules") && path.Contains("14_UI") == false)
                {
                    Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
                    if (camera != null && camera.orthographic && camera.transform.position.x < -1f)
                    {
                        camera.orthographicSize = 4.5f / (.44f * .6f);
                    }
                }
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    FinishRoot(root, path);
                }
                EditorSceneManager.SaveScene(scene);
                count++;
            }
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (setup.Any(item => item.isActive && !string.IsNullOrEmpty(item.path)))
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            }
        }
        File.WriteAllText("Artifacts~/TmpMigration/finalized.json", JsonUtility.ToJson(new Result { assets = count }));
        return count;
    }

    /// <summary>
    /// 保存 TMP 交互控件的视口和 StarForce 的自动翻译绑定。
    /// </summary>
    /// <param name="root">资产根节点。</param>
    /// <param name="path">用于识别示例的资产路径。</param>
    private static void FinishRoot(GameObject root, string path)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == null)
            {
                continue;
            }
            if (child.name == "Panel_LanguageTips")
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
                continue;
            }
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
            {
                throw new InvalidOperationException("Missing script: " + path + " / " + child.name);
            }
        }
        foreach (TMP_InputField input in root.GetComponentsInChildren<TMP_InputField>(true))
        {
            input.textViewport = (RectTransform)input.transform;
            input.textComponent = input.GetComponentsInChildren<TextMeshProUGUI>(true).Single(text => text != input.placeholder);
            if (input.textComponent == null)
            {
                throw new InvalidOperationException("Input field is missing its text: " + path);
            }
        }
        foreach (TMP_Dropdown dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true))
        {
            if (dropdown.captionText == null || dropdown.itemText == null || dropdown.template == null)
            {
                throw new InvalidOperationException("Dropdown references were not preserved: " + path);
            }
        }
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.font == null)
            {
                throw new InvalidOperationException("Text is missing its font: " + path + " / " + text.name);
            }
            if (path.Contains("02_StarForce") && !string.IsNullOrWhiteSpace(text.text)
                && text.text.IndexOf(' ') < 0 && text.text.Contains(".") && !text.text.Contains("/"))
            {
                // StarForce 原预制体保存的是字典键；改为显式绑定并由语言事件更新。
                LocalizeText binding = text.GetComponent<LocalizeText>() ?? text.gameObject.AddComponent<LocalizeText>();
                binding.Key = text.text;
                binding.FontKey = "UI.Font";
            }
        }
    }

    /// <summary>
    /// 记录已保存的资产数量。
    /// </summary>
    [Serializable]
    private sealed class Result
    {
        /// <summary>
        /// 已处理的场景和预制体数量。
        /// </summary>
        public int assets;
    }

    /// <summary>
    /// 将字体图集保存为 PNG，避免文本资产内嵌大量十六进制像素。
    /// </summary>
    /// <returns>已压缩存储的图集数量。</returns>
    public static int PackFontAtlases()
    {
        string[] paths =
        {
            "Assets/Examples/00_PointPointPoint/GameMain/Fonts/DefaultFont.asset",
            "Assets/Examples/01_Modules/10_Localization/Common/ChineseFont.asset",
            "Assets/Examples/02_StarForce/GameMain/Fonts/MainFont.asset"
        };
        int count = 0;
        foreach (string path in paths)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            Texture2D[] previous = font.atlasTextures;
            var textures = new Texture2D[previous.Length];
            for (int index = 0; index < previous.Length; index++)
            {
                string atlas = Path.ChangeExtension(path, null) + "Atlas" + index + ".png";
                File.WriteAllBytes(atlas, previous[index].EncodeToPNG());
                AssetDatabase.ImportAsset(atlas);
                var importer = (TextureImporter)AssetImporter.GetAtPath(atlas);
                importer.sRGBTexture = false;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = false;
                importer.SaveAndReimport();
                textures[index] = AssetDatabase.LoadAssetAtPath<Texture2D>(atlas);
                if (AssetDatabase.IsSubAsset(previous[index]))
                {
                    UnityEngine.Object.DestroyImmediate(previous[index], true);
                }
                count++;
            }
            font.atlasTextures = textures;
            font.material.shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            font.material.SetTexture("_MainTex", textures[0]);
            EditorUtility.SetDirty(font);
            EditorUtility.SetDirty(font.material);
        }
        AssetDatabase.SaveAssets();
        return count;
    }

    /// <summary>
    /// 生成字体与标准组件模板，供序列化迁移保留原有组件引用。
    /// </summary>
    /// <returns>已保存的字体路径。</returns>
    public static string[] Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Stop Play Mode before migrating UI assets.");
        }
        AssetDatabase.Refresh();
        TMP_Settings.LoadDefaultSettings();
        string characters = File.ReadAllText("Artifacts~/ModernizationResearch/tmp-ui-characters.txt");
        string[] sources =
        {
            "Assets/Examples/00_PointPointPoint/GameMain/Fonts/DefaultFont.otf",
            "Assets/Examples/01_Modules/10_Localization/Common/ChineseFontSource.otf",
            "Assets/Examples/02_StarForce/GameMain/Fonts/MainFont.otf"
        };
        string[] paths = sources.Select(path => Path.ChangeExtension(path.Replace("Source.otf", ".otf"), ".asset")).ToArray();
        foreach (string source in sources)
        {
            string path = Path.ChangeExtension(source.Replace("Source.otf", ".otf"), ".asset");
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null)
            {
                continue;
            }
            Font font = AssetDatabase.LoadAssetAtPath<Font>(source);
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 48, 5, GlyphRenderMode.SDFAA, 2048, 2048);
            asset.name = Path.GetFileNameWithoutExtension(source);
            if (!asset.TryAddCharacters(characters, out string missing))
            {
                throw new InvalidOperationException("Font atlas did not include all sample characters: " + missing);
            }
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (Texture2D texture in asset.atlasTextures)
            {
                AssetDatabase.AddObjectToAsset(texture, asset);
            }
            EditorUtility.SetDirty(asset);
        }
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Artifacts~/TmpMigration");
        var template = new GameObject("Text_TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        TextMeshProUGUI text = template.GetComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(paths[1]);
        text.fontSize = 20;
        text.text = "Template";
        text.raycastTarget = false;
        PrefabUtility.SaveAsPrefabAsset(template, "Assets/Examples/01_Modules/TmpMigrationTemplate.prefab");
        UnityEngine.Object.DestroyImmediate(template);
        File.Copy("Assets/Examples/01_Modules/TmpMigrationTemplate.prefab", "Artifacts~/TmpMigration/Template.prefab", true);
        AssetDatabase.DeleteAsset("Assets/Examples/01_Modules/TmpMigrationTemplate.prefab");
        return paths;
    }
}
