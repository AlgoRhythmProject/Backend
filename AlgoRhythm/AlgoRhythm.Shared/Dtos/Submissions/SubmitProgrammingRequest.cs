namespace AlgoRhythm.Api.Dtos;

public class SubmitProgrammingRequest
{
    public Guid TaskId { get; set; }
    public string Code { get; set; } = null!;
}
