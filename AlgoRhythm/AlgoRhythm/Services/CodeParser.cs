using System.Text.RegularExpressions;

public class CSharpCodeParser : ICodeParser
{
    public ParsedFunction Parse(string code)
    {
        // parsing: return type, name, args, body
        var match = Regex.Match(code,
            @"(?<return>[^\s]+)\s+(?<name>\w+)\((?<args>[^\)]*)\)\s*\{(?<body>[\s\S]*)\}");

        if (!match.Success)
            throw new InvalidOperationException("Invalid function code format.");

        var args = match.Groups["args"].Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(a =>
            {
                var parts = a.Trim().Split(' ');
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
            Arguments = args,
            Body = match.Groups["body"].Value.Trim()
        };
    }
}
