using System.IO;
using System.Text.RegularExpressions;

namespace ConsulRx.Templating
{
    public class TemplateMetadata
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