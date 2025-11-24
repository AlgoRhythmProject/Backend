using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using CodeExecutor.Helpers;
using CodeExecutor.Services;
using Newtonsoft.Json;

namespace UnitTests
{
    public class CodeExecutorTests
    {
        private readonly CSharpCodeFormatter _codeFormatter;
        private readonly CSharpCompiler _compiler;
        private readonly CSharpExecuteService _service;

        public CodeExecutorTests()
        {
            _codeFormatter = new CSharpCodeFormatter();
            _compiler = new CSharpCompiler(_codeFormatter);
            _service = new CSharpExecuteService(_compiler);
        }


        [Fact]
        public async Task Test_RunTests_ReturnedValue()
        {
            // Arrange
            List<ExecuteCodeRequest> requests =
            [
                new()
                {
                    TestCaseId = Guid.Empty,
                    Code = @"using System;
                            public class Solution
                            {    
                                public int Solve(int a, int b)
                                {        
                                    return a + b;    
                                }
                            }",
                    Args =
                    [
                        new()
                        {
                            Name = "a",
                            Value = "5",
                        },
                        new()
                        {
                            Name = "b",
                            Value = "6",
                        }
                    ],
                    Timeout = TimeSpan.FromSeconds(1),
                    ExpectedValue = "11",
                    MaxPoints = 1
                }
            ];

            // Act
            TestResultDto res = (await _service.RunTests(requests)).First();

            // Assert
            Assert.True(res.Passed);
            Assert.Equal(11, res.ReturnedValue);

        }

        [Fact]
        public async Task Test_RunTests_StdOut()
        {
            // Arrange
            List<ExecuteCodeRequest> requests =
            [
                new()
                {
                    TestCaseId = Guid.Empty,
                    Code = @"using System;
                            public class Solution
                            {    
                                public void Solve(int a, int b)
                                {        
                                    Console.WriteLine(a);
                                    Console.WriteLine(b);    
                                }
                            }",
                    Args =
                    [
                        new()
                        {
                            Name = "a",
                            Value = "5",
                        },
                        new()
                        {
                            Name = "b",
                            Value = "6",
                        }
                    ],
                    Timeout = TimeSpan.FromSeconds(1),
                    MaxPoints = 1
                }
            ];

            // Act
            TestResultDto res = (await _service.RunTests(requests)).First();

            // Assert
            Assert.True(res.Passed);
            Assert.Equal("5\r\n6\r\n", res.StdOut);

        }

        [Fact]
        public async Task Test_RunTests_CompilationFailure_ReturnsErrorForAllRequests()
        {
            // Arrange
            List<ExecuteCodeRequest> requests =
            [
                new()
                {
                    TestCaseId = Guid.NewGuid(),
                    Code = @"using System;
                            public class Solution
                            {    
                                public int Solve(int a, int b)
                                {        
                                    return a + b // Missing semicolon
                                }
                            }",
                    Args =
                    [
                        new() { Name = "a", Value = "5" },
                        new() { Name = "b", Value = "6" }
                    ],
                    Timeout = TimeSpan.FromSeconds(1),
                    ExpectedValue = "11",
                    MaxPoints = 10
                },
                new()
                {
                    TestCaseId = Guid.NewGuid(),
                    Code = @"using System;
                            public class Solution
                            {    
                                public int Solve(int a, int b)
                                {        
                                    return a + b // Missing semicolon
                                }
                            }",
                    Args =
                    [
                        new() { Name = "a", Value = "10" },
                        new() { Name = "b", Value = "20" }
                    ],
                    Timeout = TimeSpan.FromSeconds(1),
                    ExpectedValue = "30",
                    MaxPoints = 10
                }
            ];

            // Act
            List<TestResultDto> results = await _service.RunTests(requests);

            // Assert
            Assert.Equal(2, results.Count);
            Assert.All(results, result =>
            {
                Assert.False(result.Passed);
                Assert.Equal(1, result.ExitCode);
                Assert.Equal(0, result.Points);
                Assert.NotEmpty(result.Errors);
            });
        }

