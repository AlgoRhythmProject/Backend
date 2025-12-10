namespace AlgoRhythm.Shared.Dtos.Tasks;

public class InteractiveTaskDto : TaskDto
{
    public string? OptionsJson { get; set; }
    public string? CorrectAnswer { get; set; }
}