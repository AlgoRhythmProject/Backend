namespace AlgoRhythm.Shared.Dtos.Tasks;

public class UpdateTestCaseDto
{
    public string? InputJson { get; set; }
    public string? ExpectedJson { get; set; }
    public bool IsVisible { get; set; }
    public int MaxPoints { get; set; }
}