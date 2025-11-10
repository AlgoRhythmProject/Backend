using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlgoRhythm.Models.Courses;

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

    [ForeignKey(nameof(LectureId))]
    public Lecture Lecture { get; set; } = null!;
}

public class LectureText : LectureContent
{
    [Required]
    public string Text { get; set; } = null!;
}

public class LecturePhoto : LectureContent
{
    [Required]
    public string Path { get; set; } = null!;

    public string? Alt { get; set; }

    public string? Title { get; set; }
}