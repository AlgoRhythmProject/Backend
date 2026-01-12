using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Courses.Interfaces;
using AlgoRhythm.Services.Achievements.Interfaces;
using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using AlgoRhythm.Shared.Models.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlgoRhythm.Services.Courses;

public class CourseProgressService : ICourseProgressService
{
    private readonly ICourseProgressRepository _repo;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CourseProgressService> _logger;
    private readonly IAchievementService _achievementService;

    public CourseProgressService(
        ICourseProgressRepository repo, 
        ApplicationDbContext context,
        ILogger<CourseProgressService> logger,
        IAchievementService achievementService)
    {
        _repo = repo;
        _context = context;
        _logger = logger;
        _achievementService = achievementService;
    }

    public async Task<IEnumerable<CourseProgressDto>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var progresses = await _repo.GetByUserIdAsync(userId, ct);
        
        var result = new List<CourseProgressDto>();
        foreach (var progress in progresses)
        {
            result.Add(await MapToDtoAsync(progress, ct));
        }
        
        return result;
    }

    public async Task<CourseProgressDto?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        var progress = await _repo.GetByUserAndCourseAsync(userId, courseId, ct);
        return progress == null ? null : await MapToDtoAsync(progress, ct);
    }

    /// <summary>
    /// Inicjalizuje CourseProgress dla wszystkich kursów dla nowego użytkownika
    /// Wywoływane automatycznie przy tworzeniu konta
    /// </summary>
    public async Task InitializeAllCoursesForUserAsync(Guid userId, CancellationToken ct)
    {
        var allCourses = await _context.Courses.ToListAsync(ct);
        var existingProgresses = await _repo.GetByUserIdAsync(userId, ct);
        var existingCourseIds = existingProgresses.Select(p => p.CourseId).ToHashSet();

        foreach (var course in allCourses)
        {
            if (!existingCourseIds.Contains(course.Id))
            {
                var progress = new CourseProgress
                {
                    UserId = userId,
                    CourseId = course.Id,
                    Percentage = 0,
                    StartedAt = DateTime.UtcNow
                };

                await _repo.CreateAsync(progress, ct);
                
                _logger.LogInformation("Initialized course progress for course {CourseId} and user {UserId}", course.Id, userId);
            }
        }
    }

    public async Task<bool> ToggleLectureCompletionAsync(Guid userId, Guid lectureId, CancellationToken ct)
    {
        var user = await _context.Users
            .Include(u => u.CompletedLectures)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        var lecture = await _context.Lectures
            .FirstOrDefaultAsync(l => l.Id == lectureId, ct);

        if (lecture == null)
            throw new KeyNotFoundException("Lecture not found");

        bool isCompleted;
        
        if (user.CompletedLectures.Any(l => l.Id == lectureId))
        {
            // Mark as incomplete
            var completedLecture = user.CompletedLectures.First(l => l.Id == lectureId);
            user.CompletedLectures.Remove(completedLecture);
            isCompleted = false;
            _logger.LogInformation("User {UserId} marked lecture {LectureId} as incomplete", userId, lectureId);
        }
        else
        {
            // Mark as complete
            user.CompletedLectures.Add(lecture);
            isCompleted = true;
            _logger.LogInformation("User {UserId} completed lecture {LectureId}", userId, lectureId);
        }

        await _context.SaveChangesAsync(ct);

        // Check achievements after lecture completion change
        try
        {
            await _achievementService.CheckAndUpdateAchievementsAsync(userId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update achievements after lecture completion");
        }

        // Przelicz postęp kursu
        await RecalculateProgressAsync(userId, lecture.CourseId, ct);

        return isCompleted;
    }

    public async Task<bool> MarkLectureAsCompletedAsync(Guid userId, Guid lectureId, CancellationToken ct)
    {
        var user = await _context.Users
            .Include(u => u.CompletedLectures)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        var lecture = await _context.Lectures
            .FirstOrDefaultAsync(l => l.Id == lectureId, ct);

        if (lecture == null)
            throw new KeyNotFoundException("Lecture not found");

        if (user.CompletedLectures.Any(l => l.Id == lectureId))
            return false; // Already completed

        user.CompletedLectures.Add(lecture);
        await _context.SaveChangesAsync(ct);

        await RecalculateProgressAsync(userId, lecture.CourseId, ct);

        return true;
    }

    public async Task<bool> MarkLectureAsIncompletedAsync(Guid userId, Guid lectureId, CancellationToken ct)
    {
        var user = await _context.Users
            .Include(u => u.CompletedLectures)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            throw new KeyNotFoundException("User not found");

        var lecture = await _context.Lectures
            .FirstOrDefaultAsync(l => l.Id == lectureId, ct);

        if (lecture == null)
            throw new KeyNotFoundException("Lecture not found");

        var completedLecture = user.CompletedLectures.FirstOrDefault(l => l.Id == lectureId);
        if (completedLecture == null)
            return false; // Not completed

        user.CompletedLectures.Remove(completedLecture);
        await _context.SaveChangesAsync(ct);

        await RecalculateProgressAsync(userId, lecture.CourseId, ct);

        return true;
    }

    public async Task RecalculateProgressAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        var progress = await _repo.GetByUserAndCourseAsync(userId, courseId, ct);
        if (progress == null)
            return;

        var course = progress.Course;
        if (course == null)
            return;

        var totalLectures = course.Lectures.Count;
        var totalTasks = course.TaskItems.Count;
        var totalItems = totalLectures + totalTasks;

        if (totalItems == 0)
        {
            progress.Percentage = 0;
            await _repo.UpdateAsync(progress, ct);
            return;
        }

        var completedLectures = await _repo.GetCompletedLectureIdsAsync(userId, courseId, ct);
        var completedTasks = await _repo.GetCompletedTaskIdsAsync(userId, courseId, ct);

        var completedItems = completedLectures.Count + completedTasks.Count;
        var percentage = (int)Math.Round((double)completedItems / totalItems * 100);

        progress.Percentage = Math.Clamp(percentage, 0, 100);

        if (progress.Percentage == 100 && progress.CompletedAt == null)
        {
            progress.CompletedAt = DateTime.UtcNow;
            _logger.LogInformation("User {UserId} completed course {CourseId}", userId, courseId);
        }
        else if (progress.Percentage < 100)
        {
            progress.CompletedAt = null; // Reset completion if user uncompletes items
        }

        await _repo.UpdateAsync(progress, ct);
    }

    private async Task<CourseProgressDto> MapToDtoAsync(CourseProgress progress, CancellationToken ct)
    {
        var completedLectureIds = await _repo.GetCompletedLectureIdsAsync(progress.UserId, progress.CourseId, ct);
        var completedTaskIds = await _repo.GetCompletedTaskIdsAsync(progress.UserId, progress.CourseId, ct);

        var totalLectures = progress.Course?.Lectures.Count ?? 0;
        var totalTasks = progress.Course?.TaskItems.Count ?? 0;

        return new CourseProgressDto
        {
            Id = progress.Id,
            UserId = progress.UserId,
            CourseId = progress.CourseId,
            CourseName = progress.Course?.Name,
            Percentage = progress.Percentage,
            StartedAt = progress.StartedAt,
            CompletedAt = progress.CompletedAt,
            TotalLectures = totalLectures,
            CompletedLecturesCount = completedLectureIds.Count,
            CompletedLectureIds = completedLectureIds.ToList(),
            TotalTasks = totalTasks,
            CompletedTasksCount = completedTaskIds.Count,
            CompletedTaskIds = completedTaskIds.ToList()
        };
    }

    public async Task<bool> IsLectureCompletedAsync(Guid userId, Guid lectureId, CancellationToken ct)
    {
        return await _repo.IsLectureCompletedAsync(userId, lectureId, ct);
    }

    public async Task<HashSet<Guid>> GetCompletedLectureIdsAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        return await _repo.GetCompletedLectureIdsAsync(userId, courseId, ct);
    }

    public async Task<HashSet<Guid>> GetCompletedTaskIdsAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        return await _repo.GetCompletedTaskIdsAsync(userId, courseId, ct);
    }

    public async Task<UserCompletedLecturesDto> GetAllCompletedLecturesAsync(Guid userId, CancellationToken ct)
    {
        var completedLectureIds = await _repo.GetAllCompletedLectureIdsAsync(userId, ct);

        return new UserCompletedLecturesDto
        {
            UserId = userId,
            CompletedLectureIds = completedLectureIds.ToList(),
            TotalCompleted = completedLectureIds.Count
        };
    }

    public async Task<UserCompletedTasksDto> GetAllCompletedTasksAsync(Guid userId, CancellationToken ct)
    {
        var completedTaskIds = await _repo.GetAllCompletedTaskIdsAsync(userId, ct);

        return new UserCompletedTasksDto
        {
            UserId = userId,
            CompletedTaskIds = completedTaskIds.ToList(),
            TotalCompleted = completedTaskIds.Count
        };
    }
}