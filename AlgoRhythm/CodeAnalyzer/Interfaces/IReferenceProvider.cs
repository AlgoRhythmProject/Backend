using Microsoft.CodeAnalysis;

namespace CodeAnalyzer.Interfaces
{
    public interface IReferenceProvider
    {
        IReadOnlyList<MetadataReference> GetReferences();
    }

}
