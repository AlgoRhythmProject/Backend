using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Achievements.Interfaces;
using AlgoRhythm.Services.Achievements.Interfaces;
using AlgoRhythm.Shared.Dtos.Achievements;
using AlgoRhythm.Shared.Models.Achievements;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlgoRhythm.Services.Achievements;

public class AchievementService : IAchievementService
{
    private readonly IAchievementRepository _achievementRepo;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AchievementService> _logger;

    public AchievementService(
        IAchievementRepository achievementRepo,
        ApplicationDbContext context,
        ILogger<AchievementService> logger)
    {
        _achievementRepo = achievementRepo;
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<AchievementDto>> GetAllAchievementsAsync(CancellationToken ct = default)
    {
        var achievements = await _achievementRepo.GetAllAsync(ct);
        return achievements.Select(MapToDto);
    }

    public async Task<AchievementDto?> GetAchievementByIdAsync(Guid id, CancellationToken ct = default)
    {
        var achievement = await _achievementRepo.GetByIdAsync(id, ct);
        return achievement == null ? null : MapToDto(achievement);
    }

    public async Task<IEnumerable<UserAchievementDto>> GetUserAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        var userAchievements = await _achievementRepo.GetUserAchievementsAsync(userId, ct);
        return userAchievements.Select(MapToUserAchievementDto);
    }

    public async Task<UserAchievementDto?> GetUserAchievementAsync(Guid userId, Guid achievementId, CancellationToken ct = default)
    {
        var userAchievement = await _achievementRepo.GetUserAchievementAsync(userId, achievementId, ct);
        return userAchievement == null ? null : MapToUserAchievementDto(userAchievement);
    }

    public async Task InitializeAchievementsForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var allAchievements = await _achievementRepo.GetAllAsync(ct);
        var existingUserAchievements = await _achievementRepo.GetUserAchievementsAsync(userId, ct);
        var existingAchievementIds = existingUserAchievements.Select(ua => ua.AchievementId).ToHashSet();

