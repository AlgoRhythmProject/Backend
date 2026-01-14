namespace AlgoRhythm.Shared.Dtos.Courses;

public class LectureDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<LectureContentDto> Contents { get; set; } = new();
    public List<Guid> TagIds { get; set; } = new();
    public List<Guid> CourseIds { get; set; } = new(); // Lista kursów do których nale¿y lektura
}