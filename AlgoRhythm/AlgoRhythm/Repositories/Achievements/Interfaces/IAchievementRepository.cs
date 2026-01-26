using AlgoRhythm.Shared.Models.Achievements;

namespace AlgoRhythm.Repositories.Achievements.Interfaces;

public interface IAchievementRepository
{
    Task<IEnumerable<Achievement>> GetAllAsync(CancellationToken ct = default);
    Task<Achievement?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Achievement> CreateAsync(Achievement achievement, CancellationToken ct = default);
    Task UpdateAsync(Achievement achievement, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    
    Task<IEnumerable<UserAchievement>> GetUserAchievementsAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<UserAchievement>> GetEarnedUserAchievementsAsync(Guid userId, CancellationToken ct = default);
    Task<UserAchievement?> GetUserAchievementAsync(Guid userId, Guid achievementId, CancellationToken ct = default);
    Task<UserAchievement> CreateUserAchievementAsync(UserAchievement userAchievement, CancellationToken ct = default);
    Task UpdateUserAchievementAsync(UserAchievement userAchievement, CancellationToken ct = default);
    
    Task<UserRequirementProgress?> GetRequirementProgressAsync(Guid userAchievementId, Guid requirementId, CancellationToken ct = default);
    Task<UserRequirementProgress> CreateRequirementProgressAsync(UserRequirementProgress progress, CancellationToken ct = default);
    Task UpdateRequirementProgressAsync(UserRequirementProgress progress, CancellationToken ct = default);
}