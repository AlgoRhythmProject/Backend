namespace AlgoRhythm.Shared.Dtos.Tasks;

public class TaskWithProgressDto : TaskDto
{
    public bool IsStarted { get; set; }
    public bool IsCompleted { get; set; }
    public double? BestScore { get; set; }
    public int AttemptsCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
}