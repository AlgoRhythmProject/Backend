using AlgoRhythm.Shared.Models.Courses;

namespace AlgoRhythm.Repositories.Courses.Interfaces;

public interface ILectureRepository
{
    Task<IEnumerable<Lecture>> GetAllAsync(CancellationToken ct);
    Task<IEnumerable<Lecture>> GetByCourseIdAsync(Guid courseId, CancellationToken ct);
    Task<Lecture?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Lecture?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct);
    Task CreateAsync(Lecture lecture, CancellationToken ct);
    Task UpdateAsync(Lecture lecture, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task AddTagToLectureAsync(Guid lectureId, Guid tagId, CancellationToken ct);
    Task RemoveTagFromLectureAsync(Guid lectureId, Guid tagId, CancellationToken ct);
}