        [Fact]
        public async Task Test_RunTests_Timeout_ReturnsTimeoutError()
        {
            // Arrange
            List<ExecuteCodeRequest> requests =
            [
                new()
                {
                    TestCaseId = Guid.NewGuid(),
                    Code = @"using System;
                            using System.Threading;
                            public class Solution
                            {    
                                public int Solve(int a, int b)
                                {        
                                    Thread.Sleep(5000); // Sleep for 5 seconds
                                    return a + b;    
                                }
                            }",
                    Args =
                    [
                        new() { Name = "a", Value = "5" },
                        new() { Name = "b", Value = "6" }
                    ],
                    Timeout = TimeSpan.FromMilliseconds(100), // Very short timeout
                    ExpectedValue = "11",
                    MaxPoints = 10
                }
            ];

            // Act
            TestResultDto result = (await _service.RunTests(requests)).First();

            // Assert
            Assert.False(result.Passed);
            Assert.Equal(1, result.ExitCode);
            Assert.Equal(0, result.Points);
            Assert.NotEmpty(result.Errors);
            Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Test_RunTests_RuntimeException_ReturnsErrorWithExceptionMessage()
        {
            // Arrange
            List<ExecuteCodeRequest> requests =
            [
                new()
                {
                    TestCaseId = Guid.NewGuid(),
                    Code = @"using System;
                            public class Solution
                            {    
                                public int Solve(int a, int b)
                                {        
                                    throw new InvalidOperationException(""Something went wrong"");
                                }
                            }",
                    Args =
                    [
                        new() { Name = "a", Value = "5" },
                        new() { Name = "b", Value = "6" }
                    ],
                    Timeout = TimeSpan.FromSeconds(1),
                    ExpectedValue = "11",
                    MaxPoints = 10
                }
            ];

            // Act
            TestResultDto result = (await _service.RunTests(requests)).First();

            // Assert
            Assert.False(result.Passed);
            Assert.Equal(1, result.ExitCode);
            Assert.Equal(0, result.Points);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task Test_RunTests_MultipleRequests_ReturnsMultipleResults()
        {
            // Arrange
            List<ExecuteCodeRequest> requests =
            [
                new()
                {
                    TestCaseId = Guid.NewGuid(),
                    Code = @"using System;
                            public class Solution
                            {    
                                public int Solve(int a, int b)
                                {        
                                    return a + b;    
                                }
                            }",
                    Args =
                    [
                        new() { Name = "a", Value = "5" },
                        new() { Name = "b", Value = "6" }
                    ],
                    Timeout = TimeSpan.FromSeconds(1),
                    ExpectedValue = "11",
                    MaxPoints = 10
                },
                new()
                {
                    TestCaseId = Guid.NewGuid(),
                    Code = @"using System;
                            public class Solution
                            {    
                                public int Solve(int a, int b)
                                {        
                                    return a + b;    
                                }
                            }",
                    Args =
                    [
                        new() { Name = "a", Value = "10" },
                        new() { Name = "b", Value = "20" }
                    ],
                    Timeout = TimeSpan.FromSeconds(1),
                    ExpectedValue = "30",
                    MaxPoints = 15
                }
            ];

            // Act
            List<TestResultDto> results = await _service.RunTests(requests);

            // Assert
            Assert.Equal(2, results.Count);
            Assert.All(results, result =>
            {
                Assert.True(result.Passed);
                Assert.Equal(0, result.ExitCode);
            });
            Assert.Equal(11, results[0].ReturnedValue);
            Assert.Equal(30, results[1].ReturnedValue);
        }

        [Fact]
        public async Task Test_RunTests_StdErr_CapturesErrorOutput()
        {
            // Arrange
            List<ExecuteCodeRequest> requests =
            [
                new()
                {
                    TestCaseId = Guid.NewGuid(),
                    Code = @"using System;
                            public class Solution
                            {    
                                public int Solve(int a, int b)
                                {        
                                    Console.Error.WriteLine(""Error message"");
                                    return a + b;    
                                }
                            }",
                    Args =
                    [
                        new() { Name = "a", Value = "5" },
                        new() { Name = "b", Value = "6" }
                    ],
                    Timeout = TimeSpan.FromSeconds(1),
                    ExpectedValue = "11",
                    MaxPoints = 10
                }
            ];

            // Act
            TestResultDto result = (await _service.RunTests(requests)).First();

            // Assert
            Assert.True(result.Passed);
            Assert.Contains("Error message", result.StdErr);
        }

        [Fact]
        public async Task Test_RunTests_PointsCalculation_BasedOnExecutionTime()
        {
            // Arrange
            List<ExecuteCodeRequest> requests =
            [
                new()
                {
                    TestCaseId = Guid.NewGuid(),
                    Code = @"using System;
                            public class Solution
                            {    
                                public int Solve(int a, int b)
                                {        
                                    return a + b;    
                                }
                            }",
                    Args =
                    [
                        new() { Name = "a", Value = "5" },
                        new() { Name = "b", Value = "6" }
                    ],
                    Timeout = TimeSpan.FromSeconds(10),
                    ExpectedValue = "11",
                    MaxPoints = 100
                }
            ];

            // Act
            TestResultDto result = (await _service.RunTests(requests)).First();

            // Assert
            Assert.True(result.Passed);
            Assert.True(result.Points > 0 && result.Points <= 100);
            Assert.True(result.ExecutionTimeMs > 0);
        }

        [Fact]
        public async Task Test_RunTests_FailedTest_ReturnsZeroPoints()
        {
            // Arrange
            List<ExecuteCodeRequest> requests =
            [
                new()
                {
                    TestCaseId = Guid.NewGuid(),
                    Code = @"using System;
                            public class Solution
                            {    
                                public int Solve(int a, int b)
                                {        
                                    return a - b; // Wrong operation
                                }
                            }",
                    Args =
                    [
                        new() { Name = "a", Value = "5" },
                        new() { Name = "b", Value = "6" }
                    ],
                    Timeout = TimeSpan.FromSeconds(1),
                    ExpectedValue = "11",
                    MaxPoints = 100
                }
            ];

            // Act
            TestResultDto result = (await _service.RunTests(requests)).First();

            // Assert
            Assert.False(result.Passed);
            Assert.Equal(0, result.Points);
        }

        [Fact]
        public async Task Test_RunTests_TestCaseId_IsPreserved()
        {
            // Arrange
            Guid testCaseId = Guid.NewGuid();
            List<ExecuteCodeRequest> requests =
            [
                new()
                {
                    TestCaseId = testCaseId,
                    Code = @"using System;
                            public class Solution
                            {    
                                public int Solve(int a, int b)
                                {        
                                    return a + b;    
                                }
                            }",
                    Args =
                    [
                        new() { Name = "a", Value = "5" },
                        new() { Name = "b", Value = "6" }
                    ],
                    Timeout = TimeSpan.FromSeconds(1),
                    ExpectedValue = "11",
                    MaxPoints = 10
                }
            ];

            // Act
            TestResultDto result = (await _service.RunTests(requests)).First();

            // Assert
            Assert.Equal(testCaseId, result.TestCaseId);
        }

        [Fact]
        public async Task Test_RunTests_VoidReturn_WithExpectedValue_ChecksCorrectly()
        {
            // Arrange
            List<ExecuteCodeRequest> requests =
            [
                new()
                {
                    TestCaseId = Guid.NewGuid(),
                    Code = @"using System;
                            public class Solution
                            {    
                                public void Solve(int a, int b)
                                {        
                                    Console.WriteLine(a + b);    
                                }
                            }",
                    Args =
                    [
                        new() { Name = "a", Value = "5" },
                        new() { Name = "b", Value = "6" }
                    ],
                    Timeout = TimeSpan.FromSeconds(1),
                    MaxPoints = 10
                }
            ];

            // Act
            TestResultDto result = (await _service.RunTests(requests)).First();

            // Assert
            Assert.True(result.Passed);
            Assert.Equal(0, result.ExitCode);
        }


        [Fact]
        public async Task Test_RunTests_MultiFunctions()
        {
            // Arrange
            List<ExecuteCodeRequest> requests =
            [
                new()
                {
                    TestCaseId = Guid.NewGuid(),
                    Code = @"using System;

                            public class MathHelper
                            {
                                public int Add(int a, int b) => a + b;
                                public int Multiply(int a, int b) => a * b;
                            }

                            public class Calculator
                            {
                                private MathHelper helper = new MathHelper();
    
                                public int Calculate(int x, int y)
                                {
                                    int sum = helper.Add(x, y);
                                    int product = helper.Multiply(x, y);
                                    Console.WriteLine(""Sum: "" + sum + "", Product: "" + product);
                                    return sum + product;
                                }
                            }",
                    Args =
                    [
                        new() { Name = "x", Value = "5" },
                        new() { Name = "y", Value = "3" }
                    ],
                    Timeout = TimeSpan.FromSeconds(1),
                    MaxPoints = 10,
                    ExecutionClass = "Calculator",
                    ExecutionMethod = "Calculate",
                    ExpectedValue = "23"
                }
            ];

            // Act
            TestResultDto result = (await _service.RunTests(requests)).First();
            Assert.True(result.Passed);
            Assert.Equal(23, result.ReturnedValue);
        }

        [Fact]
        public async Task Test_RunTests_StaticClasses()
        {
            // Arrange
            List<ExecuteCodeRequest> requests =
            [
                new()
                {
                    TestCaseId = Guid.NewGuid(),
                    Code = @"using System;
                            public static class StringUtils
                            {
                                public static string Reverse(string input)
                                {
                                    char[] chars = input.ToCharArray();
                                    Array.Reverse(chars);
                                    return new string(chars);
                                }
                        
                                public static bool IsPalindrome(string input)
                                {
                                    string reversed = StringUtils.Reverse(input);
                                    return input.Equals(reversed, StringComparison.OrdinalIgnoreCase);
                                }
                            }",
                    Args =
                    [
                        new() { Name = "input", Value = JsonConvert.SerializeObject("racecar") }, // Note: Important for strings!
                    ],
                    Timeout = TimeSpan.FromSeconds(1),
                    MaxPoints = 10,
                    ExecutionClass = "StringUtils",
                    ExecutionMethod = "IsPalindrome",
                    ExpectedValue = "true"
                }
            ];

            // Act
            TestResultDto result = (await _service.RunTests(requests)).First();

            // Assert
            Assert.True(result.Passed);
            Assert.IsType<bool>(result.ReturnedValue);
            Assert.Equal(true, result.ReturnedValue);
        }
    }

}