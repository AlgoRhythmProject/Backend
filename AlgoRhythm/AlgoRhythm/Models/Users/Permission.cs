using System.ComponentModel.DataAnnotations;

namespace AlgoRhythm.Models.Users;

public class Permission
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public ICollection<Role> Roles { get; set; } = new List<Role>();
}