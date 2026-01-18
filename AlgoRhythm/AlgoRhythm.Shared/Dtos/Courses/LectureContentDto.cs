using System.ComponentModel.DataAnnotations;

namespace AlgoRhythm.Shared.Dtos.Courses;

public class LectureContentDto
{
    public Guid Id { get; set; }

    [Required]
    public Guid LectureId { get; set; }

    [Required]
    public string Type { get; set; } = null!; // "Text" or "Photo"

    public int Order { get; set; }

    public DateTime CreatedAt { get; set; }

    // For Text content
    public string? HtmlContent { get; set; }

    // For Photo content
    public string? Path { get; set; }
    public string? Alt { get; set; }
    public string? Title { get; set; }

    // For Video content
    public string? FileName { get; set; }
    public string? StreamUrl { get; set; }
    public long? FileSize { get; set; }
    public DateTime? LastModified { get; set; }
}