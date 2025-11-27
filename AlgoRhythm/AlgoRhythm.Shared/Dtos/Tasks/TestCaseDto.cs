namespace AlgoRhythm.Shared.Dtos.Tasks;

public class TestCaseDto
{
    public Guid Id { get; set; }
    public string? InputJson { get; set; }
    public string? ExpectedJson { get; set; }
    public int MaxPoints { get; set; }
    public bool IsVisible { get; set; }
}

