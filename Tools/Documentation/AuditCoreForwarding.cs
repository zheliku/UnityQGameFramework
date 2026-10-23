using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

// 只读取当前程序集的调用指令和接口实现，不执行被检查的方法。
public static class AuditCoreForwarding
{
    private static readonly BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private static readonly Dictionary<int, OpCode> Codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode)).Select(field => (OpCode)field.GetValue(null))
        .ToDictionary(code => (int)(ushort)code.Value);

    public static object Run()
    {
        Assembly core = typeof(FrameworkEntry).Assembly, runtime = typeof(BaseComponent).Assembly;
        var calls = new List<object>();
        var localCalls = new List<object>();
        var implementations = new List<object>();
        var implementationTypes = new List<object>();
        var managerCalls = new List<object>();
        var managerLocalCalls = new List<object>();
        var managerImplementations = new List<object>();
        var managerImplementationCalls = new List<object>();
        var errors = new List<string>();
        var components = runtime.GetExportedTypes().Where(type => type.BaseType == typeof(FrameworkComponent)).ToArray();
        foreach (Type component in components)
        foreach (MethodInfo method in component.GetMethods(Declared))
        {
            Scan(component, method, method, core, calls, localCalls, errors);
            var state = method.GetCustomAttribute<IteratorStateMachineAttribute>()?.StateMachineType
                ?? method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
            if (state != null)
            foreach (MethodInfo generated in state.GetMethods(Declared))
                Scan(component, method, generated, core, calls, localCalls, errors);
        }

        foreach (Type implementation in runtime.GetExportedTypes().Where(type => type.IsClass && !type.IsAbstract))
        foreach (Type contract in implementation.GetInterfaces().Where(type => type.Assembly == core))
        {
            var map = implementation.GetInterfaceMap(contract);
            Type definition = contract.IsGenericType ? contract.GetGenericTypeDefinition() : contract;
            implementationTypes.Add(new { contract = definition.FullName, implementation = implementation.FullName,
                members = map.InterfaceMethods.Length });
            for (int index = 0; index < map.InterfaceMethods.Length; index++)
            {
                MethodInfo member = Definition(map.InterfaceMethods[index]);
                implementations.Add(new
                {
                    contract = member.DeclaringType.FullName, member = member.ToString(), memberName = member.Name,
                    implementation = implementation.FullName,
                    declaringType = map.TargetMethods[index].DeclaringType.FullName,
                    target = map.TargetMethods[index].ToString(), member.IsSpecialName
                });
            }
        }
        Type moduleBase = core.GetType("UnityQGameFramework.Core.FrameworkModule", true);
        foreach (Type manager in core.GetTypes().Where(type => type.IsClass && !type.IsAbstract
            && moduleBase.IsAssignableFrom(type)))
        {
            foreach (MethodInfo method in manager.GetMethods(Declared))
                Scan(manager, method, method, core, managerCalls, managerLocalCalls, errors, managerImplementationCalls);
            foreach (Type contract in manager.GetInterfaces().Where(type => type.Assembly == core))
            {
                var map = manager.GetInterfaceMap(contract);
                for (int index = 0; index < map.InterfaceMethods.Length; index++)
                {
                    MethodInfo member = Definition(map.InterfaceMethods[index]);
                    managerImplementations.Add(new
                    {
                        contract = member.DeclaringType.FullName, member = member.ToString(), memberName = member.Name,
                        implementation = manager.FullName, target = map.TargetMethods[index].ToString(), member.IsSpecialName
                    });
                }
            }
        }
        if (errors.Count != 0) throw new Exception(string.Join("\n", errors));
        var report = new
        {
            scope = "Compiled component/manager call sites, shared Core implementation endpoints and Unity interface implementations; not runtime behavior coverage.",
            coreModuleId = core.ManifestModule.ModuleVersionId, runtimeModuleId = runtime.ManifestModule.ModuleVersionId,
            components = components.Select(type => type.FullName).OrderBy(name => name).ToArray(),
            calls, localCalls, implementations, implementationTypes, managerCalls, managerLocalCalls, managerImplementations, managerImplementationCalls
        };
        Directory.CreateDirectory("Artifacts~/CoreForwardingExamples");
        File.WriteAllText("Artifacts~/CoreForwardingExamples/compiled-forwarding.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return new { components = components.Length, coreCallSites = calls.Count, componentCallSites = localCalls.Count,
            interfaceImplementations = implementations.Count, managerCallSites = managerCalls.Count,
            managerLocalCallSites = managerLocalCalls.Count, managerInterfaceImplementations = managerImplementations.Count,
            managerImplementationCallSites = managerImplementationCalls.Count, errors };
    }

    private static MethodInfo Definition(MethodInfo method)
    {
        Type owner = method.DeclaringType;
        if (owner.IsGenericType) owner = owner.GetGenericTypeDefinition();
        return owner.GetMethods(Declared).Single(candidate => candidate.MetadataToken == method.MetadataToken);
    }

    private static void Scan(Type component, MethodInfo entry, MethodInfo caller, Assembly core,
        List<object> calls, List<object> localCalls, List<string> errors, List<object> implementationCalls = null)
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
                        caller.DeclaringType.GetGenericArguments(), caller.GetGenericArguments()) as MethodInfo;
                    if (target != null && ((target.DeclaringType.Assembly == core && target.DeclaringType.IsInterface)
                        || target.DeclaringType == component
                        || (implementationCalls != null && target.DeclaringType.Assembly == core)))
                    {
                        MethodInfo member = Definition(target);
                        var record = new
                        {
                            component = component.FullName, entry = entry.ToString(), entryName = entry.Name, entry.IsPublic,
                            caller = caller.DeclaringType.FullName + "." + caller.Name,
                            targetType = member.DeclaringType.FullName, target = member.ToString(), targetName = member.Name,
                            member.IsSpecialName, instruction = code.Name, offset = start
                        };
                        if (target.DeclaringType.Assembly == core && target.DeclaringType.IsInterface) calls.Add(record);
                        else if (target.DeclaringType == component) localCalls.Add(record);
                        if (!target.DeclaringType.IsInterface && target.DeclaringType.Assembly == core)
                            implementationCalls?.Add(record);
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
