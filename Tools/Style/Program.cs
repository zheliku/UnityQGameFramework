using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

string root = Path.GetFullPath(args.FirstOrDefault() ?? ".");
string package = Path.GetFullPath(Path.Combine(root, "Packages/com.zheliku.unityqgameframework"));
string[] directories = { Path.Combine(package, "Runtime"), Path.Combine(package, "Editor"), Path.Combine(root, "Assets") };
string[] candidates = directories.SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
    .Select(Path.GetFullPath).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
// Luban 运行库与 Plugins 均来自第三方，生成代码由生成器负责。
string[] thirdParty = candidates.Where(path => path.Replace('\\', '/').Contains("/Plugins/")
    || path.StartsWith(Path.GetFullPath(Path.Combine(package, "Runtime/Luban")) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)).ToArray();
string[] generated = candidates.Except(thirdParty).Where(path => path.Replace('\\', '/').Contains("/Generated/") || IsGenerated(File.ReadAllText(path))).ToArray();
string[] sources = candidates.Except(thirdParty).Except(generated).ToArray();
CSharpParseOptions[] options =
{
    new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose),
    new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose, preprocessorSymbols: new[] { "UNITY_EDITOR", "ENABLE_INPUT_SYSTEM", "ENABLE_LOG" }),
    new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose, preprocessorSymbols: new[] { "UNITY_EDITOR", "UNITY_6000_3_OR_NEWER", "ENABLE_INPUT_SYSTEM", "ENABLE_LOG" })
};
// 分部类型的说明只需定义一次，不在每个实现文件重复。
var documentedTypes = sources.SelectMany(path => options.SelectMany(option => CSharpSyntaxTree.ParseText(File.ReadAllText(path), option).GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>()))
    .Where(type => type.GetLeadingTrivia().ToFullString().Contains("<summary>", StringComparison.Ordinal))
    .Select(TypeKey).ToHashSet(StringComparer.Ordinal);
