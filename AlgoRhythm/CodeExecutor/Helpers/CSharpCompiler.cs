using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using AlgoRhythm.Shared.Models.CodeExecution;
using System.Diagnostics.CodeAnalysis;
using AlgoRhythm.Shared.Dtos.Submissions;

namespace CodeExecutor.Helpers
{
    /// <summary>
    /// Compiles C# code dynamically into an in-memory assembly using Roslyn.
    /// Supports configurable formatting, compilation options, and error reporting.
    /// </summary>
    public class CSharpCompiler
    {
        private readonly CSharpCodeFormatter _codeFormatter;
        public readonly string AssemblyName = "UserAssembly";
        public CSharpCompiler(CSharpCodeFormatter codeFormatter)
        {
            _codeFormatter = codeFormatter;  
        }

        /// <summary>
        /// Compiles the provided C# code into a dynamically linked in-memory assembly.
        /// </summary>
        /// <param name="code">The C# code to compile.</param>
        public CSharpCompilationResult Compile(string code, string methodName)
        {
            string formattedCode = _codeFormatter.Format(code);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(formattedCode);

            CSharpCompilationOptions compilationOptions = new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                allowUnsafe: false,
                checkOverflow: true,
                platform: Platform.X64,
                concurrentBuild: true,
                deterministic: true,
                nullableContextOptions: NullableContextOptions.Enable,
                reportSuppressedDiagnostics: false,
                warningLevel: 4,
                generalDiagnosticOption: ReportDiagnostic.Default
            );

            CSharpCompilation compilation = CSharpCompilation.Create(
                AssemblyName,
                [tree],
                GetMetadataReferences(),
                compilationOptions
            );

            EmitOptions emitOptions = new(
                debugInformationFormat: DebugInformationFormat.Embedded, 
                highEntropyVirtualAddressSpace: false);

            using MemoryStream ms = new();
            EmitResult result = compilation.Emit(ms, options: emitOptions);

            // Handle compilation errors
            if (!result.Success)
            {
                List<ExecutionErrorDto> errors = [.. result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => new ExecutionErrorDto(d)) ];

                return new(false, null, errors);
            }

            ms.Seek(0, SeekOrigin.Begin);

            return new(true, ms, []);
        }

        /// <summary>
        /// Gets the default metadata references required for compilation, including System.Runtime, mscorlib, Console, and LINQ.
        /// </summary>
        /// <returns>An enumerable of <see cref="MetadataReference"/> for compilation.</returns>
        private IEnumerable<MetadataReference> GetMetadataReferences()
        {
            yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(Console).Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(System.Text.StringBuilder).Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(System.Numerics.BigInteger).Assembly.Location);
            yield return MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location);
            yield return MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location);
        }

    }
}