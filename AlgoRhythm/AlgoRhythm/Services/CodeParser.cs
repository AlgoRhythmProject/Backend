using System.Text.RegularExpressions;

public class CSharpCodeParser : ICodeParser
{
    public ParsedFunction Parse(string code)
    {

        var match = Regex.Match(code,
            @"^(?<modifiers>(?:\w+\s+)*)          
              (?<return>[^\s]+)\s+                
              (?<name>\w+)\s*                    
              \((?<args>[^)]*)\)\s*            
              \{(?<body>[\s\S]*)\}\s*$",          
            RegexOptions.IgnorePatternWhitespace);

        if (!match.Success)
            throw new InvalidOperationException("Invalid function code format.");

        var argsList = match.Groups["args"].Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(a =>
            {
                var parts = a.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    throw new InvalidOperationException($"Invalid argument format: '{a}'");

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
}
