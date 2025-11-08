using System.ComponentModel.DataAnnotations;

namespace AlgoRhythm.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string PasswordHash { get; set; } = null!;

    public bool IsEmailConfirmed { get; set; } = false;

    // Email verification
    public string? EmailVerificationCode { get; set; }
    public DateTime? EmailVerificationExpiryUtc { get; set; }

    public string? Role { get; set; }
}