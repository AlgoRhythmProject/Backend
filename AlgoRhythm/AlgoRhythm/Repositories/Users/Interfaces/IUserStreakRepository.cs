using AlgoRhythm.Shared.Models.Users;

namespace AlgoRhythm.Repositories.Users.Interfaces;

public interface IUserStreakRepository
{
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);
    Task UpdateUserAsync(User user, CancellationToken ct = default);
}