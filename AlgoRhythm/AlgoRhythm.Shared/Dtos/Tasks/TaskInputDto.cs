using System.ComponentModel.DataAnnotations;

namespace AlgoRhythm.Shared.Dtos.Tasks;

public class TaskInputDto
{
    [Required]
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }
    
    [Required]
    public string Difficulty { get; set; } = null!; // "Easy", "Medium", "Hard"
    
    [Required]
    public string TaskType { get; set; } = null!; // "Programming" or "Interactive"
    
    public bool IsPublished { get; set; }
    
    // For ProgrammingTaskItem
    public string? TemplateCode { get; set; }
    
    // For InteractiveTaskItem
    public string? OptionsJson { get; set; }
    public string? CorrectAnswer { get; set; }
}
