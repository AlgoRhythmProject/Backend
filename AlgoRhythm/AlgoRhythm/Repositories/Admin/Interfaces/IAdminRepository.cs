using AlgoRhythm.Shared.Models.Users;

namespace AlgoRhythm.Repositories.Admin.Interfaces;

public interface IAdminRepository
{
    Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken ct);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct);
    Task<IList<string>> GetUserRolesAsync(User user);
    Task<bool> AddUserToRoleAsync(User user, string roleName);
    Task<bool> RemoveUserFromRoleAsync(User user, string roleName);
    Task<bool> IsInRoleAsync(User user, string roleName);
}