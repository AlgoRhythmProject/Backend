using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

using AlgoRhythm.Shared.Models.Achievements;
using AlgoRhythm.Shared.Models.Common;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Submissions;

namespace AlgoRhythm.Shared.Models.Users;

/// <summary>
/// Application user extending ASP.NET Core Identity.
/// </summary>
public class User : IdentityUser<Guid>
{
    [PersonalData]
    public string FirstName { get; set; } = null!;

    [PersonalData]
    public string LastName { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public UserPreferences? Preferences { get; set; }
    public ICollection<UserAchievement> Achievements { get; set; } = new List<UserAchievement>();
    public ICollection<CourseProgress> CourseProgresses { get; set; } = new List<CourseProgress>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}