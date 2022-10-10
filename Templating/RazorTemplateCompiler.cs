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
using Spiffy.Monitoring;

namespace ConsulRx.Templating
{
    public interface IRazorTemplateCompiler
    {
        Assembly Compile(IEnumerable<TemplateMetadata> templatePaths, Type baseClass, IEnumerable<AssemblyName> assemblyReferences);
    }

    public class RazorTemplateCompiler : IRazorTemplateCompiler
    {
        public Assembly Compile(IEnumerable<TemplateMetadata> templates, Type baseClass, IEnumerable<AssemblyName> assemblyReferences)
        {
            using var eventContext = new EventContext("ConsulRx", "CompileRazorTemplate");
            var metadataReferences = typeof(ConsulTemplateBase).GetTypeInfo()
                .Assembly
                .GetReferencedAssemblies()
                .Concat(assemblyReferences)
                .Select(Assembly.Load)
                .Concat(new[] {typeof(ConsulTemplateBase).GetTypeInfo().Assembly, baseClass.Assembly})
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
                DefaultNamespace = "ConsulRx.Templating.CompiledRazorTemplates",
            };

            // Everyone needs the System namespace, right?
            host.NamespaceImports.Add("System");
            host.NamespaceImports.Add("System.Linq");
            host.NamespaceImports.Add(baseClass.Namespace!);
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

            var compilation = CSharpCompilation.Create("ConsulRx.Templating.CompiledRazorTemplates", syntaxTrees,
                metadataReferences, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return CompileCompilation(compilation, eventContext);
        }

        private Assembly CompileCompilation(CSharpCompilation compilation, EventContext eventContext)
        {
            using (var ms = new MemoryStream())
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