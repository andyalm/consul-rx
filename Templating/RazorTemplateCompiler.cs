using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

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
            var metadataReferences = typeof(ConsulTemplateBase).GetTypeInfo()
                .Assembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Concat(new[] {typeof(ConsulTemplateBase).GetTypeInfo().Assembly})
                .Select(assembly => assembly.Location)
                .Select(location => MetadataReference.CreateFromFile(location))
                .Concat(new[]
                {
                    MetadataReference.CreateFromFile(Path.Combine(
                        Path.GetDirectoryName(typeof(Enumerable).GetTypeInfo().Assembly.Location), "mscorlib.dll")),
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location)
                })
                .ToArray();

            var language = new CSharpRazorCodeLanguage();
            var host = new RazorEngineHost(language)
            {
                DefaultBaseClass = baseClass.FullName,
                DefaultNamespace = "ConsulRazor.CompiledRazorTemplates",
            };

            // Everyone needs the System namespace, right?
            host.NamespaceImports.Add("System");
            host.NamespaceImports.Add("System.Linq");
            var engine = new RazorTemplateEngine(host);
            var syntaxTrees = templates.Select(metadata =>
                {
                    using (var templateStream = File.OpenRead(metadata.FullPath))
                    {
                        var generatorResults = engine.GenerateCode(new StreamReader(templateStream), metadata.ClassName,
                            metadata.Namespace, metadata.Filename);

                        return CSharpSyntaxTree.ParseText(generatorResults.GeneratedCode);
                    }
                })
                .ToArray();

            var compilation = CSharpCompilation.Create("ConsulRazor.CompiledRazorTemplates", syntaxTrees,
                metadataReferences, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return CompileCompilation(compilation);
        }

        private Assembly CompileCompilation(CSharpCompilation compilation)
        {
            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error).ToArray();

                    foreach (var diagnostic in failures)
                    {
                        Console.Error.WriteLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                        Console.Error.WriteLine(diagnostic.Location.SourceTree);
                    }

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