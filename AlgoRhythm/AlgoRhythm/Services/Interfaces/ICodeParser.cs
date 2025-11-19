using AlgoRhythm.Shared.Models.Tasks;

public interface ICodeParser
{
    ParsedFunction Parse(string code);

    void ValidateArguments(ParsedFunction parsedFunction, IEnumerable<TestCase> testCases);
}
