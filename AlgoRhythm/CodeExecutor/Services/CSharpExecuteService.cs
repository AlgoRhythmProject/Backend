using AlgoRhythm.Shared.Models.CodeExecution;
using AlgoRhythm.Shared.Models.CodeExecution.Responses;
using CodeExecutor.Helpers;

namespace CodeExecutor.Services
{
    /// <summary>
    /// Service responsible for executing C# code dynamically.
    /// Compiles the user-provided code, executes a specified method, 
    /// captures console output, and returns execution results.
    /// </summary>
    public class CSharpExecuteService
    {
        private readonly CSharpCompiler _codeCompiler;
        public CSharpExecuteService(CSharpCompiler codeCompiler)
        {
            _codeCompiler = codeCompiler;
        }

        /// <summary>
        /// Main class method, which compiles and executes the provided C# code, invoking a specific method with optional arguments.
        /// Captures both standard output and error streams, and returns execution results including return value.
        /// </summary>
        /// <param name="timeout">Maximum time allowed for code execution.</param>
        /// <param name="args">Optional list of function parameters to pass to the target method.</param>
        /// <param name="executionClass">The fully qualified name of the class containing the method to invoke. Default is "Solution".</param>
        /// <param name="executionMethod">The name of the method to invoke. Default is "Solve".</param>
        /// <param name="code">The C# code to compile and execute. Default is a simple "Hello world" example.</param>
        /// <returns>An <see cref="ExecuteCodeResponse"/> containing execution status, output, errors, and return value.</returns>
        public ExecuteCodeResponse Run(
            TimeSpan timeout,
            List<FunctionParameter>? args = null,
            string executionClass = "Solution",
            string executionMethod = "Solve",
            string code = @"class Solution { public void Solve() { Console.WriteLine(""Hello world""); } }")
        {
            CSharpCompilationResult result = _codeCompiler.Compile(code, executionMethod);
            
            if (!result.Success || result.AssemblyStream is null)
            {
                return new ExecuteCodeResponse
                {
                    Success = false,
                    Error = string.Join(Environment.NewLine, result.Errors),
                    ExitCode = 1
                };
            }

            using var console = new ConsoleOrchestrator();

            try
            {
                object?[] methodArgs = args.ConvertArgs(result.ParsedArgs);
                CancellationTokenSource cts = new();
                cts.CancelAfter(timeout);

                var task = Task.Run(() =>
                    new AssemblyExecutor().Execute(
                        result.AssemblyStream, executionClass, executionMethod, methodArgs), cts.Token);

                if (!task.Wait(timeout))
                {
                    throw new TimeoutException("User code execution exceeded time limit.");
                }

                object? returnValue = task.Result;
                
                return new()
                {
                    Success = true,
                    Stdout = console.StdOut.ToString(),
                    Stderr = console.StdErr.ToString(),
                    ExitCode = 0,
                    ReturnedValue = task.Result ?? string.Empty,
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    Success = false,
                    Error = ex.Message,
                    Stderr = console.StdErr.ToString(),
                    Stdout = console.StdOut.ToString(),
                    ExitCode = 1,
                    ReturnedValue = string.Empty,
                };
            }
        }
    }
}