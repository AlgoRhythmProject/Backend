using AlgoRhythm.Shared.Dtos.Admin;

namespace AlgoRhythm.Services.Admin.Interfaces;

public interface IAdminService
{
    Task<IEnumerable<UserWithRolesDto>> GetAllUsersAsync(CancellationToken ct);
    Task<UserWithRolesDto> GetUserWithRolesAsync(Guid userId, CancellationToken ct);
    Task AssignAdminRoleAsync(Guid userId, CancellationToken ct);
    Task RevokeAdminRoleAsync(Guid userId, CancellationToken ct);
}