using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlgoRhythm.Models.Tasks;

public class Hint
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TaskItemId { get; set; }

    [Required]
    public string Content { get; set; } = null!;

    public int Order { get; set; } = 0;

    [ForeignKey(nameof(TaskItemId))]
    public TaskItem TaskItem { get; set; } = null!;
}