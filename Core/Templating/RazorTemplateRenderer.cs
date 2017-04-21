using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using System.Threading;
using ConsulRazor.Reactive;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace ConsulRazor.Templating
{
    public class RazorTemplateRenderer : ITemplateRenderer
    {
        private readonly Lazy<Assembly> _templateAssembly;
        private readonly IDictionary<string, TemplateMetadata> _templateMetadata;
        private Type _baseClass = typeof(ConsulTemplateBase);
        private Action<ConsulTemplateBase> _configureTemplate = _ => {};

        public RazorTemplateRenderer(IEnumerable<string> templatePaths)
        {
            var templatePathArray = templatePaths.Select(Path.GetFullPath).ToArray();
            var partialTemplates = FindPartialTemplates(templatePathArray);
            _templateMetadata = templatePathArray.Concat(partialTemplates).Select(t => new TemplateMetadata(Path.GetFullPath(t))).ToDictionary(m => m.FullPath);

            _templateAssembly = new Lazy<Assembly>(CompileAssembly, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private IEnumerable<string> FindPartialTemplates(string[] templatePaths)
        {
            var directories = templatePaths.Select(Path.GetDirectoryName).Distinct();
            var fileExtensions = templatePaths.Select(Path.GetExtension).Distinct();
            return fileExtensions.SelectMany(
                ext => directories.SelectMany(dir =>
                    Directory.GetFiles(dir, $"*{ext}", SearchOption.AllDirectories)
                    .Where(path => Path.GetFileName(path).StartsWith("_"))));
        }

        public void UseBaseClass<T>(Action<T> configure) where T : ConsulTemplateBase
        {
            _baseClass = typeof(T);
            _configureTemplate = t => configure((T) t);
        }

        private Assembly CompileAssembly()
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
                DefaultBaseClass = _baseClass.FullName,
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
                })
                .ToArray();

            var compilation = CSharpCompilation.Create("ConsulTemplate.CompiledRazorTemplates", syntaxTrees,
                metadataReferences, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return Compile(compilation);
        }

        public TemplateDependencies AnalyzeDependencies(string templatePath, PropertyBag properties = null)
        {
            var metadata = GetTemplateMetadata(templatePath);
            var instance = CreateTemplateInstance(metadata.FullTypeName, templatePath);
            return instance.AnalyzeDependencies(properties, this);
        }

        public TemplateDependencies AnalyzePartialDependencies(string name, string parentTemplatePath, PropertyBag properties)
        {
            var templatePath = ResolvePartial(name, parentTemplatePath);
            return AnalyzeDependencies(templatePath, properties);
        }

        public void Render(string templatePath, TextWriter writer, ConsulState model, PropertyBag properties = null)
        {
            var metadata = GetTemplateMetadata(templatePath);
            var instance = CreateTemplateInstance(metadata.FullTypeName, templatePath);
            instance.Render(writer, model, this, properties);
        }

        public void RenderPartial(string name, string parentTemplatePath, TextWriter writer, ConsulState model, PropertyBag properties)
        {
            var templatePath = ResolvePartial(name, parentTemplatePath);
            Render(templatePath, writer, model, properties);
        }

        private string ResolvePartial(string name, string parentTemplatePath)
        {
            var partialShortName = Path.GetFileNameWithoutExtension(name);
            var partialExtension = Path.GetExtension(parentTemplatePath);
            var partialDirectory = Path.GetDirectoryName(name);
            var contextDirectory = Path.GetDirectoryName(parentTemplatePath);
            var filename = $"_{partialShortName}{partialExtension}";

            var partialFullPath = Path.GetFullPath(Path.Combine(contextDirectory, partialDirectory, filename));
            if (!_templateMetadata.ContainsKey(partialFullPath))
            {
                throw new ArgumentException($"Could not find the partial '{name}'. Expected to find it at '{partialFullPath}'");
            }

            return partialFullPath;
        }

        private ConsulTemplateBase CreateTemplateInstance(string typeName, string templatePath)
        {
            var templateType = _templateAssembly.Value.GetType(typeName,
                throwOnError: true);

            var instance = (ConsulTemplateBase) Activator.CreateInstance(templateType);
            instance.TemplatePath = templatePath;
            _configureTemplate(instance);

            return instance;
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