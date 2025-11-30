using AlgoRhythm.Shared.Dtos.Courses;

namespace AlgoRhythm.Services.Interfaces;

public interface ICourseProgressService
{
    Task<IEnumerable<CourseProgressDto>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<CourseProgressDto?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task<CourseProgressDto> StartCourseAsync(Guid userId, Guid courseId, CancellationToken ct);
    Task UpdateProgressAsync(Guid userId, Guid courseId, int percentage, CancellationToken ct);
    Task CompleteCourseAsync(Guid userId, Guid courseId, CancellationToken ct);
}