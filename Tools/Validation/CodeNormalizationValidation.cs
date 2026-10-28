using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Runtime;

/// <summary>
/// 验证基础类型、模块命名空间和示例程序集的统一结果。
/// </summary>
public static class CodeNormalizationValidation
{
    /// <summary>
    /// 检查全部 Core 管理器的创建与缓存，以及三个示例的程序集归属。
    /// </summary>
    /// <returns>管理器数量、示例程序集和检查结果。</returns>
    public static object Audit()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Run the audit in Edit Mode.");
        Assembly core = typeof(GameFrameworkEntry).Assembly;
        Type[] types = core.GetTypes();
        Require(types.All(type => IsGenerated(type) || type.Namespace == (IsModuleNotification(type)
            ? "UnityQGameFramework.Core.Events" : "UnityQGameFramework.Core")),
            "Only module notifications belong to Core.Events; other Core types must remain at the root.");
        Type[] notifications = types.Where(IsModuleNotification).ToArray();
        Require(notifications.Length == 60, "Expected all 60 Core module and helper notifications.");
        string[] rootNames = core.GetExportedTypes().Where(type => type.Namespace == "UnityQGameFramework.Core")
            .Select(type => type.Name).ToArray();
        string[] runtimeNames = typeof(BaseComponent).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "UnityQGameFramework.Runtime").Select(type => type.Name).ToArray();
        Require(!rootNames.Intersect(runtimeNames).Any(), "Core and Runtime imports must not contain ambiguous type names.");
        Require(typeof(IEntityManager).GetEvent("ShowEntitySuccess").EventHandlerType.GenericTypeArguments[0]
            == typeof(UnityQGameFramework.Core.Events.ShowEntitySuccessEventArgs), "Core managers must keep their own notification contracts.");
        Require(typeof(ShowEntitySuccessEventArgs).BaseType == typeof(GameEventArgs),
            "Business code must resolve the Runtime event through ordinary Core and Runtime imports.");
        Require(!core.GetReferencedAssemblies().Any(assembly => assembly.Name.StartsWith("UnityEngine", StringComparison.Ordinal)),
            "Core must remain independent of Unity.");
        Require(!types.Any(type => type.Name.StartsWith("Framework", StringComparison.Ordinal)), "A shortened foundational type remains.");

        Type[] contracts = types.Where(type => type.IsInterface && type.Name.EndsWith("Manager", StringComparison.Ordinal)).ToArray();
        MethodInfo getModule = typeof(GameFrameworkEntry).GetMethod(nameof(GameFrameworkEntry.GetModule), BindingFlags.Public | BindingFlags.Static);
        try
        {
            foreach (Type contract in contracts)
            {
                MethodInfo method = getModule.MakeGenericMethod(contract);
                object first = method.Invoke(null, null);
                Require(first != null && ReferenceEquals(first, method.Invoke(null, null)), "Module cache failed: " + contract.FullName);
            }
        }
        finally
        {
            GameFrameworkEntry.Shutdown();
            GameFrameworkLog.SetLogHelper(new DefaultLogHelper());
        }

        Require(contracts.Length == 19, "Expected all 19 Core managers.");
        var samples = new Dictionary<string, string>();
        foreach (string folder in new[] { "00_PointPointPoint", "01_Modules", "02_StarForce" })
        {
            string root = "Assets/Examples/" + folder;
            string[] definitions = Directory.GetFiles(root, "*.asmdef", SearchOption.AllDirectories);
            Require(definitions.Length == 1 && Path.GetDirectoryName(definitions[0]).Replace('\\', '/') == root,
                "Each Sample must have one root assembly: " + folder);
            string expected = "UnityQGameFramework.Examples." + folder.Substring(3);
            Require(Path.GetFileNameWithoutExtension(definitions[0]) == expected, "Unexpected Sample assembly: " + folder);
            samples.Add(folder, expected);
        }

        var result = new
        {
            passed = true,
            managers = contracts.Length,
            coreTypes = types.Length,
            moduleNotifications = notifications.Length,
            rootNameConflicts = rootNames.Intersect(runtimeNames).ToArray(),
            samples
        };
        Directory.CreateDirectory("Artifacts~/CodeStyle");
        File.WriteAllText("Artifacts~/CodeStyle/normalization.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }

    /// <summary>
    /// 使用迁移后的模板重新生成当前项目的配置工作区。
    /// </summary>
    /// <returns>生成成功的 Profile 路径。</returns>
    public static async Task<object> GenerateTables()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play Mode before generating tables.");
        string[] paths = AssetDatabase.FindAssets("t:LubanProfile", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
        var generated = new List<string>();
        EditorApplication.LockReloadAssemblies();
        try
        {
            foreach (string path in paths)
            {
                await LubanGenerator.GenerateAsync(AssetDatabase.LoadAssetAtPath<LubanProfile>(path));
                generated.Add(path);
            }

            AssetDatabase.SaveAssets();
        }
        finally
        {
            EditorApplication.UnlockReloadAssemblies();
        }

        return new
        {
            passed = true,
            generated
        };
    }

    /// <summary>
    /// 检查必须满足的条件。
    /// </summary>
    /// <param name="condition">是否满足条件。</param>
    /// <param name="message">失败原因。</param>
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// 判断类型是否由编译器或 Unity 源码生成器产生。
    /// </summary>
    /// <param name="type">要检查的类型。</param>
    /// <returns>类型或其外层类型是否带有编译器生成标记。</returns>
    private static bool IsGenerated(Type type)
    {
        return type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)
            || type.IsNested && IsGenerated(type.DeclaringType);
    }

    /// <summary>
    /// 判断类型是否是 Core 模块或 Helper 的通知参数。
    /// </summary>
    /// <param name="type">要检查的类型。</param>
    /// <returns>是否继承框架事件基类，且不属于按编号派发的游戏事件。</returns>
    private static bool IsModuleNotification(Type type)
    {
        return type != typeof(GameFrameworkEventArgs) && typeof(GameFrameworkEventArgs).IsAssignableFrom(type)
            && !typeof(BaseEventArgs).IsAssignableFrom(type);
    }
}
