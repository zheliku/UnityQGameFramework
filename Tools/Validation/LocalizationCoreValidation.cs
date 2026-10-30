using System;
using System.Collections.Generic;
using System.Linq;
using UnityQGameFramework.Core;

/// <summary>
/// 检查语言标识、完整提交和 SmartFormat 的 Core 契约。
/// </summary>
public static class LocalizationCoreValidation
{
    /// <summary>
    /// 在独立管理器上执行检查，不修改运行中的游戏状态。
    /// </summary>
    /// <returns>已通过的检查名称。</returns>
    public static string[] Run()
    {
        var checks = new List<string>();
        var manager = (ILocalizationManager)Activator.CreateInstance(typeof(ILocalizationManager).Assembly.GetType("UnityQGameFramework.Core.LocalizationManager", true));
        Require(Language.FromCode("en-us") == Language.EnglishUnitedStates, "Canonical language code", checks);
        Require(Language.FromCode("zh-TW").Code == "zh-TW", "Region language values retain their exact code", checks);
        Require(Language.FromCode("pt-BR") == Language.PortugueseBrazil, "Brazilian Portuguese mapping", checks);
        Language projectLanguage = Language.FromCode("es-MX");
        Require(projectLanguage == Language.FromCode("es-mx") && projectLanguage.Parent == Language.Spanish,
            "Project language values are extensible, canonical and comparable", checks);
        Require(!Language.TryParse("not_a_language", out Language invalid) && invalid == Language.Unspecified,
            "Invalid external language codes are rejected", checks);
        Require(typeof(ILocalizationManager).GetMethods().Where(method => method.Name == "SetLanguage")
            .All(method => method.GetParameters()[0].ParameterType == typeof(Language)), "Only strong language values enter language APIs", checks);
        var english = new Dictionary<string, string>
        {
            { "Game.Name", "Hello" },
            { "Coins", "{0:plural:one coin|many coins}" },
            { "Named", "{Player} has {Count} coins" },
            { "Number", "{0:N2}" }
        };
        int notifications = 0;
        manager.LanguageChanged += (sender, args) =>
        {
            Require(manager.Language == args.Language && manager.GetString("Game.Name") != null,
                "Language notification observes the committed dictionary", checks);
            notifications++;
        };
        manager.SetLanguage(Language.EnglishUnitedStates, english);
        Require(manager.Language == Language.EnglishUnitedStates && notifications == 1, "One notification per successful commit", checks);
        english["Game.Name"] = "Changed by caller";
        Require(manager.GetString("Game.Name") == "Hello", "Dictionary ownership is independent of the caller", checks);
        Require(manager.GetString("Coins", 1) == "one coin" && manager.GetString("Coins", 5) == "many coins", "Plural formatting", checks);
        Require(manager.GetString("Number", 1234.5) == "1,234.50", "English regional numeric formatting", checks);
        Require(manager.GetString("Named", new Dictionary<string, object> { { "Player", "Ada" }, { "Count", 3 } }) == "Ada has 3 coins", "Named dictionary arguments", checks);
        Require(manager.GetString("Missing") == "<NoKey>Missing", "Missing key diagnostic", checks);
        Require(manager.GetString("Named", new Dictionary<string, object>()).StartsWith("<Error>", StringComparison.Ordinal), "Missing placeholder diagnostic", checks);
        bool rejected = false;
        try
        {
            manager.SetLanguage(Language.FromCode("de-DE"), new Dictionary<string, string> { { "Broken", "{0" } });
        }
        catch (Exception)
        {
            rejected = true;
        }
        Require(rejected && manager.Language == Language.EnglishUnitedStates && manager.GetString("Game.Name") == "Hello" && notifications == 1,
            "A malformed dictionary preserves language, values and notification count", checks);
        manager.SetLanguage(Language.FromCode("de-DE"), new Dictionary<string, string> { { "Game.Name", "Hallo" }, { "Number", "{0:N2}" } });
        Require(manager.GetString("Number", 1234.5) == "1.234,50", "Formatting uses the selected language culture", checks);
        Require(TomlLocalizationParser.Parse("[en-US.Game]\nName = \"Regional\"\n", Language.EnglishUnitedStates)["Game.Name"] == "Regional", "Standard TOML language section", checks);
        Require(TomlLocalizationParser.Parse("[en.Game]\nName = \"Legacy\"\n", Language.English)["Game.Name"] == "Legacy", "TOML uses the same standard language identifiers", checks);
        Require(TomlLocalizationParser.Parse("[en.Game]\nName = \"Parent\"\n[en-US.Game]\nName = \"Regional\"\n", Language.EnglishUnitedStates)["Game.Name"] == "Regional", "Regional TOML overrides parent language", checks);
        Require(TomlLocalizationParser.Parse("[en.Game]\nName = \"Parent\"\n", Language.EnglishUnitedStates)["Game.Name"] == "Parent", "Regional TOML falls back to parent language", checks);
        Require(!typeof(ILocalizationManager).Assembly.GetReferencedAssemblies().Any(assembly => assembly.Name.StartsWith("UnityEngine", StringComparison.Ordinal)), "Core remains independent of UnityEngine", checks);
        return checks.ToArray();
    }

    /// <summary>
    /// 保存已通过的断言。
    /// </summary>
    /// <param name="condition">必须成立的条件。</param>
    /// <param name="description">检查说明。</param>
    /// <param name="checks">通过项集合。</param>
    private static void Require(bool condition, string description, List<string> checks)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
        checks.Add(description);
    }
}
