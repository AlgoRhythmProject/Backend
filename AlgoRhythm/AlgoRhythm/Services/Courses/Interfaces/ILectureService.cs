using AlgoRhythm.Shared.Dtos.Courses;

namespace AlgoRhythm.Services.Courses.Interfaces;

public interface ILectureService
{
    Task<IEnumerable<LectureDto>> GetAllAsync(bool publishedOnly, CancellationToken ct);
    Task<IEnumerable<LectureDto>> GetByCourseIdAsync(Guid courseId, CancellationToken ct);
    Task<LectureDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<LectureDto> CreateAsync(LectureInputDto dto, CancellationToken ct);
    Task UpdateAsync(Guid id, LectureInputDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task AddTagAsync(Guid lectureId, Guid tagId, CancellationToken ct);
    Task RemoveTagAsync(Guid lectureId, Guid tagId, CancellationToken ct);
    Task<LectureContentDto> AddContentAsync(Guid lectureId, LectureContentInputDto dto, CancellationToken ct);
    Task<LectureContentDto?> GetContentByIdAsync(Guid contentId, CancellationToken ct);
    Task<IEnumerable<LectureContentDto>> GetAllContentsByLectureIdAsync(Guid lectureId, CancellationToken ct);
    Task UpdateContentAsync(Guid lectureId, Guid contentId, LectureContentInputDto dto, CancellationToken ct);
    Task RemoveContentAsync(Guid lectureId, Guid contentId, CancellationToken ct);
    Task SwapContentOrderAsync(Guid lectureId, Guid firstContentId, Guid secondContentId, CancellationToken ct);
}