public class ParsedFunction
{
    public string ReturnType { get; set; } = null!;
    public string FunctionName { get; set; } = null!;
    public List<FunctionArgument> Arguments { get; set; } = new();
    public string Body { get; set; } = null!;
}

public class FunctionArgument
{
    public string Type { get; set; } = null!;
    public string Name { get; set; } = null!;
}
