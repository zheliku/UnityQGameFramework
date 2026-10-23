using System;
using StarForce;
using UnityQGameFramework.Core;
using Entry = StarForce.GameEntry;

// 仅用于隔离工程验证，检查 Launcher 实际安装的 JSON 辅助器。
public static class StarForceJsonChecks
{
    private static int checks;

    public static int Run()
    {
        checks = 0;
        const string json = "{\"ForceUpdateGame\":true,\"LatestGameVersion\":\"1.2.3 测试\","
            + "\"InternalGameVersion\":7,\"InternalResourceVersion\":11,"
            + "\"UpdatePrefixUri\":\"https://example.invalid/更新/\",\"VersionListLength\":1234,"
            + "\"VersionListHashCode\":-2147483648,\"VersionListCompressedLength\":567,"
            + "\"VersionListCompressedHashCode\":-1234567}";
        var expected = new VersionInfo
        {
            ForceUpdateGame = true, LatestGameVersion = "1.2.3 测试", InternalGameVersion = 7,
            InternalResourceVersion = 11, UpdatePrefixUri = "https://example.invalid/更新/",
            VersionListLength = 1234, VersionListHashCode = int.MinValue,
            VersionListCompressedLength = 567, VersionListCompressedHashCode = -1234567
        };
        var typed = Utility.Json.ToObject<VersionInfo>(json);
        Match(expected, typed);
        var byType = (VersionInfo)Utility.Json.ToObject(typeof(VersionInfo), json);
        Match(expected, byType);
        Require(!ReferenceEquals(typed, byType), "Each JSON read creates its own version object");
        string serialized = Utility.Json.ToJson(typed);
        Match(expected, Utility.Json.ToObject<VersionInfo>(serialized));
        Match(expected, (VersionInfo)Utility.Json.ToObject(typeof(VersionInfo), serialized));
        var missing = Utility.Json.ToObject<VersionInfo>("{}");
        Require(missing != null && !missing.ForceUpdateGame && missing.InternalGameVersion == 0
            && missing.UpdatePrefixUri == null, "Missing JSON fields use default values");
        try
        {
            Utility.Json.ToObject(typeof(VersionInfo), "{");
            throw new Exception("Invalid JSON was accepted");
        }
        catch (FrameworkException exception)
        {
            Require(exception.InnerException is ArgumentException, "Parsing error retains the original exception");
        }

        // Setting 的 Type 入口同样经过 Utility.Json；测试键在保存前移除。
        string key = "QGF.Validation.Json." + Guid.NewGuid().ToString("N");
        try
        {
            Entry.Setting.SetObject<VersionInfo>(key, typed);
            Match(expected, Entry.Setting.GetObject<VersionInfo>(key));
            Match(expected, (VersionInfo)Entry.Setting.GetObject(typeof(VersionInfo), key));
            Entry.Setting.SetObject(key, (object)byType);
            Match(expected, Entry.Setting.GetObject<VersionInfo>(key));
            Match(expected, (VersionInfo)Entry.Setting.GetObject(typeof(VersionInfo), key));
        }
        finally { Entry.Setting.RemoveSetting(key); }
        Require(!Entry.Setting.HasSetting(key), "Validation setting removed");
        return checks;
    }

    private static void Match(VersionInfo expected, VersionInfo actual)
    {
        Require(actual != null, "VersionInfo exists");
        Require(actual.ForceUpdateGame == expected.ForceUpdateGame, "ForceUpdateGame");
        Require(actual.LatestGameVersion == expected.LatestGameVersion, "LatestGameVersion");
        Require(actual.InternalGameVersion == expected.InternalGameVersion, "InternalGameVersion");
        Require(actual.InternalResourceVersion == expected.InternalResourceVersion, "InternalResourceVersion");
        Require(actual.UpdatePrefixUri == expected.UpdatePrefixUri, "UpdatePrefixUri");
        Require(actual.VersionListLength == expected.VersionListLength, "VersionListLength");
        Require(actual.VersionListHashCode == expected.VersionListHashCode, "VersionListHashCode");
        Require(actual.VersionListCompressedLength == expected.VersionListCompressedLength, "VersionListCompressedLength");
        Require(actual.VersionListCompressedHashCode == expected.VersionListCompressedHashCode, "VersionListCompressedHashCode");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("StarForce JSON: " + message);
        checks++;
    }
}
