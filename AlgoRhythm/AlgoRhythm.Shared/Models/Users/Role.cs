using Microsoft.AspNetCore.Identity;

namespace AlgoRhythm.Shared.Models.Users;

/// <summary>
/// Custom role extending ASP.NET Core Identity role.
/// </summary>
public class Role : IdentityRole<Guid>
{
    public string? Description { get; set; }
    
    // Many-to-many with Permissions
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}