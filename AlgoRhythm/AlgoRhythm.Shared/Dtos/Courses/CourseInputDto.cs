namespace AlgoRhythm.Shared.Dtos.Courses;

public class CourseInputDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsPublished { get; set; }
}