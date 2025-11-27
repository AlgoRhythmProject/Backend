using AlgoRhythm.Shared.Dtos;

namespace AlgoRhythm.Shared.Dtos.Tasks;

public class InteractiveTaskInputDto : TaskInputDto
{
    public string? OptionsJson { get; set; }
    public string? CorrectAnswer { get; set; }
}