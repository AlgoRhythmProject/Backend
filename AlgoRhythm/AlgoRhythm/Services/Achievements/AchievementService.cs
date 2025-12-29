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

        // Update basic properties
        if (!string.IsNullOrWhiteSpace(dto.Name))
            achievement.Name = dto.Name;
        
        if (dto.Description != null)
            achievement.Description = dto.Description;
        
        if (dto.IconPath != null)
            achievement.IconPath = dto.IconPath;

        // Update requirements if provided
        if (dto.Requirements != null)
        {
            // Remove requirements marked for deletion
            var requirementsToDelete = dto.Requirements
                .Where(r => r.Id.HasValue && r.ShouldDelete)
                .Select(r => r.Id!.Value)
                .ToHashSet();

            achievement.Requirements = achievement.Requirements
                .Where(r => !requirementsToDelete.Contains(r.Id))
                .ToList();

            // Update existing or add new requirements
            foreach (var reqDto in dto.Requirements.Where(r => !r.ShouldDelete))
            {
                if (reqDto.Id.HasValue)
                {
                    // Update existing
                    var existing = achievement.Requirements.FirstOrDefault(r => r.Id == reqDto.Id.Value);
                    if (existing != null)
                    {
                        if (reqDto.Description != null)
                            existing.Description = reqDto.Description;
                        
                        if (reqDto.Type != null || reqDto.TargetValue.HasValue || reqDto.TargetId.HasValue)
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
                    }
                }
                else if (reqDto.Type != null)
                {
                    // Add new
                    var newRequirement = new Requirement
                    {
                        AchievementId = achievement.Id,
                        Description = reqDto.Description,
                        Condition = new RequirementCondition
                        {
                            Type = Enum.Parse<RequirementType>(reqDto.Type),
                            TargetValue = reqDto.TargetValue ?? 1,
                            TargetId = reqDto.TargetId
                        }
                    };
                    achievement.Requirements.Add(newRequirement);
                }
            }
        }

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