        foreach (var achievement in allAchievements)
        {
            if (!existingAchievementIds.Contains(achievement.Id))
            {
                var userAchievement = new UserAchievement
                {
                    UserId = userId,
                    AchievementId = achievement.Id,
                    IsCompleted = false
                };

                var createdUserAchievement = await _achievementRepo.CreateUserAchievementAsync(userAchievement, ct);

                // Create progress tracking for each requirement
                foreach (var requirement in achievement.Requirements)
                {
                    var progress = new UserRequirementProgress
                    {
                        UserAchievementId = createdUserAchievement.Id,
                        RequirementId = requirement.Id,
                        ProgressValue = 0,
                        IsSatisfied = false
                    };

                    await _achievementRepo.CreateRequirementProgressAsync(progress, ct);
                }

                _logger.LogInformation("Initialized achievement {AchievementId} for user {UserId}", achievement.Id, userId);
            }
        }
    }

    public async Task CheckAndUpdateAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users
            .Include(u => u.CompletedLectures)
            .Include(u => u.CompletedTasks)
            .Include(u => u.CourseProgresses)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return;
        }

        var completedTasksCount = user.CompletedTasks.Count;
        var completedLecturesCount = user.CompletedLectures.Count;
        var completedCoursesCount = user.CourseProgresses.Count(cp => cp.Percentage == 100);

        var userAchievements = await _achievementRepo.GetUserAchievementsAsync(userId, ct);

        foreach (var userAchievement in userAchievements)
        {
            bool hasChanges = false;
            bool allRequirementsSatisfied = true;

            foreach (var requirementProgress in userAchievement.RequirementProgresses)
            {
                var requirement = requirementProgress.Requirement;
                var condition = requirement.Condition;

                if (condition == null) continue;

                int currentValue = 0;

                switch (condition.Type)
                {
                    case RequirementType.CompleteTasks:
                        currentValue = completedTasksCount;
                        break;

                    case RequirementType.CompleteLectures:
                        currentValue = completedLecturesCount;
                        break;

                    case RequirementType.CompleteCourses:
                        currentValue = completedCoursesCount;
                        break;

                    case RequirementType.CompleteSpecificCourse:
                        if (condition.TargetId.HasValue)
                        {
                            var courseProgress = user.CourseProgresses
                                .FirstOrDefault(cp => cp.CourseId == condition.TargetId.Value);
                            currentValue = courseProgress?.Percentage == 100 ? 1 : 0;
                        }
                        break;

                    case RequirementType.CompleteSpecificTask:
                        if (condition.TargetId.HasValue)
                        {
                            currentValue = user.CompletedTasks
                                .Any(t => t.Id == condition.TargetId.Value) ? 1 : 0;
                        }
                        break;

                    case RequirementType.CompleteSpecificLecture:
                        if (condition.TargetId.HasValue)
                        {
                            currentValue = user.CompletedLectures
                                .Any(l => l.Id == condition.TargetId.Value) ? 1 : 0;
                        }
                        break;
                }

                bool isSatisfied = currentValue >= condition.TargetValue;

                if (requirementProgress.ProgressValue != currentValue || 
                    requirementProgress.IsSatisfied != isSatisfied)
                {
                    requirementProgress.ProgressValue = currentValue;
                    requirementProgress.IsSatisfied = isSatisfied;
                    await _achievementRepo.UpdateRequirementProgressAsync(requirementProgress, ct);
                    hasChanges = true;
                }

                if (!isSatisfied)
                {
                    allRequirementsSatisfied = false;
                }
            }

            // Update achievement completion status
            if (allRequirementsSatisfied && !userAchievement.IsCompleted)
            {
                userAchievement.IsCompleted = true;
                userAchievement.EarnedAt = DateTime.UtcNow;
                await _achievementRepo.UpdateUserAchievementAsync(userAchievement, ct);
                
                _logger.LogInformation("User {UserId} earned achievement {AchievementName}", 
                    userId, userAchievement.Achievement.Name);
            }
        }
    }

    private AchievementDto MapToDto(Achievement achievement)
    {
        return new AchievementDto
        {
            Id = achievement.Id,
            Name = achievement.Name,
            Description = achievement.Description,
            IconPath = achievement.IconPath,
            Requirements = achievement.Requirements.Select(r => new RequirementDto
            {
                Id = r.Id,
                Description = r.Description,
                Type = r.Condition?.Type.ToString() ?? "Unknown",
                TargetValue = r.Condition?.TargetValue ?? 0,
                TargetId = r.Condition?.TargetId
            }).ToList()
        };
    }

    private UserAchievementDto MapToUserAchievementDto(UserAchievement userAchievement)
    {
        var requirementProgresses = userAchievement.RequirementProgresses.Select(rp =>
        {
            var targetValue = rp.Requirement.Condition?.TargetValue ?? 1;
            var percentage = targetValue > 0 
                ? Math.Min(100, (double)rp.ProgressValue / targetValue * 100) 
                : 0;

            return new RequirementProgressDto
            {
                RequirementId = rp.RequirementId,
                Description = rp.Requirement.Description,
                CurrentValue = rp.ProgressValue,
                TargetValue = targetValue,
                IsSatisfied = rp.IsSatisfied,
                ProgressPercentage = Math.Round(percentage, 2)
            };
        }).ToList();

        var overallProgress = requirementProgresses.Any()
            ? requirementProgresses.Average(rp => rp.ProgressPercentage)
            : 0;

        return new UserAchievementDto
        {
            Id = userAchievement.Id,
            UserId = userAchievement.UserId,
            AchievementId = userAchievement.AchievementId,
            AchievementName = userAchievement.Achievement.Name,
            AchievementDescription = userAchievement.Achievement.Description,
            IconPath = userAchievement.Achievement.IconPath,
            EarnedAt = userAchievement.EarnedAt,
            IsCompleted = userAchievement.IsCompleted,
            ProgressPercentage = Math.Round(overallProgress, 2),
            RequirementProgresses = requirementProgresses
        };
    }
}