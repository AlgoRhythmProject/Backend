using AlgoRhythm.Shared.Models.Courses;

namespace AlgoRhythm.Repositories.Courses.Interfaces;

public interface ICourseProgressRepository
{
    Task<IEnumerable<CourseProgress>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<CourseProgress?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task<CourseProgress?> GetByIdAsync(Guid id, CancellationToken ct);
    Task CreateAsync(CourseProgress progress, CancellationToken ct);
    Task UpdateAsync(CourseProgress progress, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<HashSet<Guid>> GetCompletedLectureIdsAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task<HashSet<Guid>> GetCompletedTaskIdsAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task<bool> IsLectureCompletedAsync(Guid userId, Guid lectureId, CancellationToken ct);
    Task<HashSet<Guid>> GetAllCompletedLectureIdsAsync(Guid userId, CancellationToken ct);
    Task<HashSet<Guid>> GetAllCompletedTaskIdsAsync(Guid userId, CancellationToken ct);
}