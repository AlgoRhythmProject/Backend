using System.ComponentModel.DataAnnotations;

namespace AlgoRhythm.Models.Courses;

public class CourseTask
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CourseId { get; set; }

    [Required]
    public Guid TaskId { get; set; }

    public Course Course { get; set; } = null!;
    public Task Task { get; set; } = null!;
}