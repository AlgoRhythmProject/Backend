using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Achievements.Interfaces;
using AlgoRhythm.Services.Achievements.Interfaces;
using AlgoRhythm.Shared.Dtos.Achievements;
using AlgoRhythm.Shared.Models.Achievements;
using AlgoRhythm.Shared.Models.Users;
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
        var user = await LoadUserWithProgressDataAsync(userId, ct);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", userId);
            return;
        }

        var userStats = CalculateUserStats(user);
        var userAchievements = await _achievementRepo.GetUserAchievementsAsync(userId, ct);

        foreach (var userAchievement in userAchievements)
        {
            await ProcessUserAchievementAsync(userAchievement, user, userStats, ct);
        }
    }

    public async Task<IEnumerable<EarnedAchievementDto>> GetEarnedAchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        var earnedAchievements = await _achievementRepo.GetEarnedUserAchievementsAsync(userId, ct);

        return earnedAchievements.Select(ua => new EarnedAchievementDto
        {
            Id = ua.Achievement.Id,
            Name = ua.Achievement.Name,
            Description = ua.Achievement.Description,
            IconPath = ua.Achievement.IconPath
        });
    }

    public async Task<AchievementDto> CreateAchievementAsync(CreateAchievementDto dto, CancellationToken ct = default)
    {
        var achievement = new Achievement
        {
            Name = dto.Name,
            Description = dto.Description,
            IconPath = dto.IconPath,
            Requirements = dto.Requirements.Select(r => new Requirement
            {
                Description = r.Description,
                Condition = new RequirementCondition
                {
                    Type = Enum.Parse<RequirementType>(r.Type),
                    TargetValue = r.TargetValue,
                    TargetId = r.TargetId
                }
            }).ToList()
        };

        var created = await _achievementRepo.CreateAsync(achievement, ct);

        _logger.LogInformation("Created achievement {AchievementId}: {AchievementName}", created.Id, created.Name);

        return MapToDto(created);
    }

    public async Task<AchievementDto?> UpdateAchievementAsync(Guid id, UpdateAchievementDto dto, CancellationToken ct = default)
    {
        var achievement = await _achievementRepo.GetByIdAsync(id, ct);
        if (achievement == null)
            return null;

        UpdateAchievementProperties(achievement, dto);
        UpdateAchievementRequirements(achievement, dto);

        await _achievementRepo.UpdateAsync(achievement, ct);

        _logger.LogInformation("Updated achievement {AchievementId}: {AchievementName}", achievement.Id, achievement.Name);

        return MapToDto(achievement);
    }

    public async Task<bool> DeleteAchievementAsync(Guid id, CancellationToken ct = default)
    {
        var achievement = await _achievementRepo.GetByIdAsync(id, ct);
        if (achievement == null)
            return false;

        await _achievementRepo.DeleteAsync(id, ct);

        _logger.LogInformation("Deleted achievement {AchievementId}: {AchievementName}", id, achievement.Name);

        return true;
    }

    // Private helper methods to reduce cognitive complexity

    private async Task<User?> LoadUserWithProgressDataAsync(Guid userId, CancellationToken ct)
    {
        return await _context.Users
            .Include(u => u.CompletedLectures)
            .Include(u => u.CompletedTasks)
            .Include(u => u.CourseProgresses)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
    }

    private static UserCompletionStats CalculateUserStats(User user)
    {
        return new UserCompletionStats
        {
            CompletedTasksCount = user.CompletedTasks.Count,
            CompletedLecturesCount = user.CompletedLectures.Count,
            CompletedCoursesCount = user.CourseProgresses.Count(cp => cp.Percentage == 100)
        };
    }

    private async Task ProcessUserAchievementAsync(
        UserAchievement userAchievement,
        User user,
        UserCompletionStats stats,
        CancellationToken ct)
    {
        bool allRequirementsSatisfied = true;

        foreach (var requirementProgress in userAchievement.RequirementProgresses)
        {
            var isSatisfied = await UpdateRequirementProgressAsync(requirementProgress, user, stats, ct);

            if (!isSatisfied)
            {
                allRequirementsSatisfied = false;
            }
        }

        await UpdateAchievementCompletionStatusAsync(userAchievement, allRequirementsSatisfied, ct);
    }

    private async Task<bool> UpdateRequirementProgressAsync(
        UserRequirementProgress requirementProgress,
        User user,
        UserCompletionStats stats,
        CancellationToken ct)
    {
        var requirement = requirementProgress.Requirement;
        var condition = requirement.Condition;

        if (condition == null)
            return false;

        int currentValue = CalculateRequirementCurrentValue(condition, user, stats);
        bool isSatisfied = currentValue >= condition.TargetValue;

        if (HasProgressChanged(requirementProgress, currentValue, isSatisfied))
        {
            requirementProgress.ProgressValue = currentValue;
            requirementProgress.IsSatisfied = isSatisfied;
            await _achievementRepo.UpdateRequirementProgressAsync(requirementProgress, ct);
        }

        return isSatisfied;
    }

    private static int CalculateRequirementCurrentValue(
        RequirementCondition condition,
        User user,
        UserCompletionStats stats)
    {
        return condition.Type switch
        {
            RequirementType.CompleteTasks => stats.CompletedTasksCount,
            RequirementType.CompleteLectures => stats.CompletedLecturesCount,
            RequirementType.CompleteCourses => stats.CompletedCoursesCount,
            RequirementType.CompleteSpecificCourse => CheckSpecificCourseCompletion(condition, user),
            RequirementType.CompleteSpecificTask => CheckSpecificTaskCompletion(condition, user),
            RequirementType.CompleteSpecificLecture => CheckSpecificLectureCompletion(condition, user),
            _ => 0
        };
    }

    private static int CheckSpecificCourseCompletion(RequirementCondition condition, User user)
    {
        if (!condition.TargetId.HasValue)
            return 0;

        var courseProgress = user.CourseProgresses
            .FirstOrDefault(cp => cp.CourseId == condition.TargetId.Value);

        return courseProgress?.Percentage == 100 ? 1 : 0;
    }

    private static int CheckSpecificTaskCompletion(RequirementCondition condition, User user)
    {
        if (!condition.TargetId.HasValue)
            return 0;

        return user.CompletedTasks.Any(t => t.Id == condition.TargetId.Value) ? 1 : 0;
    }

    private static int CheckSpecificLectureCompletion(RequirementCondition condition, User user)
    {
        if (!condition.TargetId.HasValue)
            return 0;

        return user.CompletedLectures.Any(l => l.Id == condition.TargetId.Value) ? 1 : 0;
    }

    private static bool HasProgressChanged(UserRequirementProgress progress, int currentValue, bool isSatisfied)
    {
        return progress.ProgressValue != currentValue || progress.IsSatisfied != isSatisfied;
    }

    private async Task UpdateAchievementCompletionStatusAsync(
        UserAchievement userAchievement,
        bool allRequirementsSatisfied,
        CancellationToken ct)
    {
        if (ShouldMarkAsCompleted(userAchievement, allRequirementsSatisfied))
        {
            userAchievement.IsCompleted = true;
            userAchievement.EarnedAt = DateTime.UtcNow;
            await _achievementRepo.UpdateUserAchievementAsync(userAchievement, ct);

            _logger.LogInformation(
                "User {UserId} earned achievement {AchievementName}",
                userAchievement.UserId,
                userAchievement.Achievement.Name);
        }
    }

    private static bool ShouldMarkAsCompleted(UserAchievement userAchievement, bool allRequirementsSatisfied)
    {
        return allRequirementsSatisfied && !userAchievement.IsCompleted;
    }

    private static void UpdateAchievementProperties(Achievement achievement, UpdateAchievementDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.Name))
            achievement.Name = dto.Name;

        if (dto.Description != null)
            achievement.Description = dto.Description;

        if (dto.IconPath != null)
            achievement.IconPath = dto.IconPath;
    }

    private static void UpdateAchievementRequirements(Achievement achievement, UpdateAchievementDto dto)
    {
        if (dto.Requirements == null)
            return;

        RemoveDeletedRequirements(achievement, dto.Requirements);
        UpdateOrAddRequirements(achievement, dto.Requirements);
    }

    private static void RemoveDeletedRequirements(Achievement achievement, List<UpdateRequirementDto> requirements)
    {
        var requirementsToDelete = requirements
            .Where(r => r.Id.HasValue && r.ShouldDelete)
            .Select(r => r.Id!.Value)
            .ToHashSet();

        achievement.Requirements = achievement.Requirements
            .Where(r => !requirementsToDelete.Contains(r.Id))
            .ToList();
    }

    private static void UpdateOrAddRequirements(Achievement achievement, List<UpdateRequirementDto> requirements)
    {
        foreach (var reqDto in requirements.Where(r => !r.ShouldDelete))
        {
            if (reqDto.Id.HasValue)
            {
                UpdateExistingRequirement(achievement, reqDto);
            }
            else if (reqDto.Type != null)
            {
                AddNewRequirement(achievement, reqDto);
            }
        }
    }

    private static void UpdateExistingRequirement(Achievement achievement, UpdateRequirementDto reqDto)
    {
        var existing = achievement.Requirements.FirstOrDefault(r => r.Id == reqDto.Id!.Value);
        if (existing == null)
            return;

        if (reqDto.Description != null)
            existing.Description = reqDto.Description;

        if (reqDto.Type != null || reqDto.TargetValue.HasValue || reqDto.TargetId.HasValue)
        {
            UpdateRequirementCondition(existing, reqDto);
        }
    }

    private static void UpdateRequirementCondition(Requirement existing, UpdateRequirementDto reqDto)
    {
        var condition = existing.Condition ?? new RequirementCondition();

        if (reqDto.Type != null)
            condition.Type = Enum.Parse<RequirementType>(reqDto.Type);

        if (reqDto.TargetValue.HasValue)
            condition.TargetValue = reqDto.TargetValue.Value;

        if (reqDto.TargetId.HasValue)
            condition.TargetId = reqDto.TargetId;

        existing.Condition = condition;
    }

    private static void AddNewRequirement(Achievement achievement, UpdateRequirementDto reqDto)
    {
        var newRequirement = new Requirement
        {
            AchievementId = achievement.Id,
            Description = reqDto.Description,
            Condition = new RequirementCondition
            {
                Type = Enum.Parse<RequirementType>(reqDto.Type!),
                TargetValue = reqDto.TargetValue ?? 1,
                TargetId = reqDto.TargetId
            }
        };
        achievement.Requirements.Add(newRequirement);
    }

    private static AchievementDto MapToDto(Achievement achievement)
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

    private static UserAchievementDto MapToUserAchievementDto(UserAchievement userAchievement)
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

        var overallProgress = requirementProgresses.Count != 0
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

    // Helper class for user statistics
    private sealed class UserCompletionStats
    {
        public int CompletedTasksCount { get; set; }
        public int CompletedLecturesCount { get; set; }
        public int CompletedCoursesCount { get; set; }
    }
}