using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.IO;
using Microsoft.CodeAnalysis.Emit;
using System.Text;
using CodeExecutor.DTO;

namespace CodeExecutor.Services
{
    public class CSharpCompileService
    {
        private readonly string assemblyName = "UserAssembly";
        private readonly string codeTemplate = 
            @"using System;
              using System.Linq;
              using System.Collections.Generic;

               public class Solution 
               {{ 
                   public {0} Solve({1}) 
                   {{ 
                       {2} 
                   }} 
               }}";

        public ExecutionResult Run(
            string type = "void",
            string body = "Console.WriteLine(\"Hello world\");",
            Dictionary<(string type, string id), object?> args = null)
        {
            string argsFormat = args is null || args.Count == 0
                ? string.Empty
                : string.Join(", ", args.Select(kvp => $"{kvp.Key.type} {kvp.Key.id}"));
            
            string code = string.Format(codeTemplate, type, argsFormat, body);

            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CSharpCompilationOptions options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary
            );

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                [tree],
                [
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)
                ],
                options
            );

            using MemoryStream ms = new();
            EmitResult result = compilation.Emit(ms);

            // Handle compilation errors
            if (!result.Success)
            {
                var errors = result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.GetMessage())
                    .ToList();

                return new()
                {
                    Success = false,
                    Error = string.Join(Environment.NewLine, errors),
                    ExitCode = 1
                };
            }

            // Capture Console output
            var originalOut = Console.Out;
            var originalError = Console.Error;

            using var stdoutWriter = new StringWriter();
            using var stderrWriter = new StringWriter();

            Console.SetOut(stdoutWriter);
            Console.SetError(stderrWriter);

            try
            {
                ms.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(ms.ToArray());
                Type? methodType = assembly.GetType("Solution");
                object? instance = Activator.CreateInstance(methodType!);

                if (instance is null)
                {
                    return new()
                    {
                        Success = false,
                        Error = "Failed to create instance of Solution class",
                        ExitCode = 1
                    };
                }

                MethodInfo? method = methodType?.GetMethod("Solve");
                if (method is null)
                {
                    return new()
                    {
                        Success = false,
                        Error = "Solve method not found",
                        ExitCode = 1
                    };
                }

                // Execute the method
                object?[]? methodArgs = args?.Values.ToArray();
                object? returnValue = method.Invoke(instance, methodArgs);

                // If method returns a value (not void), add it to stdout
                if (returnValue is not null && type != "void")
                {
                    stdoutWriter.WriteLine(returnValue);
                }

                return new()
                {
                    Stdout = stdoutWriter.ToString(),
                    Stderr = stderrWriter.ToString(),
                    Success = true,
                    ExitCode = 0
                };
            }
            catch (TargetInvocationException ex)
            {
                // This catches exceptions thrown by the invoked method
                return new()
                {
                    Success = false,
                    Error = ex.InnerException?.Message ?? ex.Message,
                    Stderr = stderrWriter.ToString(),
                    Stdout = stdoutWriter.ToString(),
                    ExitCode = 1
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    Success = false,
                    Error = ex.Message,
                    Stderr = stderrWriter.ToString(),
                    Stdout = stdoutWriter.ToString(),
                    ExitCode = 1
                };
            }
            finally
            {
                // Restore original Console output
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
    }
}
