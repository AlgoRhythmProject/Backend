using CodeAnalyzer.Interfaces;
using Microsoft.CodeAnalysis;

namespace CodeAnalyzer.Services
{
    public sealed class RuntimeReferenceProvider : IReferenceProvider
    {
        private readonly IReadOnlyList<MetadataReference> _references;

        public RuntimeReferenceProvider()
        {
            _references = Load();
        }

        public IReadOnlyList<MetadataReference> GetReferences() => _references;

        private static IReadOnlyList<MetadataReference> Load()
        {
            var list = new List<MetadataReference>();
            var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
            if (tpa == null) return list;

            var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

            foreach (var path in tpa.Split(Path.PathSeparator))
            {
                if (path.EndsWith(".dll") &&
                    path.StartsWith(runtimeDir, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(MetadataReference.CreateFromFile(path));
                }
            }

            return list;
        }
    }


}
