using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlgoRhythm.Shared.Models.Courses;

public enum ContentType
{
    Text,
    Photo
}

public abstract class LectureContent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid LectureId { get; set; }

    [Required]
    public ContentType Type { get; set; }

    public int Order { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(LectureId))]
    public Lecture Lecture { get; set; } = null!;
}

public class LectureText : LectureContent
{
    [Required]
    public string HtmlContent { get; set; } = null!;
}

public class LecturePhoto : LectureContent
{
    [Required]
    public string Path { get; set; } = null!;

    public string? Alt { get; set; }

    public string? Title { get; set; }
}