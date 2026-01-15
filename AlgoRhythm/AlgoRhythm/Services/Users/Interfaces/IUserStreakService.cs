using AlgoRhythm.Shared.Dtos.Users;

namespace AlgoRhythm.Services.Users.Interfaces;

public interface IUserStreakService
{
    Task UpdateLoginStreakAsync(Guid userId, CancellationToken ct = default);
    Task<UserStreakDto> GetUserStreakAsync(Guid userId, CancellationToken ct = default);
}