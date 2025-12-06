using AlgoRhythm.Services.Interfaces;
using AlgoRhythm.Shared.Models.CodeExecution;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using AlgoRhythm.Shared.Models.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AlgoRhythm.Services;

public class CSharpCodeParser : ICodeParser
{
    public ExecuteCodeRequest ParseToExecuteRequest(string code, string? inputJson = null, TimeSpan? timeout = null)
    {
        try
        {
            var classMatch = Regex.Match(code,
                @"public\s+class\s+(?<classname>\w+)",
                RegexOptions.Multiline);

            if (!classMatch.Success)
                throw new FormatException("Cannot find class declaration in code.");

            var className = classMatch.Groups["classname"].Value;

            var methodMatch = Regex.Match(code,
                @"public\s+(?<return>[^\s]+)\s+(?<method>\w+)\s*\((?<args>[^)]*)\)",
                RegexOptions.Multiline);

            if (!methodMatch.Success)
                throw new FormatException("Cannot find method declaration in code.");

            var methodName = methodMatch.Groups["method"].Value;

            var argsList = methodMatch.Groups["args"].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(a =>
                {
                    var parts = a.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        throw new FormatException($"Invalid argument format: '{a}'. Expected 'Type Name'.");
                    return new FunctionParameter
                    {
                        Name = parts[1],
                        Value = string.Empty
                    };
                }).ToList();

            if (!string.IsNullOrWhiteSpace(inputJson))
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(inputJson)
                           ?? new Dictionary<string, JsonElement>();

                foreach (var arg in argsList)
                {
                    if (dict.TryGetValue(arg.Name, out var value))
                        arg.Value = value.ToString() ?? string.Empty;
                }
            }

            return new ExecuteCodeRequest
            {
                Code = code,
                Args = argsList,
                Timeout = timeout ?? TimeSpan.FromSeconds(5),
                ExecutionClass = className,
                ExecutionMethod = methodName,
            };
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Code parsing error: {ex.Message}");
        }
    }

    public List<ExecuteCodeRequest> BuildRequestsForTestCases(string code, IEnumerable<TestCase> testCases, TimeSpan? timeout = null)
    {
        var templateRequest = ParseToExecuteRequest(code, null, timeout);

        var requests = new List<ExecuteCodeRequest>();

        foreach (var tc in testCases)
        {
            var argsForTest = templateRequest.Args
                .Select(a => new FunctionParameter { Name = a.Name, Value = string.Empty })
                .ToList();

            if (!string.IsNullOrWhiteSpace(tc.InputJson))
            {
                try
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tc.InputJson)
                               ?? [];

                    foreach (var arg in argsForTest)
                    {
                        if (dict.TryGetValue(arg.Name, out var value))
                            arg.Value = value.ToString() ?? string.Empty;
                    }
                }
                catch
                {
                }
            }

            requests.Add(new ExecuteCodeRequest
            {
                TestCaseId = tc.Id,
                Code = templateRequest.Code,
                ExecutionClass = templateRequest.ExecutionClass,
                ExecutionMethod = templateRequest.ExecutionMethod,
                Args = argsForTest,
                Timeout = templateRequest.Timeout,
                ExpectedValue = tc?.ExpectedJson ?? string.Empty,
            });
        }

        return requests;
    }

    public void ValidateArguments(string code, IEnumerable<TestCase> testCases)
    {
        var errors = new HashSet<string>();

        var requestTemplate = ParseToExecuteRequest(code);

        foreach (var tc in testCases)
        {
            if (string.IsNullOrWhiteSpace(tc.InputJson))
            {
                errors.Add($"TestCase {tc.Id}: No input provided.");
                continue;
            }

            Dictionary<string, JsonElement>? inputDict;
            try
            {
                inputDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tc.InputJson);
                if (inputDict is null)
                    throw new JsonException("JSON is null.");
            }
            catch (JsonException)
            {
                errors.Add($"TestCase {tc.Id}: Failed to parse InputJson. Expected a JSON object like {{ \"a\": 1, \"b\": 2 }}.");
                continue;
            }

            foreach (var arg in requestTemplate.Args)
            {
                if (!inputDict.ContainsKey(arg.Name))
                    errors.Add($"TestCase {tc.Id}: Missing argument '{arg.Name}' in InputJson.");
            }
        }

        if (errors.Count != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
    }
}
