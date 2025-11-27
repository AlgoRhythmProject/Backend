namespace AlgoRhythm.Shared.Dtos.Tasks;

public class ProgrammingTaskDto : TaskDto
{
    public string? TemplateCode { get; set; }
    public List<TestCaseDto> TestCases { get; set; } = [];
}