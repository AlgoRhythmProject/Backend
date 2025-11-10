using System.ComponentModel.DataAnnotations;
using AlgoRhythm.Models.Achievements;
using AlgoRhythm.Models.Common;
using AlgoRhythm.Models.Courses;
using AlgoRhythm.Models.Submissions;

namespace AlgoRhythm.Models.Users;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string PasswordHash { get; set; } = null!;

    [Required]
    public string FirstName { get; set; } = null!;

    [Required]
    public string LastName { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Email verification (temporary — usuwane po weryfikacji)
    public string? EmailVerificationCode { get; set; }
    public DateTime? EmailVerificationExpiryUtc { get; set; }
    public bool IsEmailConfirmed { get; set; } = false;

    // Navigation properties
    public UserPreferences? Preferences { get; set; }
    
    // Direct many-to-many relationships
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<UserAchievement> Achievements { get; set; } = new List<UserAchievement>();
    public ICollection<CourseProgress> CourseProgresses { get; set; } = new List<CourseProgress>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}