var issues = new List<StyleIssue>();
var languageIssues = new List<LanguageIssue>();
int files = 0;
foreach (string path in sources)
{
    string source = File.ReadAllText(path);
    files++;
    if (options.All(option => !CSharpSyntaxTree.ParseText(source, option).GetRoot().DescendantTokens()
        .Any(token => !token.IsKind(SyntaxKind.EndOfFileToken))))
    {
        issues.Add(new StyleIssue(Path.GetRelativePath(root, path).Replace('\\', '/'), 1, string.Empty, "empty_source_file"));
    }

    foreach (CSharpParseOptions option in options)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, option);
        string relative = Path.GetRelativePath(root, path).Replace('\\', '/');
        foreach (Diagnostic diagnostic in tree.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            issues.Add(new StyleIssue(relative, diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1, diagnostic.Id, "syntax_error"));
        }
        foreach (MemberDeclarationSyntax member in tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>())
        {
            bool type = member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax;
            bool explicitInterface = member is MethodDeclarationSyntax { ExplicitInterfaceSpecifier: not null }
                or PropertyDeclarationSyntax { ExplicitInterfaceSpecifier: not null }
                or IndexerDeclarationSyntax { ExplicitInterfaceSpecifier: not null }
                or EventDeclarationSyntax { ExplicitInterfaceSpecifier: not null };
            bool api = member.Modifiers.Any(SyntaxKind.PublicKeyword) || member.Modifiers.Any(SyntaxKind.ProtectedKeyword)
                || path.StartsWith(package, StringComparison.OrdinalIgnoreCase) && member.Modifiers.Any(SyntaxKind.InternalKeyword)
                || explicitInterface || member.Parent is InterfaceDeclarationSyntax || member is EnumMemberDeclarationSyntax;
            // public/protected 成员不因所在类型是 internal 或 private 而跳过。
            // 框架内部的方法同样检查文档；示例的简单私有回调保留用途明确的行内说明。
            bool frameworkMethod = path.StartsWith(package, StringComparison.OrdinalIgnoreCase) && member is BaseMethodDeclarationSyntax;
            if (!type && !api && !frameworkMethod)
            {
                continue;
            }
            string name = member switch
            {
                BaseTypeDeclarationSyntax item => item.Identifier.ValueText,
                BaseMethodDeclarationSyntax item => item switch { MethodDeclarationSyntax method => method.Identifier.ValueText, ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText, _ => item.Kind().ToString() },
                PropertyDeclarationSyntax item => item.Identifier.ValueText,
                DelegateDeclarationSyntax item => item.Identifier.ValueText,
                FieldDeclarationSyntax item => item.Declaration.Variables.First().Identifier.ValueText,
                EnumMemberDeclarationSyntax item => item.Identifier.ValueText,
                EventFieldDeclarationSyntax item => item.Declaration.Variables.First().Identifier.ValueText,
                _ => member.Kind().ToString()
            };
            string document = string.Join("\n", member.GetLeadingTrivia().Where(trivia => trivia.HasStructure && trivia.GetStructure() is DocumentationCommentTriviaSyntax));
            int line = tree.GetLineSpan(member.Span).StartLinePosition.Line + 1;
            void Issue(string rule)
            {
                issues.Add(new StyleIssue(relative, line, name, rule));
            }
            SyntaxTrivia documentTrivia = member.GetLeadingTrivia().FirstOrDefault(trivia => trivia.GetStructure() is DocumentationCommentTriviaSyntax);
            if (documentTrivia.HasStructure)
            {
                var lines = tree.GetText().Lines;
                int documentLine = lines.GetLineFromPosition(documentTrivia.SpanStart).LineNumber;
                if (documentLine > 0)
                {
                    string previousLine = lines[documentLine - 1].ToString().Trim();
                    if (!previousLine.StartsWith("//", StringComparison.Ordinal) && previousLine.Length > 0 && ";},".Contains(previousLine[^1]))
                    {
                        Issue("documentation_spacing");
                    }
                }
            }
            bool documentedPartial = member is BaseTypeDeclarationSyntax declaration && declaration.Modifiers.Any(SyntaxKind.PartialKeyword) && documentedTypes.Contains(TypeKey(declaration));
            if (!document.Contains("<summary>", StringComparison.Ordinal) && !documentedPartial)
            {
                Issue("summary_missing");
            }
            else if (Regex.IsMatch(document, @"<summary>[^\r\n]+</summary>"))
            {
                Issue("summary_single_line");
            }
            if (document.Contains("<summary>", StringComparison.Ordinal) && !Regex.IsMatch(Regex.Match(document, @"<summary>([\s\S]*?)</summary>").Value, "[\u4e00-\u9fff]"))
            {
                Issue("summary_not_chinese");
            }
            TypeParameterListSyntax? generics = member switch
            {
                TypeDeclarationSyntax item => item.TypeParameterList,
                DelegateDeclarationSyntax item => item.TypeParameterList,
                _ => null
            };

            // 检查标签是否与当前签名对应，避免同名重载之间混入错误参数。
            XElement xml;
            try
            {
                xml = XElement.Parse("<doc>" + Regex.Replace(document, @"(?m)^\s*/// ?", string.Empty) + "</doc>");
            }
            catch (System.Xml.XmlException)
            {
                Issue("xml_invalid");
                continue;
            }
            TypeParameterListSyntax? allGenerics = member is MethodDeclarationSyntax genericMethod ? genericMethod.TypeParameterList : generics;
            IEnumerable<ParameterSyntax> allParameters = member switch
            {
                BaseMethodDeclarationSyntax item => item.ParameterList.Parameters,
                DelegateDeclarationSyntax item => item.ParameterList.Parameters,
                IndexerDeclarationSyntax item => item.ParameterList.Parameters,
                _ => Array.Empty<ParameterSyntax>()
            };
            CheckNamedTags(xml, "param", allParameters.Select(item => item.Identifier.ValueText).ToArray(), Issue);
            if (!(documentedPartial && !document.Contains("<summary>", StringComparison.Ordinal)))
            {
                CheckNamedTags(xml, "typeparam", (allGenerics?.Parameters ?? default).Select(item => item.Identifier.ValueText).ToArray(), Issue);
            }
            TypeSyntax? returnType = member switch
            {
                MethodDeclarationSyntax item => item.ReturnType,
                DelegateDeclarationSyntax item => item.ReturnType,
                OperatorDeclarationSyntax item => item.ReturnType,
                ConversionOperatorDeclarationSyntax item => item.Type,
                IndexerDeclarationSyntax item => item.Type,
                _ => null
            };
            bool needsReturn = returnType != null && returnType.ToString() != "void";
            if (needsReturn && xml.Element("returns") == null)
            {
                Issue("returns_missing");
            }
            if (!needsReturn && xml.Element("returns") != null)
            {
                Issue("returns_unexpected");
            }
            int lastRank = -1;
            foreach (XElement tag in xml.Elements())
            {
                int rank = tag.Name.LocalName switch { "summary" => 0, "typeparam" => 1, "param" => 2, "returns" => 3, _ => 4 };
                if (rank < lastRank)
                {
                    Issue("tag_order");
                }
                lastRank = rank;
                if (string.IsNullOrWhiteSpace(tag.Value) && tag.Name.LocalName is "summary" or "typeparam" or "param" or "returns")
                {
                    Issue("tag_empty:" + tag.Name.LocalName);
                }
                else if ((tag.Name.LocalName is "typeparam" or "param" or "returns") && !Regex.IsMatch(tag.Value, "[\u4e00-\u9fff]"))
                {
                    Issue("tag_not_chinese:" + tag.Name.LocalName);
                }
            }
            foreach (var duplicate in xml.Elements().GroupBy(tag => tag.Name.LocalName + ":" + (string?)tag.Attribute("name")).Where(group => group.Count() > 1))
            {
                Issue("tag_duplicate:" + duplicate.Key);
            }
        }
        foreach (ArrowExpressionClauseSyntax arrow in tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>())
        {
            issues.Add(new StyleIssue(Path.GetRelativePath(root, path).Replace('\\', '/'), tree.GetLineSpan(arrow.Span).StartLinePosition.Line + 1, arrow.Parent?.Kind().ToString() ?? string.Empty, "expression_body"));
        }
        foreach (SyntaxNode node in tree.GetRoot().DescendantNodes())
        {
            StatementSyntax? embedded = node switch
            {
                IfStatementSyntax item => item.Statement,
                ElseClauseSyntax item when item.Statement is not IfStatementSyntax => item.Statement,
                ForStatementSyntax item => item.Statement,
                ForEachStatementSyntax item => item.Statement,
                WhileStatementSyntax item => item.Statement,
                DoStatementSyntax item => item.Statement,
                UsingStatementSyntax item => item.Statement,
                LockStatementSyntax item => item.Statement,
                _ => null
            };
            if (embedded != null && embedded is not BlockSyntax)
            {
                issues.Add(new StyleIssue(Path.GetRelativePath(root, path).Replace('\\', '/'), tree.GetLineSpan(embedded.Span).StartLinePosition.Line + 1, node.Kind().ToString(), "statement_block_missing"));
            }
        }
        foreach (SyntaxToken token in tree.GetRoot().DescendantTokens())
        {
            if ((token.IsKind(SyntaxKind.StringLiteralToken) || token.IsKind(SyntaxKind.InterpolatedStringTextToken)
                || token.IsKind(SyntaxKind.IdentifierToken) || token.IsKind(SyntaxKind.Utf8StringLiteralToken))
                && Regex.IsMatch(token.ValueText, "[\u3400-\u9fff\uac00-\ud7af\u3040-\u30ff]"))
            {
                languageIssues.Add(new LanguageIssue(Path.GetRelativePath(root, path).Replace('\\', '/'),
                    tree.GetLineSpan(token.Span).StartLinePosition.Line + 1, token.SpanStart, token.Span.Length, token.Text));
            }

            if ((!token.IsKind(SyntaxKind.OpenBraceToken) && !token.IsKind(SyntaxKind.CloseBraceToken))
                || token.Parent is not (BlockSyntax or BaseTypeDeclarationSyntax or BaseNamespaceDeclarationSyntax or SwitchStatementSyntax or AccessorListSyntax)
                || token.Parent is AccessorListSyntax accessorList && accessorList.Accessors.All(accessor => accessor.Body == null && accessor.ExpressionBody == null))
            {
                continue;
            }
            int lineStart = source.LastIndexOf('\n', token.SpanStart) + 1;
            if (!string.IsNullOrWhiteSpace(source[lineStart..token.SpanStart]))
            {
                issues.Add(new StyleIssue(Path.GetRelativePath(root, path).Replace('\\', '/'), tree.GetLineSpan(token.Span).StartLinePosition.Line + 1, token.Parent.Kind().ToString(), "brace_own_line"));
            }
        }
    }
}
issues = issues.Distinct().ToList();
languageIssues = languageIssues.Distinct().ToList();
issues.AddRange(languageIssues.Select(issue => new StyleIssue(issue.Path, issue.Line, issue.Text, "code_not_english")));
string report = Path.Combine(root, "Artifacts~/CodeStyle/audit.json");
Directory.CreateDirectory(Path.GetDirectoryName(report)!);
File.WriteAllText(Path.Combine(Path.GetDirectoryName(report)!, "language.json"), JsonSerializer.Serialize(languageIssues, new JsonSerializerOptions { WriteIndented = true }));
var coverage = directories.ToDictionary(directory => Path.GetRelativePath(root, directory).Replace('\\', '/'),
    directory => sources.Count(path => path.StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)));
