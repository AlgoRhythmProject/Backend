using System.ComponentModel.DataAnnotations;
using AlgoRhythm.Models.Tasks;

namespace AlgoRhythm.Models.Courses;

public class Course
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Direct many-to-many relationships
    public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
    public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    public ICollection<CourseProgress> CourseProgresses { get; set; } = new List<CourseProgress>();
}