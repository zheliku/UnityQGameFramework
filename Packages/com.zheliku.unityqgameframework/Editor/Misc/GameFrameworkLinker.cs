using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.UnityLinker;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 按框架扩展契约生成 Player 的动态类型保留清单。
    /// </summary>
    internal sealed class GameFrameworkLinker : IUnityLinkerProcessor
    {
        /// <summary>
        /// 获取回调执行顺序。
        /// </summary>
        public int callbackOrder
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// 保留 Helper、流程和引用池的具体扩展类型，不保留整个框架程序集。
        /// </summary>
        /// <param name="report">当前构建报告。</param>
        /// <param name="data">UnityLinker 构建数据。</param>
        /// <returns>本次构建在 Library 中生成的清单路径。</returns>
        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            UnityEditor.Compilation.Assembly[] assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            var playerAssemblies = new HashSet<string>(assemblies.Select(assembly => assembly.name)
                .Concat(assemblies.SelectMany(assembly => assembly.compiledAssemblyReferences).Select(Path.GetFileNameWithoutExtension)), StringComparer.Ordinal);
            var contracts = new[]
            {
                typeof(Utility.Text.ITextHelper), typeof(Core.Version.IVersionHelper), typeof(GameFrameworkLog.ILogHelper),
                typeof(Utility.Compression.ICompressionHelper), typeof(Utility.Json.IJsonHelper),
                typeof(ConfigHelperBase), typeof(LocalizationHelperBase), typeof(SettingHelperBase),
                typeof(FileSystemHelperBase), typeof(EntityHelperBase), typeof(EntityGroupHelperBase),
                typeof(UIFormHelperBase), typeof(UIGroupHelperBase), typeof(SoundHelperBase), typeof(SoundGroupHelperBase),
                typeof(SoundAgentHelperBase), typeof(DownloadAgentHelperBase), typeof(WebRequestAgentHelperBase),
                typeof(ProcedureBase), typeof(IResourcePackageInitializer), typeof(IReference),
                // YooAsset 3 的文件系统契约为 internal；仅此构建边界按官方程序集解析。
                typeof(YooAsset.FileSystemParameters).Assembly.GetType("YooAsset.IFileSystem", true)
            };
            var types = new HashSet<System.Type>();
            foreach (System.Type contract in contracts)
            {
                foreach (System.Type type in TypeCache.GetTypesDerivedFrom(contract))
                {
                    if (!type.IsAbstract && !type.ContainsGenericParameters && playerAssemblies.Contains(type.Assembly.GetName().Name))
                    {
                        types.Add(type);
                    }
                }
            }
            var linker = new XElement("linker");
            foreach (var assembly in types.GroupBy(type => type.Assembly.GetName().Name).OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                var entry = new XElement("assembly", new XAttribute("fullname", assembly.Key));
                foreach (System.Type type in assembly.OrderBy(type => type.FullName, StringComparer.Ordinal))
                {
                    // 扩展的序列化字段和虚方法也是契约的一部分；只保留这些具体类型。
                    bool referenceOnly = typeof(IReference).IsAssignableFrom(type);
                    var preserved = new XElement("type", new XAttribute("fullname", type.FullName.Replace('+', '/')),
                        new XAttribute("preserve", referenceOnly ? "nothing" : "all"));
                    if (referenceOnly)
                    {
                        preserved.Add(new XElement("method", new XAttribute("signature", "System.Void .ctor()")));
                    }
                    entry.Add(preserved);
                }
                linker.Add(entry);
            }
            string path = Path.GetFullPath("Library/QGF/link.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            new XDocument(linker).Save(path);
            return path;
        }
    }
}
