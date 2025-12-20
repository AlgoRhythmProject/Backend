using AlgoRhythm.Shared.Dtos.Courses;

namespace AlgoRhythm.Services.Courses.Interfaces;

public interface ICourseProgressService
{
    Task<IEnumerable<CourseProgressDto>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<CourseProgressDto?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task InitializeAllCoursesForUserAsync(Guid userId, CancellationToken ct);
    Task<bool> ToggleLectureCompletionAsync(Guid userId, Guid lectureId, CancellationToken ct);
    Task<bool> MarkLectureAsCompletedAsync(Guid userId, Guid lectureId, CancellationToken ct);
    Task<bool> MarkLectureAsIncompletedAsync(Guid userId, Guid lectureId, CancellationToken ct);
    Task RecalculateProgressAsync(Guid userId, Guid courseId, CancellationToken ct);
}