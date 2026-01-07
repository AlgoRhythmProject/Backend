namespace AlgoRhythm.Shared.Dtos.Tasks;

public class CreateTestCaseDto
{
    public Guid ProgrammingTaskItemId { get; set; }
    public string? InputJson { get; set; }
    public string? ExpectedJson { get; set; }
    public bool IsVisible { get; set; } = true;
    public int MaxPoints { get; set; } = 10;
}