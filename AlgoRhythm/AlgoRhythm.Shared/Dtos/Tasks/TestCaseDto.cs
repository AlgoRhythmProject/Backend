namespace AlgoRhythm.Shared.Dtos.Tasks;

public class TestCaseDto
{
    public Guid Id { get; set; }
    public Guid ProgrammingTaskItemId { get; set; }
    public string? InputJson { get; set; }
    public string? ExpectedJson { get; set; }
    public bool IsVisible { get; set; }
    public int MaxPoints { get; set; }
    public TimeSpan? Timeout { get; set; }
}

