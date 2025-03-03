using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Spiffy.Monitoring;

namespace ConsulRx.Templating
{
    public interface IRazorTemplateCompiler
    {
        Assembly Compile(IEnumerable<TemplateMetadata> templatePaths, Type baseClass);
    }

    public class RazorTemplateCompiler : IRazorTemplateCompiler
    {
        public Assembly Compile(IEnumerable<TemplateMetadata> templates, Type baseClass)
        {
            var runtimeDir = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);
            var metadataReferences = typeof(ConsulTemplateBase).GetTypeInfo()
                .Assembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Concat(new[] {typeof(ConsulTemplateBase).GetTypeInfo().Assembly})
                .Select(assembly => assembly.Location)
                .Select(location => MetadataReference.CreateFromFile(location))
                .Concat(new[] { "mscorlib.dll", "System.Runtime.dll", "netstandard.dll" }
                    .Select(dll => Path.Combine(runtimeDir, dll))
                    .Where(File.Exists)
                    .Select(path => MetadataReference.CreateFromFile(path)))
                .Concat(new[] { MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location) })
                .ToArray();

            var fileSystem = RazorProjectFileSystem.Create("/");
            var projectEngine = RazorProjectEngine.Create(
                RazorConfiguration.Default,
                fileSystem,
                builder =>
                {
                    builder.SetNamespace("ConsulTemplate.CompiledRazorTemplates");
                    builder.SetBaseType(baseClass.FullName);
                    builder.ConfigureClass((document, classNode) =>
                    {
                        var fileName = Path.GetFileNameWithoutExtension(document.Source.FilePath);
                        var hash = document.Source.FilePath.GetHashCode().ToString();
                        classNode.ClassName = System.Text.RegularExpressions.Regex.Replace(
                            fileName, "[^a-z0-9]", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                            + "_" + hash.Replace('-', '_');
                    });
                    builder.AddDefaultImports(
                        "@using System",
                        "@using System.Linq"
                    );
                });

            var syntaxTrees = templates.Select(metadata =>
                {
                    var sourceDocument = RazorSourceDocument.Create(
                        File.ReadAllText(metadata.FullPath), metadata.FullPath);
                    var codeDocument = RazorCodeDocument.Create(sourceDocument);
                    projectEngine.Engine.Process(codeDocument);
                    var generatedCode = codeDocument.GetCSharpDocument().GeneratedCode;
                    return CSharpSyntaxTree.ParseText(generatedCode);
                })
                .ToArray();

            var assemblyName = $"ConsulRazor.CompiledRazorTemplates.{Guid.NewGuid():N}";
            var compilation = CSharpCompilation.Create(assemblyName, syntaxTrees,
                metadataReferences, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return CompileCompilation(compilation);
        }

        private Assembly CompileCompilation(CSharpCompilation compilation)
        {
            using (var ms = new MemoryStream())
            using (var eventContext = new EventContext("ConsulRx", "CompileRazorTemplates"))
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    eventContext.SetLevel(Level.Error);
                    var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

                    var errorWriter = new StringWriter();
                    foreach (var diagnostic in failures)
                    {
                        errorWriter.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                        errorWriter.WriteLine(diagnostic.Location.SourceTree);
                    }
                    eventContext["Diagnostics"] = errorWriter.ToString();

                    throw new TemplateCompilationException(failures);
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    return AssemblyLoadContext.Default.LoadFromStream(ms);
                }
            }
        }
    }
}
