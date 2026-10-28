using System;
using StarForce;
using UnityQGameFramework.Core;
using Entry = StarForce.GameEntry;

/// <summary>
/// 检查 Launcher 使用的 JSON 辅助器和 Setting 对象存取。
/// </summary>
public static class StarForceJsonChecks
{
    private static int checks;

    /// <summary>
    /// 执行 JSON 检查。
    /// </summary>
    /// <returns>通过的检查数量。</returns>
    public static int Run()
    {
        checks = 0;
        const string json = "{\"GameVersion\":\"1.2.3 测试\",\"InternalGameVersion\":7,"
            + "\"WindowsAppUrl\":\"https://example.invalid/下载/\"}";
        var expected = new JsonData
        {
            GameVersion = "1.2.3 测试",
            InternalGameVersion = 7,
            WindowsAppUrl = "https://example.invalid/下载/"
        };
        var typed = Utility.Json.ToObject<JsonData>(json);
        Match(expected, typed);
        var byType = (JsonData)Utility.Json.ToObject(typeof(JsonData), json);
        Match(expected, byType);
        Require(!ReferenceEquals(typed, byType), "Each JSON read creates its own object");
        string serialized = Utility.Json.ToJson(typed);
        Match(expected, Utility.Json.ToObject<JsonData>(serialized));
        Match(expected, (JsonData)Utility.Json.ToObject(typeof(JsonData), serialized));
        var missing = Utility.Json.ToObject<JsonData>("{}");
        Require(missing != null && missing.GameVersion == null && missing.InternalGameVersion == 0
            && missing.WindowsAppUrl == null, "Missing JSON fields use default values");
        try
        {
            Utility.Json.ToObject(typeof(JsonData), "{");
            throw new Exception("Invalid JSON was accepted");
        }
        catch (GameFrameworkException exception)
        {
            Require(exception.InnerException is ArgumentException, "Parsing error retains the original exception");
        }

        // Setting 的 Type 入口同样经过 Utility.Json；测试键在保存前移除。
        string key = "QGF.Validation.Json." + Guid.NewGuid().ToString("N");
        try
        {
            Entry.Setting.SetObject<JsonData>(key, typed);
            Match(expected, Entry.Setting.GetObject<JsonData>(key));
            Match(expected, (JsonData)Entry.Setting.GetObject(typeof(JsonData), key));
            Entry.Setting.SetObject(key, (object)byType);
            Match(expected, Entry.Setting.GetObject<JsonData>(key));
            Match(expected, (JsonData)Entry.Setting.GetObject(typeof(JsonData), key));
        }
        finally { Entry.Setting.RemoveSetting(key); }
        Require(!Entry.Setting.HasSetting(key), "Validation setting removed");
        return checks;
    }

    private static void Match(JsonData expected, JsonData actual)
    {
        Require(actual != null, "JsonData exists");
        Require(actual.GameVersion == expected.GameVersion, "GameVersion");
        Require(actual.InternalGameVersion == expected.InternalGameVersion, "InternalGameVersion");
        Require(actual.WindowsAppUrl == expected.WindowsAppUrl, "WindowsAppUrl");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new Exception("StarForce JSON: " + message);
        checks++;
    }

    // Unity JSON 辅助器序列化字段，验证数据不依赖已经移除的旧版本清单类型。
    [Serializable]
    private sealed class JsonData
    {
        /// <summary>
        /// 游戏版本。
        /// </summary>
        public string GameVersion;

        /// <summary>
        /// 内部游戏版本。
        /// </summary>
        public int InternalGameVersion;

        /// <summary>
        /// 下载地址。
        /// </summary>
        public string WindowsAppUrl;
    }
}
