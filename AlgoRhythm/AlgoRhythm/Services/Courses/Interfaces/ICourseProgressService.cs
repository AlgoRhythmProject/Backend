using AlgoRhythm.Shared.Dtos.Courses;

namespace AlgoRhythm.Services.Courses.Interfaces;

public interface ICourseProgressService
{
    Task<IEnumerable<CourseProgressDto>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<CourseProgressDto?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task InitializeAllCoursesForUserAsync(Guid userId, CancellationToken ct);
    Task InitializeCourseForAllUsersAsync(Guid courseId, CancellationToken ct);
    Task DeleteAllByCourseIdAsync(Guid courseId, CancellationToken ct);
    Task<bool> ToggleLectureCompletionAsync(Guid userId, Guid lectureId, CancellationToken ct);
    Task<bool> MarkLectureAsCompletedAsync(Guid userId, Guid lectureId, CancellationToken ct);
    Task<bool> MarkLectureAsIncompletedAsync(Guid userId, Guid lectureId, CancellationToken ct);
    Task RecalculateProgressAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task<bool> IsLectureCompletedAsync(Guid userId, Guid lectureId, CancellationToken ct);
    Task<HashSet<Guid>> GetCompletedLectureIdsAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task<HashSet<Guid>> GetCompletedTaskIdsAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task<UserCompletedLecturesDto> GetAllCompletedLecturesAsync(Guid userId, CancellationToken ct);
    Task<UserCompletedTasksDto> GetAllCompletedTasksAsync(Guid userId, CancellationToken ct);
}