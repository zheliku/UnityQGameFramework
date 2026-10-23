using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

// 开发工具：生成演示覆盖清单，不给示例增加运行时依赖。
public static class AuditExampleApi
{
    public static object Run()
    {
        var runtime = typeof(BaseComponent).Assembly.GetExportedTypes()
            .Where(type => type.BaseType == typeof(FrameworkComponent));
        var core = typeof(FrameworkEntry).Assembly.GetExportedTypes()
            .Where(type => type.IsInterface || type == typeof(ReferencePool)
                || type == typeof(FrameworkEntry) || type == typeof(Utility)
                || type == typeof(UnityQGameFramework.Core.Fsm.FsmBase) || type == typeof(ReferencePoolInfo)
                || type == typeof(UnityQGameFramework.Core.ObjectPool.ObjectPoolBase)
                || type == typeof(UnityQGameFramework.Core.ObjectPool.ObjectBase)
                || type == typeof(UnityQGameFramework.Core.ObjectPool.ObjectInfo)
                || type.DeclaringType == typeof(Utility));
        var entries = runtime.Concat(core).OrderBy(type => type.FullName).Select(type => new
        {
            type = type.FullName,
            layer = type.Assembly == typeof(BaseComponent).Assembly ? "Unity" : "Core",
            interfaces = type.GetInterfaces().Select(contract => contract.FullName).OrderBy(name => name).ToArray(),
            methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName).Select(method => new
                {
                    name = method.Name,
                    signature = method.ToString(),
                    returns = method.ReturnType.ToString(),
                    parameters = method.GetParameters().Select(parameter => new
                    {
                        name = parameter.Name,
                        type = parameter.ParameterType.ToString(),
                        optional = parameter.IsOptional
                    }).ToArray(),
                    obsolete = method.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length != 0
                }).OrderBy(method => method.signature).ToArray(),
            properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(property => new { name = property.Name, type = property.PropertyType.ToString(),
                    readable = property.GetGetMethod() != null, writable = property.GetSetMethod() != null })
                .OrderBy(property => property.name).ToArray(),
            events = type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(member => new { name = member.Name, handlerType = member.EventHandlerType.ToString() })
                .OrderBy(member => member.name).ToArray()
        }).ToArray();
        var result = new
        {
            status = "inventory; not a coverage or runtime pass",
            schemaVersion = 2,
            coreModuleId = typeof(FrameworkEntry).Assembly.ManifestModule.ModuleVersionId,
            runtimeModuleId = typeof(BaseComponent).Assembly.ManifestModule.ModuleVersionId,
            types = entries.Length,
            methods = entries.Sum(entry => entry.methods.Length),
            properties = entries.Sum(entry => entry.properties.Length),
            events = entries.Sum(entry => entry.events.Length),
            entries
        };
        Directory.CreateDirectory("Artifacts~/Documentation");
        File.WriteAllText("Artifacts~/Documentation/example-api-inventory.json",
            Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return new { result.status, result.schemaVersion, result.types, result.methods, result.properties, result.events };
    }
}
