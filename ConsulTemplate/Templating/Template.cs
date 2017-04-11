using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using ConsulTemplate.Reactive;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace ConsulTemplate.Templating
{
    public class Template
    {
        private readonly Assembly _templateAssembly;

        public Template(string templatePath)
        {
            var language = new CSharpRazorCodeLanguage();
            var host = new RazorEngineHost(language)
            {
                DefaultBaseClass = "ConsulTemplate.Templating.ConsulTemplateBase",
                DefaultClassName = "ConsulTemplateImpl",
                DefaultNamespace = "ConsulTemplate.CompiledRazorTemplates",
            };

            // Everyone needs the System namespace, right?
            host.NamespaceImports.Add("System");
            host.NamespaceImports.Add("System.Linq");

            var engine = new RazorTemplateEngine(host);
            GeneratorResults generatorResults;

            templatePath = Path.GetFullPath(templatePath);
            using (var templateStream = File.OpenRead(templatePath))
            {
                generatorResults = engine.GenerateCode(new StreamReader(templateStream));
            }
            var syntaxTree = CSharpSyntaxTree.ParseText(generatorResults.GeneratedCode);
            var metadataReferences = typeof(ConsulTemplateBase).GetTypeInfo().Assembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Concat(new[] { typeof(ConsulTemplateBase).GetTypeInfo().Assembly })
                .Select(assembly => assembly.Location)
                .Select(location => MetadataReference.CreateFromFile(location))
                .Concat(new []
                {
                    MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(Enumerable).GetTypeInfo().Assembly.Location), "mscorlib.dll")),
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location)
                })
                .ToArray();

            var compilation = CSharpCompilation.Create("ConsulTemplate.CompiledRazorTemplates", new[] {syntaxTree},
                metadataReferences, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            _templateAssembly = Compile(compilation);
        }

        public TemplateAnalysis Analyse()
        {
            var instance = CreateTemplateInstance();
            return instance.Analyse();
        }


        public void Render(TextWriter writer, TemplateModel model)
        {
            var instance = CreateTemplateInstance();
            instance.Render(writer, model);
        }

        private ConsulTemplateBase CreateTemplateInstance()
        {
            var templateType = _templateAssembly.GetType("ConsulTemplate.CompiledRazorTemplates.ConsulTemplateImpl",
                throwOnError: true);
            return (ConsulTemplateBase) Activator.CreateInstance(templateType);
        }

        private Assembly Compile(CSharpCompilation compilation)
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
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
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

    public class TemplateCompilationException : Exception
    {
        private Diagnostic[] _errors;

        public TemplateCompilationException(Diagnostic[] errors) : base("An exception ocurred compiling the template")
        {
            _errors = errors;
        }
    }
}