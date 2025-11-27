namespace AlgoRhythm.Shared.Dtos.Tasks;

public class TaskStatisticsDto
{
    public Guid TaskId { get; set; }
    public string? TaskTitle { get; set; }
    public int TotalSubmissions { get; set; }
    public int SuccessfulSubmissions { get; set; }
    public double SuccessRate { get; set; }
    public double AverageScore { get; set; }
    public int UniqueUsers { get; set; }
}