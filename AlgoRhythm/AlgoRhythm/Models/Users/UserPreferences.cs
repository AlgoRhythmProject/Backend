using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlgoRhythm.Models.Users;

public class UserPreferences
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    public bool IsDarkTheme { get; set; } = false;

    [Required]
    public string Language { get; set; } = "en";

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}