using AlgoRhythm.Services.CodeExecutor;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using AlgoRhythm.Shared.Models.Tasks;

namespace UnitTests
{
    public class CSharpCodeParserUnitTests
    {
        private readonly CSharpCodeParser _parser;

        public CSharpCodeParserUnitTests()
        {
            _parser = new CSharpCodeParser();
        }

        [Fact]
        public void ParseToExecuteRequest_ValidCode_ReturnsCorrectRequest()
        {
            var code = @"
                public class Solution 
                {
                    public int Add(int a, int b)
                    {
                        return a + b;
                    }
                }";

            var result = _parser.ParseToExecuteRequest(code);

            Assert.NotNull(result);
            Assert.Equal("Solution", result.ExecutionClass);
            Assert.Equal("Add", result.ExecutionMethod);
            Assert.Equal(2, result.Args.Count);
            Assert.Equal("a", result.Args[0].Name);
            Assert.Equal("b", result.Args[1].Name);
            Assert.Equal(TimeSpan.FromSeconds(5), result.Timeout);
        }

        [Fact]
        public void ParseToExecuteRequest_WithInputJson_PopulatesArgValues()
        {
            var code = @"
                public class Calculator 
                {
                    public int Multiply(int x, int y)
                    {
                        return x * y;
                    }
                }";
            var inputJson = "{\"x\": 5, \"y\": 10}";

            var result = _parser.ParseToExecuteRequest(code, inputJson);

            Assert.NotNull(result);
            Assert.Equal("5", result.Args[0].Value);
            Assert.Equal("10", result.Args[1].Value);
        }

        [Fact]
        public void ParseToExecuteRequest_WithCustomTimeout_UsesCustomTimeout()
        {
            var code = @"
                public class Solution 
                {
                    public void DoSomething()
                    {
                    }
                }";
            var timeout = TimeSpan.FromSeconds(10);

            var result = _parser.ParseToExecuteRequest(code, null, timeout);

            Assert.Equal(timeout, result.Timeout);
        }

        [Fact]
        public void ParseToExecuteRequest_NoClassDeclaration_ThrowsInvalidOperationException()
        {
            var code = @"
                public int Add(int a, int b)
                {
                    return a + b;
                }";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                _parser.ParseToExecuteRequest(code));

            Assert.Contains("Cannot find class declaration", exception.Message);
        }

        [Fact]
        public void ParseToExecuteRequest_NoMethodDeclaration_ThrowsInvalidOperationException()
        {
            var code = @"
                public class Solution 
                {
                }";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                _parser.ParseToExecuteRequest(code));

            Assert.Contains("Cannot find method declaration", exception.Message);
        }

        [Fact]
        public void ParseToExecuteRequest_InvalidArgumentFormat_ThrowsInvalidOperationException()
        {
            var code = @"
                public class Solution 
                {
                    public int Add(int)
                    {
                        return 0;
                    }
                }";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                _parser.ParseToExecuteRequest(code));

            Assert.Contains("Invalid argument format", exception.Message);
        }

        [Fact]
        public void ParseToExecuteRequest_MultipleArguments_ParsesAllCorrectly()
        {
            var code = @"
                public class Solution 
                {
                    public string Concat(string first, string second, string third)
                    {
                        return first + second + third;
                    }
                }";

            var result = _parser.ParseToExecuteRequest(code);

            Assert.Equal(3, result.Args.Count);
            Assert.Equal("first", result.Args[0].Name);
            Assert.Equal("second", result.Args[1].Name);
            Assert.Equal("third", result.Args[2].Name);
        }

        [Fact]
        public void ParseToExecuteRequest_NoArguments_ReturnsEmptyArgsList()
        {
            var code = @"
                public class Solution 
                {
                    public int GetNumber()
                    {
                        return 42;
                    }
                }";

            var result = _parser.ParseToExecuteRequest(code);

            Assert.Empty(result.Args);
        }

        [Fact]
        public void BuildRequestsForTestCases_MultipleTestCases_ReturnsCorrectRequests()
        {
            var code = @"
                public class Solution 
                {
                    public int Add(int a, int b)
                    {
                        return a + b;
                    }
                }";

            var testCases = new List<TestCase>
            {
                new TestCase
                {
                    Id = Guid.NewGuid(),
                    InputJson = "{\"a\": 1, \"b\": 2}",
                    ExpectedJson = "3"
                },
                new TestCase
                {
                    Id = Guid.NewGuid(),
                    InputJson = "{\"a\": 5, \"b\": 10}",
                    ExpectedJson = "15"
                }
            };

            var results = _parser.BuildRequestsForTestCases(code, testCases);

            Assert.Equal(2, results.Count);
            Assert.Equal("1", results[0].Args[0].Value);
            Assert.Equal("2", results[0].Args[1].Value);
            Assert.Equal("3", results[0].ExpectedValue);
            Assert.Equal("5", results[1].Args[0].Value);
            Assert.Equal("10", results[1].Args[1].Value);
            Assert.Equal("15", results[1].ExpectedValue);
        }

        [Fact]
        public void BuildRequestsForTestCases_TestCaseWithoutInput_CreatesRequestWithEmptyArgs()
        {
            var code = @"
                public class Solution 
                {
                    public int Add(int a, int b)
                    {
                        return a + b;
                    }
                }";

            var testCases = new List<TestCase>
            {
                new TestCase
                {
                    Id = Guid.NewGuid(),
                    InputJson = null,
                    ExpectedJson = "0"
                }
            };

            var results = _parser.BuildRequestsForTestCases(code, testCases);

            Assert.Single(results);
            Assert.Empty(results[0].Args[0].Value);
            Assert.Empty(results[0].Args[1].Value);
        }

