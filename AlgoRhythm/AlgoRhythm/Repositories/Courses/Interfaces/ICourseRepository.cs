using AlgoRhythm.Shared.Models.Courses;

namespace AlgoRhythm.Repositories.Courses.Interfaces;

public interface ICourseRepository
{
    Task<IEnumerable<Course>> GetAllAsync(CancellationToken ct);
    Task<IEnumerable<Course>> GetPublishedAsync(CancellationToken ct);
    Task<Course?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Course?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct);
    Task CreateAsync(Course course, CancellationToken ct);
    Task UpdateAsync(Course course, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task AddTaskToCourseAsync(Guid courseId, Guid taskId, CancellationToken ct);
    Task RemoveTaskFromCourseAsync(Guid courseId, Guid taskId, CancellationToken ct);
    Task AddLectureToCourseAsync(Guid courseId, Guid lectureId, CancellationToken ct);
    Task RemoveLectureFromCourseAsync(Guid courseId, Guid lectureId, CancellationToken ct);
}