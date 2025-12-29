using AlgoRhythm.Shared.Dtos.Achievements;

namespace AlgoRhythm.Services.Achievements.Interfaces;

public interface IAchievementService
{
    Task<IEnumerable<AchievementDto>> GetAllAchievementsAsync(CancellationToken ct = default);
    Task<AchievementDto?> GetAchievementByIdAsync(Guid id, CancellationToken ct = default);
    
    Task<IEnumerable<UserAchievementDto>> GetUserAchievementsAsync(Guid userId, CancellationToken ct = default);
    Task<UserAchievementDto?> GetUserAchievementAsync(Guid userId, Guid achievementId, CancellationToken ct = default);
    Task<IEnumerable<EarnedAchievementDto>> GetEarnedAchievementsAsync(Guid userId, CancellationToken ct = default);
    
    Task InitializeAchievementsForUserAsync(Guid userId, CancellationToken ct = default);
    Task CheckAndUpdateAchievementsAsync(Guid userId, CancellationToken ct = default);
    
    // Admin methods
    Task<AchievementDto> CreateAchievementAsync(CreateAchievementDto dto, CancellationToken ct = default);
    Task<AchievementDto?> UpdateAchievementAsync(Guid id, UpdateAchievementDto dto, CancellationToken ct = default);
    Task<bool> DeleteAchievementAsync(Guid id, CancellationToken ct = default);
}