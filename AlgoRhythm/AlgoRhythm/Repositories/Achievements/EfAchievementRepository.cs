using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Achievements.Interfaces;
using AlgoRhythm.Shared.Models.Achievements;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories.Achievements;

public class EfAchievementRepository : IAchievementRepository
{
    private readonly ApplicationDbContext _context;

    public EfAchievementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Achievement>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Achievements
            .Include(a => a.Requirements)
            .ToListAsync(ct);
    }

    public async Task<Achievement?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Achievements
            .Include(a => a.Requirements)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<Achievement> CreateAsync(Achievement achievement, CancellationToken ct = default)
    {
        await _context.Achievements.AddAsync(achievement, ct);
        await _context.SaveChangesAsync(ct);
        return achievement;
    }

    public async Task UpdateAsync(Achievement achievement, CancellationToken ct = default)
    {
        _context.Achievements.Update(achievement);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var achievement = await _context.Achievements.FindAsync(new object[] { id }, ct);
        if (achievement != null)
        {
            _context.Achievements.Remove(achievement);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<IEnumerable<UserAchievement>> GetUserAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.UserAchievements
            .Include(ua => ua.Achievement)
                .ThenInclude(a => a.Requirements)
            .Include(ua => ua.RequirementProgresses)
                .ThenInclude(rp => rp.Requirement)
            .Where(ua => ua.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task<UserAchievement?> GetUserAchievementAsync(Guid userId, Guid achievementId, CancellationToken ct = default)
    {
        return await _context.UserAchievements
            .Include(ua => ua.Achievement)
                .ThenInclude(a => a.Requirements)
            .Include(ua => ua.RequirementProgresses)
                .ThenInclude(rp => rp.Requirement)
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId, ct);
    }

    public async Task<UserAchievement> CreateUserAchievementAsync(UserAchievement userAchievement, CancellationToken ct = default)
    {
        await _context.UserAchievements.AddAsync(userAchievement, ct);
        await _context.SaveChangesAsync(ct);
        return userAchievement;
    }

    public async Task UpdateUserAchievementAsync(UserAchievement userAchievement, CancellationToken ct = default)
    {
        _context.UserAchievements.Update(userAchievement);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<UserRequirementProgress?> GetRequirementProgressAsync(Guid userAchievementId, Guid requirementId, CancellationToken ct = default)
    {
        return await _context.UserRequirementProgresses
            .Include(rp => rp.Requirement)
            .FirstOrDefaultAsync(rp => rp.UserAchievementId == userAchievementId && rp.RequirementId == requirementId, ct);
    }

    public async Task<UserRequirementProgress> CreateRequirementProgressAsync(UserRequirementProgress progress, CancellationToken ct = default)
    {
        await _context.UserRequirementProgresses.AddAsync(progress, ct);
        await _context.SaveChangesAsync(ct);
        return progress;
    }

    public async Task UpdateRequirementProgressAsync(UserRequirementProgress progress, CancellationToken ct = default)
    {
        _context.UserRequirementProgresses.Update(progress);
        await _context.SaveChangesAsync(ct);
    }
}