var excluded = new { thirdParty = thirdParty.Length, generated = generated.Length };
File.WriteAllText(report, JsonSerializer.Serialize(new { files, coverage, excluded, issues = issues.Count, rules = issues.GroupBy(issue => issue.Rule.Split(':')[0]).ToDictionary(group => group.Key, group => group.Count()), details = issues }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(JsonSerializer.Serialize(new { files, coverage, excluded, issues = issues.Count, report, rules = issues.GroupBy(issue => issue.Rule.Split(':')[0]).ToDictionary(group => group.Key, group => group.Count()), topFiles = issues.GroupBy(issue => issue.Path).OrderByDescending(group => group.Count()).Take(25).Select(group => new { path = group.Key, count = group.Count() }) }, new JsonSerializerOptions { WriteIndented = true }));
Environment.ExitCode = issues.Count == 0 ? 0 : 1;

/// <summary>
/// 检查具名文档标签与声明参数是否一致。
/// </summary>
/// <param name="document">成员文档。</param>
/// <param name="tag">要检查的标签名称。</param>
/// <param name="expected">按声明顺序排列的参数名称。</param>
/// <param name="issue">记录检查问题的回调。</param>
static void CheckNamedTags(XElement document, string tag, string[] expected, Action<string> issue)
{
    string[] actual = document.Elements(tag).Select(element => (string?)element.Attribute("name") ?? string.Empty).ToArray();
    foreach (string name in expected.Except(actual))
    {
        issue(tag + "_missing:" + name);
    }
    foreach (string name in actual.Except(expected))
    {
        issue(tag + "_unexpected:" + name);
    }
    if (expected.Length == actual.Length && !expected.Except(actual).Any() && !expected.SequenceEqual(actual))
    {
        issue(tag + "_order");
    }
}

/// <summary>
/// 获取用于合并分部类型文档的标识。
/// </summary>
/// <param name="type">类型声明。</param>
/// <returns>包含命名空间、外层类型和泛型参数数量的类型标识。</returns>
static string TypeKey(BaseTypeDeclarationSyntax type)
{
    return string.Join(".", type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(item => item.Name.ToString())
        .Concat(type.Ancestors().OfType<BaseTypeDeclarationSyntax>().Reverse().Select(TypeName)).Append(TypeName(type)));
}

/// <summary>
/// 获取包含泛型参数数量的类型名称。
/// </summary>
/// <param name="type">类型声明。</param>
/// <returns>用于区分同名泛型类型的名称。</returns>
static string TypeName(BaseTypeDeclarationSyntax type)
{
    return type.Identifier.ValueText + (type is TypeDeclarationSyntax declaration ? "`" + (declaration.TypeParameterList?.Parameters.Count ?? 0) : string.Empty);
}

/// <summary>
/// 检查文件头是否标记为生成代码。
/// </summary>
/// <param name="source">源文件内容。</param>
/// <returns>文件头是否包含生成代码标记。</returns>
static bool IsGenerated(string source)
{
    // 只识别文件头注释，生成器中的模板字符串不能使整个生成器被跳过。
    return CSharpSyntaxTree.ParseText(source).GetRoot().GetLeadingTrivia()
        .Any(trivia => (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            && trivia.ToString().Contains("<auto-generated>", StringComparison.Ordinal));
}

/// <summary>
/// 代码规范检查项。
/// </summary>
/// <param name="Path">相对项目的源文件路径。</param>
/// <param name="Line">问题所在行号。</param>
/// <param name="Member">成员名称或语法节点类型。</param>
/// <param name="Rule">未满足的规则。</param>
internal sealed record StyleIssue(string Path, int Line, string Member, string Rule);

/// <summary>
/// 自有代码中需要翻译的标识符或字符串。
/// </summary>
/// <param name="Path">相对项目的源文件路径。</param>
/// <param name="Line">字符串所在行号。</param>
/// <param name="Start">字符串在源文件中的偏移。</param>
/// <param name="Length">字符串的源文本长度。</param>
/// <param name="Text">包含引号及转义字符的源文本。</param>
internal sealed record LanguageIssue(string Path, int Line, int Start, int Length, string Text);
