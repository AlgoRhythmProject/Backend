namespace AlgoRhythm.Shared.Dtos.Submissions;

public class SubmitProgrammingRequestDto
{
    public Guid TaskId { get; set; }
    public string Code { get; set; } = null!;
}
