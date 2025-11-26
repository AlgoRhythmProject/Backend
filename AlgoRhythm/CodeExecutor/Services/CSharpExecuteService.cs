using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Shared.Models.CodeExecution;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using CodeExecutor.Helpers;
using System.Diagnostics;

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
        /// <returns>An <see cref="TestResultDto"/> containing execution status, output, errors, and return value.</returns>
        public TestResultDto Run(
            TimeSpan timeout,
            string expectedValue,
            List<FunctionParameter>? args = null,
            string executionClass = "Solution",
            string executionMethod = "Solve",
            string code = @"class Solution { public void Solve() { Console.WriteLine(""Hello world""); } }")
        {
            CSharpCompilationResult result = _codeCompiler.Compile(code, executionMethod);

            if (!result.Success || result.AssemblyStream is null)
            {
                return new TestResultDto
                {
                    Passed = false,
                    Errors = result.Errors,
                    ExitCode = 1
                };
            }

            using var console = new ConsoleOrchestrator();

            try
            {
                CancellationTokenSource cts = new();
                cts.CancelAfter(timeout);

                var task = Task.Run(() =>
                    new AssemblyExecutor().Execute(
                        result.AssemblyStream, executionClass, executionMethod, args, expectedValue), cts.Token);

                if (!task.Wait(timeout))
                {
                    throw new TimeoutException("User code execution exceeded time limit.");
                }

                return new()
                {
                    Passed = true,
                    StdOut = console.StdOut.ToString(),
                    StdErr = console.StdErr.ToString(),
                    ExitCode = 0,
                    ReturnedValue = task.Result.returnedValue ?? string.Empty,
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    Passed = false,
                    Errors = [new(ex.Message)],
                    StdErr = console.StdErr.ToString(),
                    StdOut = console.StdOut.ToString(),
                    ExitCode = 1,
                    ReturnedValue = string.Empty,
                };
            }
        }

        public async Task<List<TestResultDto>> RunTests(
            List<ExecuteCodeRequest> requests)
        {
            string code = requests[0].Code;
            string executionMethod = requests[0].ExecutionMethod;

            CSharpCompilationResult result = _codeCompiler.Compile(code, executionMethod);
            
            // Code didn't compile
            if (!result.Success || result.AssemblyStream is null)
            {

                return [.. requests.Select(r => new TestResultDto()
                {
                    TestCaseId = r.TestCaseId,
                    Passed = false,
                    Errors = result.Errors,
                    ExitCode = 1,
                    Points = 0,
                })];
            }

            List<TestResultDto> results = [];

            foreach (var request in requests)
            {
                using var console = new ConsoleOrchestrator();
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    using CancellationTokenSource cts = new();
                    result.AssemblyStream.Position = 0;

                    (bool? passed, object? returnValue) = await Task.Run(() =>
                        new AssemblyExecutor().Execute(
                            result.AssemblyStream,
                            request.ExecutionClass,
                            request.ExecutionMethod,
                            request.Args,
                            request.ExpectedValue), cts.Token)
                        .WaitAsync(request.Timeout);

                    stopwatch.Stop();

                    results.Add(new TestResultDto()
                    {
                        TestCaseId = request.TestCaseId,
                        Passed = passed.GetValueOrDefault(),
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                        ReturnedValue = returnValue ?? string.Empty,
                        StdOut = console.StdOut.ToString(),
                        StdErr = console.StdErr.ToString(),
                        ExitCode = 0,
                        Points = Grade(passed, stopwatch.ElapsedMilliseconds, request.Timeout.TotalMilliseconds, request.MaxPoints),
                    });
                }
                catch (TimeoutException)
                {
                    stopwatch.Stop();
                    results.Add(new TestResultDto()
                    {
                        TestCaseId = request.TestCaseId,
                        Passed = false,
                        Errors = [new("Execution timeout")],
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                        StdOut = console.StdOut.ToString(),
                        StdErr = console.StdErr.ToString(),
                        ExitCode = 1,
                        Points = 0
                    });
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    results.Add(new TestResultDto()
                    {
                        TestCaseId = request.TestCaseId,
                        Passed = false,
                        Errors = [new(ex.Message)],
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                        StdOut = console.StdOut.ToString(),
                        StdErr = console.StdErr.ToString(),
                        ExitCode = 1,
                        Points = 0
                    });
                }
            }

            return results;
        }

        private int Grade(bool? passed, double elapsedMs, double expectedMs, int maxPoints = 10)
        {
            if (!passed.GetValueOrDefault()) return 0;

            return (elapsedMs / expectedMs) switch
            {
                < 0.25 => maxPoints,                // Full points if < 25% of timeout
                < 0.50 => (int)(maxPoints * 0.8),   // 80% if < 50% of timeout
                < 0.75 => (int)(maxPoints * 0.6),   // 60% if < 75% of timeout
                _ => (int)(maxPoints * 0.4)         // 40% otherwise
            };
        }
    }
}