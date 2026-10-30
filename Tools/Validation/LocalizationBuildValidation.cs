using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

/// <summary>
/// 在隔离工程验证本地化构建检查能拒绝实际错误配置。
/// </summary>
public static class LocalizationBuildValidation
{
    /// <summary>
    /// 修改测试副本并检查错误；字典文件会在结束时还原。
    /// </summary>
    /// <returns>已通过的构建检查名称。</returns>
    public static string[] Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use an idle isolated validation project.");
        }
        EditorSceneManager.OpenScene("Assets/Examples/01_Modules/10_Localization/02_SwitchLanguage/SwitchLanguage.unity");
        LocalizationComponent localization = UnityEngine.Object.FindFirstObjectByType<LocalizationComponent>();
        MethodInfo validate = typeof(UnityQGameFramework.Editor.ResourceBuildService).Assembly
            .GetType("UnityQGameFramework.Editor.LocalizationBuildValidator").GetMethod("Validate", BindingFlags.Static | BindingFlags.NonPublic);
        var checks = new List<string>();
        Action run = () => validate.Invoke(null, new object[] { localization });
        run();
        checks.Add("Actual sample and YooAsset addresses pass");
        const string dictionary = "Assets/Examples/01_Modules/10_Localization/Common/Dictionary.toml";
        byte[] original = File.ReadAllBytes(dictionary);
        string text = File.ReadAllText(dictionary);
        try
        {
            File.WriteAllText(dictionary, text.Replace("{0} received", "{3} received"));
            Reject(run, "argument mismatch", checks);
            File.WriteAllText(dictionary, text.Replace("Coins = \"{0} received", "Other = \"{0} received"));
            Reject(run, "Missing translation", checks);
            File.WriteAllText(dictionary, text.Replace("{0} received", "{0 received"));
            Reject(run, "Localization validation failed", checks);
            File.WriteAllText(dictionary, "[en]\n[en-US]\n[zh-Hans]\n");
            Reject(run, "dictionary is empty", checks);
        }
        finally
        {
            File.WriteAllBytes(dictionary, original);
        }
        LocalizationLanguage[] languages = localization.Languages.ToArray();
        try
        {
            localization.SetLanguages(languages.Select(language => new LocalizationLanguage(language.Language, language.DictionaryAssetName,
                language.FallbackLanguage, language.Assets.Concat(new[] { new LocalizationAssetMapping("Missing", "NotCollected") }).ToArray())).ToArray());
            Reject(run, "not collected", checks);
        }
        finally
        {
            localization.SetLanguages(languages);
        }
        LocalizeText binding = UnityEngine.Object.FindObjectsByType<LocalizeText>(FindObjectsSortMode.None).First();
        string key = binding.Key;
        try
        {
            binding.Key = "Missing.Binding";
            Reject(run, "Missing localization binding", checks);
        }
        finally
        {
            binding.Key = key;
        }
        Reject(() => localization.SetLanguages(
            new LocalizationLanguage(Language.English, "Dictionary", Language.ChineseSimplified),
            new LocalizationLanguage(Language.ChineseSimplified, "Dictionary", Language.English)), "fallback cycle", checks);
        run();
        checks.Add("Restored sample still passes");
        return checks.ToArray();
    }

    /// <summary>
    /// 确认校验拒绝目标错误，避免把测试自身的异常误当作成功。
    /// </summary>
    /// <param name="action">触发配置校验的操作。</param>
    /// <param name="message">预期诊断中的文字。</param>
    /// <param name="checks">接收已验证错误的集合。</param>
    private static void Reject(Action action, string message, List<string> checks)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            if (exception.ToString().IndexOf(message, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                checks.Add("Rejects " + message);
                return;
            }
            throw;
        }
        throw new InvalidOperationException("Validation accepted an invalid configuration: " + message);
    }
}
