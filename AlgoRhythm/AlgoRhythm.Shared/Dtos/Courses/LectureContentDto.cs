namespace AlgoRhythm.Shared.Dtos.Courses;

public class LectureContentDto
{
    public Guid Id { get; set; }
    public required string Type { get; set; }
    public string? Text { get; set; }
    public string? Path { get; set; }
    public string? Alt { get; set; }
    public string? Title { get; set; }
}