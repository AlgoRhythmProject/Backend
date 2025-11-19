using AlgoRhythm.Shared.Models.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;

public class CSharpCodeParser : ICodeParser
{
    public ParsedFunction Parse(string code)
    {
        try
        {
            var match = Regex.Match(code,
                @"^(?<modifiers>(?:\w+\s+)*)          
              (?<return>[^\s]+)\s+                
              (?<name>\w+)\s*                    
              \((?<args>[^)]*)\)\s*            
              \{(?<body>[\s\S]*)\}\s*$",
                RegexOptions.IgnorePatternWhitespace);

            if (!match.Success)
                throw new FormatException(
                    "Invalid function format. Expected something like: " +
                    "'public int Add(int a, int b) { ... }'");

            var argsList = match.Groups["args"].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(a =>
                {
                    var parts = a.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                        throw new FormatException(
                            $"Invalid argument format: '{a}'. Expected 'Type Name'.");

                    return new FunctionArgument
                    {
                        Type = parts[0],
                        Name = parts[1]
                    };
                }).ToList();

            return new ParsedFunction
            {
                ReturnType = match.Groups["return"].Value,
                FunctionName = match.Groups["name"].Value,
                Arguments = argsList,
                Body = match.Groups["body"].Value.Trim()
            };
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Function parsing error: {ex.Message}");
        }
    }
    public void ValidateArguments(ParsedFunction parsedFunction, IEnumerable<TestCase> testCases)
    {
        var errors = new HashSet<string>();

        foreach (var tc in testCases)
        {
            if (string.IsNullOrWhiteSpace(tc.InputJson))
            {
                errors.Add("No input provided.");
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
                errors.Add("Failed to parse InputJson. Expected a JSON object like { \"a\": 1, \"b\": 2 }.");
                continue;
            }

            foreach (var arg in parsedFunction.Arguments)
            {
                if (!inputDict.ContainsKey(arg.Name))
                    errors.Add($"Missing argument '{arg.Name}' in InputJson.");
            }
        }

        if (errors.Any())
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
    }

}
