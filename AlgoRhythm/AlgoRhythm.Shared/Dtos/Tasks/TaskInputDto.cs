using AlgoRhythm.Shared.Models.Tasks;
using System.ComponentModel.DataAnnotations;

namespace AlgoRhythm.Shared.Dtos.Tasks;

public class TaskInputDto
{
    [Required]
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }
    
    [Required]
    public Difficulty Difficulty { get; set; } // "Easy", "Medium", "Hard"
    
    [Required]
    public TaskType TaskType { get; set; } // "Programming" or "Interactive"
    
    public bool IsPublished { get; set; }
    
    // For ProgrammingTaskItem
    public string? TemplateCode { get; set; }
    
    // For InteractiveTaskItem
    public string? OptionsJson { get; set; }
    public string? CorrectAnswer { get; set; }
}
