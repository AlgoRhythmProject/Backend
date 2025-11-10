using System.ComponentModel.DataAnnotations;
using AlgoRhythm.Models.Courses;
using AlgoRhythm.Models.Tasks;

namespace AlgoRhythm.Models.Common;

public class Tag
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
}