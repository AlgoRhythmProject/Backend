using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Interfaces;
using AlgoRhythm.Shared.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories;


public class EfUserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;

    public EfUserRepository(ApplicationDbContext db) => _db = db;

    public async Task<User?> GetUserAsync(Guid id, CancellationToken ct)
    {
        return await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }
}
