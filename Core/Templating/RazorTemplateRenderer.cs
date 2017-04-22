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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace ConsulRazor.Templating
{
    public class RazorTemplateRenderer : ITemplateRenderer
    {
        private readonly IRazorTemplateCompiler _compiler;
        private readonly Lazy<Assembly> _templateAssembly;
        private readonly IDictionary<string, TemplateMetadata> _templateMetadata;
        private Type _baseClass = typeof(ConsulTemplateBase);
        private Action<ConsulTemplateBase> _configureTemplate = _ => {};

        public RazorTemplateRenderer(IEnumerable<string> templatePaths, IRazorTemplateCompiler compiler)
        {
            var templatePathArray = templatePaths.Select(Path.GetFullPath).ToArray();
            var partialTemplates = FindPartialTemplates(templatePathArray);
            _templateMetadata = templatePathArray.Concat(partialTemplates).Select(t => new TemplateMetadata(Path.GetFullPath(t))).ToDictionary(m => m.FullPath);
            _compiler = compiler;
            _templateAssembly = new Lazy<Assembly>(Compile, LazyThreadSafetyMode.ExecutionAndPublication);
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

        private Assembly Compile()
        {
            return _compiler.Compile(_templateMetadata.Values, _baseClass);
        }

        private TemplateMetadata GetTemplateMetadata(string templatePath)
        {
            var fullPath = Path.GetFullPath(templatePath);
            if (_templateMetadata.TryGetValue(fullPath, out var metadata))
                return metadata;

            throw new ArgumentException($"Unknown template '{templatePath}'");
        }
    }
}