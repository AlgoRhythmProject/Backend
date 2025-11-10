using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AlgoRhythm.Models.Common;

namespace AlgoRhythm.Models.Courses;

public class Lecture
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CourseId { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;
    
    public ICollection<LectureContent> Contents { get; set; } = new List<LectureContent>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}