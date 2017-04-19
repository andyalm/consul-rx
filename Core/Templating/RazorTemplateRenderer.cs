using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using ConsulTemplate.Reactive;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace ConsulTemplate.Templating
{
    public class RazorTemplateRenderer : ITemplateRenderer
    {
        private readonly Assembly _templateAssembly;
        private readonly IDictionary<string, TemplateMetadata> _templateMetadata;

        public RazorTemplateRenderer(IEnumerable<string> templatePaths)
        {
            _templateMetadata = templatePaths.Select(t => new TemplateMetadata(Path.GetFullPath(t))).ToDictionary(m => m.FullPath);
            
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

            var language = new CSharpRazorCodeLanguage();
            var host = new RazorEngineHost(language)
            {
                DefaultBaseClass = "ConsulTemplate.Templating.ConsulTemplateBase",
                DefaultNamespace = "ConsulTemplate.CompiledRazorTemplates",
            };

            // Everyone needs the System namespace, right?
            host.NamespaceImports.Add("System");
            host.NamespaceImports.Add("System.Linq");
            var engine = new RazorTemplateEngine(host);
            var syntaxTrees = _templateMetadata.Values.Select(metadata =>
            {
                using (var templateStream = File.OpenRead(metadata.FullPath))
                {
                    var generatorResults = engine.GenerateCode(new StreamReader(templateStream), metadata.ClassName,
                        metadata.Namespace, metadata.Filename);

                    return CSharpSyntaxTree.ParseText(generatorResults.GeneratedCode);
                }
            }).ToArray();

            var compilation = CSharpCompilation.Create("ConsulTemplate.CompiledRazorTemplates", syntaxTrees,
                metadataReferences, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            _templateAssembly = Compile(compilation);
        }

        public TemplateDependencies AnalyzeDependencies(string templatePath)
        {
            var metadata = GetTemplateMetadata(templatePath);
            var instance = CreateTemplateInstance(metadata.FullTypeName);
            return instance.AnalyzeDependencies();
        }


        public void Render(string templatePath, TextWriter writer, ConsulState model)
        {
            var metadata = GetTemplateMetadata(templatePath);
            var instance = CreateTemplateInstance(metadata.FullTypeName);
            instance.Render(writer, model);
        }

        private ConsulTemplateBase CreateTemplateInstance(string typeName)
        {
            var templateType = _templateAssembly.GetType(typeName,
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

        private TemplateMetadata GetTemplateMetadata(string templatePath)
        {
            var fullPath = Path.GetFullPath(templatePath);
            if (_templateMetadata.TryGetValue(fullPath, out var metadata))
                return metadata;

            throw new ArgumentException($"Unknown template '{templatePath}'");
        }

        private class TemplateMetadata
        {
            private readonly string _fullPath;
            private static readonly Regex _validClassVarsRx = new Regex("[^a-z0-9]", RegexOptions.IgnoreCase);

            public TemplateMetadata(string fullPath)
            {
                _fullPath = fullPath;
            }

            public string FullPath => _fullPath;

            public string ClassName
            {
                get
                {
                    var hash = _fullPath.GetHashCode().ToString();
                    var filename = Path.GetFileNameWithoutExtension(_fullPath);
                    return _validClassVarsRx.Replace(filename, "") + "_" + hash.Replace('-', '_');
                }
            }

            public string Filename => Path.GetFileName(_fullPath);

            public string Namespace => "ConsulTemplate.CompiledRazorTemplates";

            public string FullTypeName => $"{Namespace}.{ClassName}";
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