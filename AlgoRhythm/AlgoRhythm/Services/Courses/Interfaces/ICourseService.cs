using AlgoRhythm.Shared.Dtos.Courses;

namespace AlgoRhythm.Services.Courses.Interfaces;

public interface ICourseService
{
    Task<IEnumerable<CourseDto>> GetAllAsync(CancellationToken ct);
    Task<IEnumerable<CourseDto>> GetPublishedAsync(CancellationToken ct);
    Task<CourseDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CourseDto> CreateAsync(CourseInputDto dto, CancellationToken ct);
    Task UpdateAsync(Guid id, CourseInputDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task AddTaskToCourseAsync(Guid courseId, Guid taskId, CancellationToken ct);
    Task RemoveTaskFromCourseAsync(Guid courseId, Guid taskId, CancellationToken ct);
    Task AddLectureToCourseAsync(Guid courseId, Guid lectureId, CancellationToken ct);
    Task RemoveLectureFromCourseAsync(Guid courseId, Guid lectureId, CancellationToken ct);
    Task AddTagToCourseAsync(Guid courseId, Guid tagId, CancellationToken ct);
    Task RemoveTagFromCourseAsync(Guid courseId, Guid tagId, CancellationToken ct);
}