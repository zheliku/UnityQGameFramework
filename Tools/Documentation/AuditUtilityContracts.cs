using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Newtonsoft.Json;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

// 只读核对 Utility 的编译调用者与辅助器实现，不执行被检查的方法。
public static class AuditUtilityContracts
{
    private static readonly BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private static readonly Dictionary<int, OpCode> Codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null))
        .ToDictionary(code => (int)(ushort)code.Value);

    public static object Run()
    {
        Assembly core = typeof(FrameworkEntry).Assembly, runtime = typeof(BaseComponent).Assembly;
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly == core || assembly == runtime
            || assembly.GetName().Name.StartsWith("UnityQGameFramework.", StringComparison.Ordinal) || assembly.GetName().Name == "Assembly-CSharp").ToArray();
        var calls = new List<object>();
        var unused = new List<object>();
        var implementations = new List<object>();
        var implementationTypes = new List<object>();
        var errors = new List<string>();
        foreach (var assembly in assemblies)
        foreach (Type type in assembly.GetTypes())
        {
            foreach (MethodBase method in type.GetMethods(Declared).Cast<MethodBase>().Concat(type.GetConstructors(Declared)))
                Scan(type, method, method, core, calls, unused, errors);
            if (!type.IsClass || type.ContainsGenericParameters) continue;
            foreach (Type contract in type.GetInterfaces().Where(candidate => candidate.Assembly == core && candidate.FullName?.StartsWith("UnityQGameFramework.Core.Utility+", StringComparison.Ordinal) == true))
            {
                var map = type.GetInterfaceMap(contract);
                Type definition = contract.IsGenericType ? contract.GetGenericTypeDefinition() : contract;
                implementationTypes.Add(new { contract = definition.FullName, implementation = type.FullName, type.IsAbstract, members = map.InterfaceMethods.Length });
                for (int index = 0; index < map.InterfaceMethods.Length; index++)
                {
                    MethodInfo member = Definition(map.InterfaceMethods[index]);
                    implementations.Add(new { contract = member.DeclaringType.FullName, member = member.ToString(), memberName = member.Name,
                        implementation = type.FullName, declaringType = map.TargetMethods[index].DeclaringType.FullName,
                        target = map.TargetMethods[index].ToString(), member.IsSpecialName, type.IsAbstract });
                }
            }
        }
        if (errors.Count != 0) throw new Exception(string.Join("\n", errors));
        var report = new { scope = "Compiled Utility callers and helper implementations; not behavior coverage.",
            coreModuleId = core.ManifestModule.ModuleVersionId, runtimeModuleId = runtime.ManifestModule.ModuleVersionId,
            assemblies = assemblies.Select(assembly => new { name = assembly.GetName().Name, id = assembly.ManifestModule.ModuleVersionId }),
            calls, implementations, implementationTypes };
        Directory.CreateDirectory("Artifacts~/UtilityContracts");
        File.WriteAllText("Artifacts~/UtilityContracts/utility-contracts.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return new { assemblies = assemblies.Length, utilityCallSites = calls.Count, helperImplementations = implementations.Count, errors };
    }

    private static MethodInfo Definition(MethodInfo method)
    {
        Type owner = method.DeclaringType;
        if (owner.IsGenericType) owner = owner.GetGenericTypeDefinition();
        return owner.GetMethods(Declared).Single(candidate => candidate.MetadataToken == method.MetadataToken);
    }

    private static void Scan(Type component, MethodBase entry, MethodBase caller, Assembly core,
        List<object> calls, List<object> localCalls, List<string> errors)
    {
        byte[] il = caller.GetMethodBody()?.GetILAsByteArray();
        if (il == null) return;
        try
        {
            for (int offset = 0; offset < il.Length;)
            {
                int start = offset, value = il[offset++];
                if (value == 0xfe) value = 0xfe00 | il[offset++];
                OpCode code = Codes[value];
                if (code.OperandType == OperandType.InlineMethod)
                {
                    var target = caller.Module.ResolveMethod(BitConverter.ToInt32(il, offset),
                        caller.DeclaringType.GetGenericArguments(), caller.IsGenericMethod ? caller.GetGenericArguments() : Type.EmptyTypes) as MethodInfo;
                    if (target != null && (target.DeclaringType.Assembly == core && target.DeclaringType.FullName?.StartsWith("UnityQGameFramework.Core.Utility+", StringComparison.Ordinal) == true))
                    {
                        MethodInfo member = Definition(target);
                        var record = new
                        {
                            component = component.FullName, entry = entry.ToString(), entryName = entry.Name, entry.IsPublic,
                            caller = caller.DeclaringType.FullName + "." + caller.Name,
                            targetType = member.DeclaringType.FullName, target = member.ToString(), targetName = member.Name,
                            targetIsPublic = member.IsPublic,
                            member.IsSpecialName, instruction = code.Name, offset = start
                        };
                        calls.Add(record);
                    }
                }
                switch (code.OperandType)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar: offset++; break;
                    case OperandType.InlineVar: offset += 2; break;
                    case OperandType.InlineI:
                    case OperandType.InlineBrTarget:
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                    case OperandType.InlineSig:
                    case OperandType.InlineString:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                    case OperandType.ShortInlineR: offset += 4; break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR: offset += 8; break;
                    case OperandType.InlineSwitch: offset += 4 + BitConverter.ToInt32(il, offset) * 4; break;
                    default: throw new Exception("Unsupported operand " + code.OperandType);
                }
            }
        }
        catch (Exception exception) { errors.Add(component.FullName + "." + entry.Name + ": " + exception.Message); }
    }
}
