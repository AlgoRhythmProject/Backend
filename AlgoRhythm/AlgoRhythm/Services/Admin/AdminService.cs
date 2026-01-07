using AlgoRhythm.Repositories.Admin.Interfaces;
using AlgoRhythm.Services.Admin.Interfaces;
using AlgoRhythm.Shared.Dtos.Admin;

namespace AlgoRhythm.Services.Admin;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _repo;
    private readonly ILogger<AdminService> _logger;

    public AdminService(IAdminRepository repo, ILogger<AdminService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IEnumerable<UserWithRolesDto>> GetAllUsersAsync(CancellationToken ct)
    {
        var users = await _repo.GetAllUsersAsync(ct);
        var userDtos = new List<UserWithRolesDto>();

        foreach (var user in users)
        {
            var roles = await _repo.GetUserRolesAsync(user);
            userDtos.Add(new UserWithRolesDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList()
            });
        }

        return userDtos;
    }

    public async Task<UserWithRolesDto> GetUserWithRolesAsync(Guid userId, CancellationToken ct)
    {
        var user = await _repo.GetUserByIdAsync(userId, ct);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {userId} not found");

        var roles = await _repo.GetUserRolesAsync(user);

        return new UserWithRolesDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles.ToList(),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task AssignAdminRoleAsync(Guid userId, CancellationToken ct)
    {
        var user = await _repo.GetUserByIdAsync(userId, ct);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {userId} not found");

        var isAlreadyAdmin = await _repo.IsInRoleAsync(user, "Admin");
        if (isAlreadyAdmin)
        {
            _logger.LogInformation("User {UserId} is already an Admin", userId);
            throw new InvalidOperationException("User already has Admin role");
        }

        var currentRoles = await _repo.GetUserRolesAsync(user);
        
        // remove all roles
        foreach (var role in currentRoles)
        {
            var removeResult = await _repo.RemoveUserFromRoleAsync(user, role);
            if (!removeResult)
            {
                _logger.LogWarning("Failed to remove role {Role} from user {UserId}", role, userId);
            }
            else
            {
                _logger.LogInformation("Removed role {Role} from user {UserId}", role, userId);
            }
        }

        // add admin role
        var result = await _repo.AddUserToRoleAsync(user, "Admin");
        if (!result)
            throw new InvalidOperationException("Failed to assign Admin role");

        _logger.LogInformation("Admin role assigned to user {UserId}. Previous roles removed: {Roles}", 
            userId, string.Join(", ", currentRoles));
    }

    public async Task RevokeAdminRoleAsync(Guid userId, CancellationToken ct)
    {
        var user = await _repo.GetUserByIdAsync(userId, ct);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {userId} not found");

        var isAdmin = await _repo.IsInRoleAsync(user, "Admin");
        if (!isAdmin)
        {
            _logger.LogInformation("User {UserId} is not an Admin", userId);
            throw new InvalidOperationException("User does not have Admin role");
        }

        // remove admin role
        var result = await _repo.RemoveUserFromRoleAsync(user, "Admin");
        if (!result)
            throw new InvalidOperationException("Failed to revoke Admin role");

        // Add user role
        var addUserResult = await _repo.AddUserToRoleAsync(user, "User");
        if (!addUserResult)
        {
            _logger.LogWarning("Failed to assign User role to user {UserId} after revoking Admin", userId);
        }

        _logger.LogInformation("Admin role revoked from user {UserId}. User role assigned.", userId);
    }

    public async Task<bool> IsUserAdminAsync(Guid userId, CancellationToken ct)
    {
        var user = await _repo.GetUserByIdAsync(userId, ct);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        return await _repo.IsInRoleAsync(user, "Admin");
    }
}