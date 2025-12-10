using System.ComponentModel.DataAnnotations;

namespace AlgoRhythm.Shared.Dtos.Courses;

public class LectureContentInputDto
{
    [Required]
    public string Type { get; set; } = null!; // "Text" or "Photo"

    // For Text content
    public string? HtmlContent { get; set; }

    // For Photo content
    public string? Path { get; set; }
    public string? Alt { get; set; }
    public string? Title { get; set; }
}