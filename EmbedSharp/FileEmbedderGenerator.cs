using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace EmbedSharp
{
    [Generator]
    internal class FileEmbedderGenerator : ISourceGenerator
    {
        private const string AttributeText = @"
using System;
using Microsoft.CodeAnalysis;
namespace EmbedSharp
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class EmbedFileAttribute : Attribute
    {
        public EmbedFileAttribute()
        {
        }
        
        public string Path { get; init; }

        public Accessibility Accessibility { get; init; } = Accessibility.Public;
    }
}
";

        private ISymbol AttributeSymbol { get; set; }

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("EmbedFileAttribute", AttributeText);

            if (!(context.SyntaxReceiver is FileEmbedderSyntaxReceiver receiver))
                return;

            CSharpParseOptions options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(AttributeText, Encoding.UTF8), options));

            this.AttributeSymbol = compilation.GetTypeByMetadataName($"EmbedSharp.EmbedFileAttribute");
            List<IFieldSymbol> fieldSymbols = new List<IFieldSymbol>();

#pragma warning disable RS1024 // Compare symbols correctly
            foreach (var f in receiver.CandidateFields.Select(f =>
            {
                SemanticModel model = compilation.GetSemanticModel(f.SyntaxTree);
                foreach (var v in f.Declaration.Variables)
                {
                    var symbol = model.GetDeclaredSymbol(v) as IFieldSymbol;
                    if (symbol.GetAttributes().Any(a => a.AttributeClass.Equals(this.AttributeSymbol, SymbolEqualityComparer.Default)))
                    {
                        return symbol;
                    }
                }
                return null;
            }).GroupBy(f => f.ContainingType))
            {
                ProcessClass(context, f.Key, f);
            }
#pragma warning restore RS1024
        }

        private void ProcessClass(GeneratorExecutionContext context, INamedTypeSymbol classSymbol, IEnumerable<IFieldSymbol> fields)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
                return;

            string namespaceName = classSymbol.ContainingNamespace.Name;

            StringBuilder source = new StringBuilder($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Name}
    {{
");
            foreach(var f in fields)
            {
                string name = f.Name.TrimStart('_');
                name = name.Substring(0, 1).ToUpper() + name.Substring(1); // Remove beginning '_' and make sure the first character is the upper variant. (_name -> Name)

                AttributeData data = f.GetAttributes().Single(a => a.AttributeClass.Equals(this.AttributeSymbol, SymbolEqualityComparer.Default));
                string path = data.NamedArguments.SingleOrDefault(kvp => kvp.Key == "Path").Value.Value.ToString();
              
                byte[] buf = null;
                try
                {
                    buf = File.ReadAllBytes(path);
                }
                catch (FileNotFoundException _)
                {
                    string fieldSourcePath = f.Locations.FirstOrDefault().SourceTree.FilePath;
                    fieldSourcePath = fieldSourcePath.Substring(0, fieldSourcePath.LastIndexOf('\\') + 1);
                    buf = File.ReadAllBytes(fieldSourcePath + path);
                }
                catch
                {
                    return;
                }

                var typeStr = f.Type.ToString();
                var accessStr = ((Accessibility)data.NamedArguments.SingleOrDefault(kvp => kvp.Key == "Accessibility").Value.Value).ToString();
                accessStr = char.ToLower(accessStr[0]) + accessStr.Substring(1);

                if (typeStr == "string")
                {
                    source.AppendLine($"{accessStr} static string {name} = \"{Encoding.UTF8.GetString(buf)}\";");
                }
                else if (typeStr == "byte[]")
                {
                    StringBuilder str = new StringBuilder($"{accessStr} static byte[] {name} = new byte[] {{");
                    foreach (byte b in buf)
                    {
                        str.Append(b);
                        str.Append(',');
                    }

                    str.Append("};");
                    source.AppendLine(str.ToString());
                }
            }
            source.Append("} }");
            context.AddSource($"{classSymbol.Name}_embeddedfiles.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new FileEmbedderSyntaxReceiver());
        }
    }
}
