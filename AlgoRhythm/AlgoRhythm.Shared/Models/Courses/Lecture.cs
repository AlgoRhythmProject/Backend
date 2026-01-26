using System.ComponentModel.DataAnnotations;
using AlgoRhythm.Shared.Models.Common;

namespace AlgoRhythm.Shared.Models.Courses;

public class Lecture
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Title { get; set; } = null!;

    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Course> Courses { get; set; } = new List<Course>();
    
    public ICollection<LectureContent> Contents { get; set; } = new List<LectureContent>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}