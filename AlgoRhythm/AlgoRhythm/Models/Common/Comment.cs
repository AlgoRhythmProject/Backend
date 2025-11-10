using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AlgoRhythm.Models.Tasks;
using AlgoRhythm.Models.Users;

namespace AlgoRhythm.Models.Common;

public class Comment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AuthorId { get; set; }

    [Required]
    public Guid TaskItemId { get; set; }

    [Required]
    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsEdited { get; set; } = false;

    public DateTime? EditedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    [ForeignKey(nameof(AuthorId))]
    public User Author { get; set; } = null!;
    
    [ForeignKey(nameof(TaskItemId))]
    public TaskItem TaskItem { get; set; } = null!;
}