using AlgoRhythm.Shared.Dtos.Courses;

namespace AlgoRhythm.Services.Interfaces;

public interface ILectureService
{
    Task<IEnumerable<LectureDto>> GetAllAsync(CancellationToken ct);
    Task<IEnumerable<LectureDto>> GetByCourseIdAsync(Guid courseId, CancellationToken ct);
    Task<LectureDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<LectureDto> CreateAsync(LectureInputDto dto, CancellationToken ct);
    Task UpdateAsync(Guid id, LectureInputDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task AddTagAsync(Guid lectureId, Guid tagId, CancellationToken ct);
    Task RemoveTagAsync(Guid lectureId, Guid tagId, CancellationToken ct);
}