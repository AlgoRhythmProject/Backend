using CodeAnalyzer.Interfaces;
using Microsoft.CodeAnalysis;

namespace CodeAnalyzer.Services
{
    /// <summary>
    /// Provides the necessary metadata references required for Roslyn to perform code analysis.
    /// This implementation dynamically loads assemblies from the current .NET runtime 
    /// and specific shared libraries to ensure the compiler understands the available types.
    /// </summary>
    public sealed class RuntimeReferenceProvider : IReferenceProvider
    {
        private readonly IReadOnlyList<MetadataReference> _references;

        public RuntimeReferenceProvider()
        {
            _references = Load();
        }

        /// <summary>
        /// Gets the cached list of metadata references for use in compilation or diagnostic analysis.
        /// </summary>
        public IReadOnlyList<MetadataReference> GetReferences() => _references;

        /// <summary>
        /// Scans the Trusted Platform Assemblies (TPA) and the current runtime directory 
        /// to create metadata references for all core .NET libraries.
        /// </summary>
        /// <remarks>
        /// This method also includes custom shared assemblies (like IGraph) to allow 
        /// the analyzed code to interact with the system's core interfaces.
        /// </remarks>
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
            var sharedAssemblyLocation = typeof(Graph.IGraph).Assembly.Location;
            list.Add(MetadataReference.CreateFromFile(sharedAssemblyLocation));

            return list;
        }
    }
}
