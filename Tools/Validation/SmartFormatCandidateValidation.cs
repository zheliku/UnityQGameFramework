using System;
using System.Collections.Generic;
using System.Globalization;
using SmartFormat;
using SmartFormat.Extensions;

/// <summary>
/// 验证本地化格式化候选在托管与 AOT 环境中的行为。
/// </summary>
public static class SmartFormatCandidateValidation
{
    /// <summary>
    /// 检查位置参数、命名参数、复数、区域格式及无效占位符。
    /// </summary>
    /// <returns>已通过的检查。</returns>
    public static string[] Run()
    {
        var formatter = new SmartFormatter();
        // 显式登记实际使用的扩展，命名参数使用字典，避免依赖业务类型反射。
        formatter.AddExtensions(new DefaultSource(), new DictionarySource());
        formatter.AddExtensions(new PluralLocalizationFormatter(), new DefaultFormatter());
        var checks = new List<string>();
        CultureInfo english = CultureInfo.GetCultureInfo("en-US");
        Require(formatter.Format(english, "{0} received {1} coins", "Ada", 2) == "Ada received 2 coins", "Existing positional arguments", checks);
        Require(formatter.Format(english, "{0:plural:one coin|many coins}", 1) == "one coin", "English singular", checks);
        Require(formatter.Format(english, "{0:plural:one coin|many coins}", 2) == "many coins", "English plural", checks);
        Require(formatter.Format(CultureInfo.GetCultureInfo("ru-RU"), "{0:plural:one|few|many}", 2) == "few", "Russian few form", checks);
        Require(formatter.Format(CultureInfo.GetCultureInfo("ru-RU"), "{0:plural:one|few|many}", 5) == "many", "Russian many form", checks);
        var arguments = new Dictionary<string, object>
        {
            { "Player", "Ada" },
            { "Count", 2 }
        };
        Require(formatter.Format(english, "{Player}: {Count:plural:coin|coins}", arguments) == "Ada: coins", "Named dictionary arguments", checks);
        Require(formatter.Format(CultureInfo.GetCultureInfo("de-DE"), "{0:N2}", 1234.5) == "1.234,50", "German number format", checks);
        Require(formatter.Format(english, "{0:N2}", 1234.5) == "1,234.50", "English number format", checks);
        Require(formatter.Format(CultureInfo.GetCultureInfo("zh-Hans"), "{0} 获得 {1} 金币", "Ada", 2) == "Ada 获得 2 金币", "Chinese positional arguments", checks);
        bool invalid = false;
        try
        {
            formatter.Format(english, "{Missing}", arguments);
        }
        catch (Exception)
        {
            invalid = true;
        }
        Require(invalid, "Missing placeholders report an error", checks);
        return checks.ToArray();
    }

    /// <summary>
    /// 检查候选库是否满足指定行为。
    /// </summary>
    /// <param name="condition">必须成立的条件。</param>
    /// <param name="description">检查名称。</param>
    /// <param name="checks">接收通过项的集合。</param>
    private static void Require(bool condition, string description, List<string> checks)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
        checks.Add(description);
    }
}
