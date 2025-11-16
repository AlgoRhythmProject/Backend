using AlgoRhythm.Shared.Models.Users;

namespace AlgoRhythm.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserAsync(Guid id, CancellationToken ct);
}