        [Fact]
        public void BuildRequestsForTestCases_WithCustomTimeout_UsesCustomTimeout()
        {
            var code = @"
                public class Solution 
                {
                    public void Method()
                    {
                    }
                }";

            var testCases = new List<TestCase>
            {
                new TestCase { Id = Guid.NewGuid(), InputJson = "{}", ExpectedJson = "" }
            };
            var timeout = TimeSpan.FromSeconds(20);

            var results = _parser.BuildRequestsForTestCases(code, testCases, timeout);

            Assert.All(results, r => Assert.Equal(timeout, r.Timeout));
        }

        [Fact]
        public void BuildRequestsForTestCases_InvalidInputJson_CreatesRequestWithEmptyArgs()
        {
            var code = @"
                public class Solution 
                {
                    public int Add(int a, int b)
                    {
                        return a + b;
                    }
                }";

            var testCases = new List<TestCase>
            {
                new TestCase
                {
                    Id = Guid.NewGuid(),
                    InputJson = "invalid json",
                    ExpectedJson = "0"
                }
            };

            var results = _parser.BuildRequestsForTestCases(code, testCases);

            Assert.Single(results);
            Assert.Empty(results[0].Args[0].Value);
            Assert.Empty(results[0].Args[1].Value);
        }

        [Fact]
        public void ValidateArguments_ValidTestCases_DoesNotThrow()
        {
            var code = @"
                public class Solution 
                {
                    public int Add(int a, int b)
                    {
                        return a + b;
                    }
                }";

            var testCases = new List<TestCase>
            {
                new TestCase
                {
                    Id = Guid.NewGuid(),
                    InputJson = "{\"a\": 1, \"b\": 2}",
                    ExpectedJson = "3"
                }
            };

            var exception = Record.Exception(() =>
                _parser.ValidateArguments(code, testCases));

            Assert.Null(exception);
        }

        [Fact]
        public void ValidateArguments_MissingInput_ThrowsInvalidOperationException()
        {
            var code = @"
                public class Solution 
                {
                    public int Add(int a, int b)
                    {
                        return a + b;
                    }
                }";

            var testCases = new List<TestCase>
            {
                new TestCase
                {
                    Id = Guid.NewGuid(),
                    InputJson = null,
                    ExpectedJson = "3"
                }
            };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                _parser.ValidateArguments(code, testCases));

            Assert.Contains("No input provided", exception.Message);
        }

        [Fact]
        public void ValidateArguments_InvalidJson_ThrowsInvalidOperationException()
        {
            var code = @"
                public class Solution 
                {
                    public int Add(int a, int b)
                    {
                        return a + b;
                    }
                }";

            var testCases = new List<TestCase>
            {
                new TestCase
                {
                    Id = Guid.NewGuid(),
                    InputJson = "not valid json",
                    ExpectedJson = "3"
                }
            };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                _parser.ValidateArguments(code, testCases));

            Assert.Contains("Failed to parse InputJson", exception.Message);
        }

        [Fact]
        public void ValidateArguments_MissingArgument_ThrowsInvalidOperationException()
        {
            var code = @"
                public class Solution 
                {
                    public int Add(int a, int b)
                    {
                        return a + b;
                    }
                }";

            var testCases = new List<TestCase>
            {
                new TestCase
                {
                    Id = Guid.NewGuid(),
                    InputJson = "{\"a\": 1}",
                    ExpectedJson = "3"
                }
            };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                _parser.ValidateArguments(code, testCases));

            Assert.Contains("Missing argument 'b'", exception.Message);
        }

        [Fact]
        public void ValidateArguments_MultipleErrors_ThrowsWithAllErrors()
        {
            var code = @"
                public class Solution 
                {
                    public int Add(int a, int b)
                    {
                        return a + b;
                    }
                }";

            var testCaseId1 = Guid.NewGuid();
            var testCaseId2 = Guid.NewGuid();

            var testCases = new List<TestCase>
            {
                new TestCase
                {
                    Id = testCaseId1,
                    InputJson = null,
                    ExpectedJson = "3"
                },
                new TestCase
                {
                    Id = testCaseId2,
                    InputJson = "{\"a\": 1}",
                    ExpectedJson = "3"
                }
            };

            var exception = Assert.Throws<InvalidOperationException>(() =>
                _parser.ValidateArguments(code, testCases));

            Assert.Contains(testCaseId1.ToString(), exception.Message);
            Assert.Contains(testCaseId2.ToString(), exception.Message);
            Assert.Contains("No input provided", exception.Message);
            Assert.Contains("Missing argument", exception.Message);
        }

        [Fact]
        public void ParseToExecuteRequest_ComplexReturnType_ParsesCorrectly()
        {
            var code = @"
                public class Solution 
                {
                    public List<int> GetNumbers(int count)
                    {
                        return new List<int>();
                    }
                }";

            var result = _parser.ParseToExecuteRequest(code);

            Assert.NotNull(result);
            Assert.Equal("Solution", result.ExecutionClass);
            Assert.Equal("GetNumbers", result.ExecutionMethod);
            Assert.Single(result.Args);
            Assert.Equal("count", result.Args[0].Name);
        }

        [Fact]
        public void ParseToExecuteRequest_ArrayParameter_ParsesCorrectly()
        {
            var code = @"
                public class Solution 
                {
                    public int Sum(int[] numbers)
                    {
                        return 0;
                    }
                }";

            var result = _parser.ParseToExecuteRequest(code);

            Assert.NotNull(result);
            Assert.Single(result.Args);
            Assert.Equal("numbers", result.Args[0].Name);
        }
    }
}