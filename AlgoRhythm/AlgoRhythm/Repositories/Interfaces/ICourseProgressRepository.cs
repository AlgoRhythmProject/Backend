using AlgoRhythm.Shared.Models.Courses;

namespace AlgoRhythm.Repositories.Interfaces;

public interface ICourseProgressRepository
{
    Task<IEnumerable<CourseProgress>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<CourseProgress?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task<CourseProgress?> GetByIdAsync(Guid id, CancellationToken ct);
    Task CreateAsync(CourseProgress progress, CancellationToken ct);
    Task UpdateAsync(CourseProgress progress, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}