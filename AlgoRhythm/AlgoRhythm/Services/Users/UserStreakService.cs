using AlgoRhythm.Repositories.Users.Interfaces;
using AlgoRhythm.Services.Achievements.Interfaces;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Users;

namespace AlgoRhythm.Services.Users;

public class UserStreakService : IUserStreakService
{
    private readonly IUserStreakRepository _repo;
    private readonly IAchievementService _achievementService;
    private readonly ILogger<UserStreakService> _logger;

    public UserStreakService(
        IUserStreakRepository repo,
        IAchievementService achievementService,
        ILogger<UserStreakService> logger)
    {
        _repo = repo;
        _achievementService = achievementService;
        _logger = logger;
    }

    public async Task UpdateLoginStreakAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _repo.GetUserByIdAsync(userId, ct);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for streak update", userId);
            return;
        }

        var today = DateTime.UtcNow.Date;
        var lastLogin = user.LastLoginDate?.Date;

        if (lastLogin == today)
        {
            // Already logged in today, no update needed
            _logger.LogDebug("User {UserId} already logged in today", userId);
            return;
        }

        if (lastLogin == today.AddDays(-1))
        {
            // Logged in yesterday, increment streak
            user.CurrentStreak++;
            _logger.LogInformation("User {UserId} streak increased to {Streak}", userId, user.CurrentStreak);
        }
        else if (lastLogin < today.AddDays(-1) || lastLogin == null)
        {
            // Streak broken or first login
            user.CurrentStreak = 1;
            _logger.LogInformation("User {UserId} streak reset to 1", userId);
        }

        // Update longest streak if current is higher
        if (user.CurrentStreak > user.LongestStreak)
        {
            user.LongestStreak = user.CurrentStreak;
            _logger.LogInformation("User {UserId} new longest streak: {LongestStreak}", userId, user.LongestStreak);
        }

        user.LastLoginDate = today;
        await _repo.UpdateUserAsync(user, ct);

        // Check for streak achievements
        try
        {
            await _achievementService.CheckAndUpdateAchievementsAsync(userId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check achievements for user {UserId}", userId);
        }
    }

    public async Task<UserStreakDto> GetUserStreakAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _repo.GetUserByIdAsync(userId, ct);

        if (user == null)
            throw new KeyNotFoundException($"User {userId} not found");

        return new UserStreakDto
        {
            UserId = user.Id,
            CurrentStreak = user.CurrentStreak,
            LongestStreak = user.LongestStreak,
            LastLoginDate = user.LastLoginDate
        };
    }
}