using AlgoRhythm.Shared.Dtos;

namespace AlgoRhythm.Shared.Dtos.Tasks;

public class InteractiveTaskInputDto : TaskInputDto
{
    public new string? OptionsJson { get; set; }
    public new string? CorrectAnswer { get; set; }
}