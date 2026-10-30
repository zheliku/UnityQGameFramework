using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

/// <summary>
/// 在专用目录验证设置文件的提交、备份及故障恢复。
/// </summary>
public static class SettingPersistenceValidation
{
    /// <summary>
    /// 检查真实文件故障和原有序列化格式，不访问玩家文件。
    /// </summary>
    /// <returns>已通过的持久化检查名称。</returns>
    public static string[] Run()
    {
        var checks = new List<string>();
        string root = Path.GetFullPath("Artifacts~/SettingValidation/" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        SettingValidationHelper.PathForTest = Path.Combine(root, "Settings.dat");
        var owner = new GameObject("Setting Validation");
        owner.SetActive(false);
        var helper = owner.AddComponent<SettingValidationHelper>();
        typeof(DefaultSettingHelper).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(helper, null);
        try
        {
            Require(helper.Load(), "Absent settings use defaults", checks);
            helper.SetInt("Score", 1);
            Require(helper.Save() && !File.Exists(helper.FilePath + ".tmp"), "First save commits and cleans its temporary file", checks);
            byte[] first = File.ReadAllBytes(helper.FilePath);
            using (var legacy = new MemoryStream())
            {
                using (var writer = new BinaryWriter(legacy, Encoding.UTF8, true))
                {
                    writer.Write(new byte[] { (byte)'G', (byte)'F', (byte)'S', 0 });
                    writer.Write7BitEncodedInt32(1);
                    writer.Write("Score");
                    writer.Write("1");
                }
                Require(first.SequenceEqual(legacy.ToArray()), "Original GFS version zero bytes are unchanged", checks);
            }
            helper.SetInt("Score", 2);
            Require(helper.Save() && File.ReadAllBytes(helper.FilePath + ".bak").SequenceEqual(first), "Replacement retains the previous committed file", checks);
            helper.SetInt("Score", 9);
            Require(helper.Load() && helper.GetInt("Score") == 2, "Load replaces the complete memory snapshot", checks);
            using (var locked = new FileStream(helper.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Require(helper.Load() && helper.GetInt("Score") == 1, "Unreadable primary attempts the backup", checks);
            }
            Require(helper.Load() && helper.GetInt("Score") == 2, "Primary remains readable after its lock is released", checks);
            File.WriteAllBytes(helper.FilePath, first.Take(first.Length - 1).ToArray());
            Require(helper.Load() && helper.GetInt("Score") == 1, "Truncated primary recovers from backup", checks);
            helper.SetInt("Score", 3);
            Require(helper.Save() && File.ReadAllBytes(helper.FilePath + ".bak").SequenceEqual(first), "Recovery retains the valid backup", checks);
            byte[] committed = File.ReadAllBytes(helper.FilePath);
            helper.Serializer.RegisterSerializeCallback(0, (stream, setting) => false);
            Require(!helper.Save() && File.ReadAllBytes(helper.FilePath).SequenceEqual(committed) && !File.Exists(helper.FilePath + ".tmp"),
                "Rejected serialization preserves the file", checks);
            helper.Serializer.RegisterSerializeCallback(0, (stream, setting) => throw new IOException("Injected write failure"));
            Require(!helper.Save() && File.ReadAllBytes(helper.FilePath).SequenceEqual(committed) && !File.Exists(helper.FilePath + ".tmp"),
                "Serialization exception preserves the file", checks);
            helper.Serializer.RegisterSerializeCallback(0, (stream, setting) =>
            {
                setting.Serialize(stream);
                return true;
            });
            using (var locked = new FileStream(helper.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Require(!helper.Save() && File.ReadAllBytes(helper.FilePath).SequenceEqual(committed), "Replacement failure preserves the file", checks);
            }
            Require(helper.Save(), "Save can retry after a replacement failure", checks);
            var alternate = new DefaultSetting();
            alternate.SetInt("Other", 8);
            using (var stream = new MemoryStream())
            {
                Require(helper.Serializer.Serialize(stream, alternate) && stream.CanWrite, "Serialization leaves caller-owned streams open", checks);
                stream.Position = 0;
                DefaultSetting read = helper.Serializer.Deserialize(stream);
                Require(read.GetInt("Other") == 8 && !read.HasSetting("Score") && helper.HasSetting("Score") && stream.CanRead,
                    "Callbacks use independent snapshots and the supplied data", checks);
            }
            using (var broken = new MemoryStream(new byte[] { 1, 1, (byte)'A' }))
            {
                try
                {
                    alternate.Deserialize(broken);
                    throw new InvalidOperationException("Truncated data was accepted.");
                }
                catch (EndOfStreamException)
                {
                    Require(alternate.GetInt("Other") == 8 && broken.CanRead, "Failed parsing preserves memory and stream ownership", checks);
                }
            }
            File.WriteAllBytes(helper.FilePath, new byte[] { 0 });
            File.WriteAllBytes(helper.FilePath + ".bak", new byte[] { 0 });
            Require(!helper.Load() && helper.GetInt("Score") == 3, "Two damaged files preserve the memory snapshot", checks);
            var manager = (ISettingManager)Activator.CreateInstance(typeof(ISettingManager).Assembly.GetType("UnityQGameFramework.Core.SettingManager", true));
            manager.SetSettingHelper(helper);
            Require(!manager.Load(), "Load failure reaches the manager", checks);
            manager.GetType().GetMethod("Shutdown", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            Require(File.ReadAllBytes(helper.FilePath).SequenceEqual(new byte[] { 0 }), "Failed load suppresses shutdown autosave", checks);
            Require(manager.Save(), "Explicit save can recover after a failed load", checks);
            File.Delete(helper.FilePath);
            File.WriteAllBytes(helper.FilePath + ".bak", first);
            Require(helper.Load() && helper.GetInt("Score") == 1, "Backup recovers a missing primary", checks);
            File.WriteAllBytes(helper.FilePath, first.Concat(new byte[] { 0 }).ToArray());
            File.Delete(helper.FilePath + ".bak");
            Require(!helper.Load() && helper.GetInt("Score") == 1, "Trailing bytes never commit partial data", checks);
            return checks.ToArray();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }

    /// <summary>
    /// 保存通过的持久化断言。
    /// </summary>
    /// <param name="condition">必须成立的条件。</param>
    /// <param name="message">检查名称。</param>
    /// <param name="checks">接收通过项的集合。</param>
    private static void Require(bool condition, string message, List<string> checks)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
        checks.Add(message);
    }
}

/// <summary>
/// 只为验收指定专用设置文件路径。
/// </summary>
public sealed class SettingValidationHelper : DefaultSettingHelper
{
    /// <summary>
    /// 本次测试专用的文件路径。
    /// </summary>
    public static string PathForTest;

    /// <summary>
    /// 返回隔离文件路径。
    /// </summary>
    /// <returns>本次测试的设置文件。</returns>
    protected override string GetFilePath()
    {
        return PathForTest;
    }
}
