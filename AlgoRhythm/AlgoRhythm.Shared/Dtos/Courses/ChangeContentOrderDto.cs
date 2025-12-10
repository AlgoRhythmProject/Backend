using System.ComponentModel.DataAnnotations;

namespace AlgoRhythm.Shared.Dtos.Courses;

public class ChangeContentOrderDto
{
    [Required]
    public Guid FirstContentId { get; set; }

    [Required]
    public Guid SecondContentId { get; set; }
}