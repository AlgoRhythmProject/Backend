using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Users.Interfaces;
using AlgoRhythm.Shared.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories.Users;

public class EfUserStreakRepository : IUserStreakRepository
{
    private readonly ApplicationDbContext _context;

    public EfUserStreakRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    public async Task UpdateUserAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
    }
}