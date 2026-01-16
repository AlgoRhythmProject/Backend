using AlgoRhythm.Shared.Models.Courses;

namespace AlgoRhythm.Repositories.Courses.Interfaces;

public interface ILectureRepository
{
    Task<IEnumerable<Lecture>> GetAllAsync(bool publishedOnly, CancellationToken ct);
    Task<IEnumerable<Lecture>> GetByCourseIdAsync(Guid courseId, CancellationToken ct);
    Task<Lecture?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Lecture?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct);
    Task CreateAsync(Lecture lecture, CancellationToken ct);
    Task UpdateAsync(Lecture lecture, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task AddTagToLectureAsync(Guid lectureId, Guid tagId, CancellationToken ct);
    Task RemoveTagFromLectureAsync(Guid lectureId, Guid tagId, CancellationToken ct);
    Task<LectureContent> AddContentToLectureAsync(Guid lectureId, LectureContent content, CancellationToken ct);
    Task UpdateContentAsync(Guid contentId, LectureContent content, CancellationToken ct);
    Task RemoveContentFromLectureAsync(Guid lectureId, Guid contentId, CancellationToken ct);
    Task<LectureContent?> GetContentByIdAsync(Guid contentId, CancellationToken ct);
    Task<IEnumerable<LectureContent>> GetContentsByLectureIdAsync(Guid lectureId, CancellationToken ct);
    Task SwapContentOrderAsync(Guid lectureId, Guid firstContentId, Guid secondContentId, CancellationToken ct);
}