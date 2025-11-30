using AlgoRhythm.Repositories.Interfaces;
using AlgoRhythm.Services.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using AlgoRhythm.Shared.Models.Courses;

namespace AlgoRhythm.Services;

public class LectureService : ILectureService
{
    private readonly ILectureRepository _repo;

    public LectureService(ILectureRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<LectureDto>> GetAllAsync(CancellationToken ct)
    {
        var lectures = await _repo.GetAllAsync(ct);
        return lectures.Select(MapToDto);
    }

    public async Task<IEnumerable<LectureDto>> GetByCourseIdAsync(Guid courseId, CancellationToken ct)
    {
        var lectures = await _repo.GetByCourseIdAsync(courseId, ct);
        return lectures.Select(MapToDto);
    }

    public async Task<LectureDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var lecture = await _repo.GetByIdWithDetailsAsync(id, ct);
        return lecture == null ? null : MapToDtoWithDetails(lecture);
    }

    public async Task<LectureDto> CreateAsync(LectureInputDto dto, CancellationToken ct)
    {
        var lecture = new Lecture
        {
            CourseId = dto.CourseId,
            Title = dto.Title,
            IsPublished = dto.IsPublished
        };

        await _repo.CreateAsync(lecture, ct);
        return MapToDto(lecture);
    }

    public async Task UpdateAsync(Guid id, LectureInputDto dto, CancellationToken ct)
    {
        var lecture = await _repo.GetByIdAsync(id, ct);
        if (lecture == null)
            throw new KeyNotFoundException("Lecture not found");

        lecture.Title = dto.Title;
        lecture.IsPublished = dto.IsPublished;

        await _repo.UpdateAsync(lecture, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
    }

    public async Task AddTagAsync(Guid lectureId, Guid tagId, CancellationToken ct)
    {
        await _repo.AddTagToLectureAsync(lectureId, tagId, ct);
    }

    public async Task RemoveTagAsync(Guid lectureId, Guid tagId, CancellationToken ct)
    {
        await _repo.RemoveTagFromLectureAsync(lectureId, tagId, ct);
    }

    private static LectureDto MapToDto(Lecture lecture)
    {
        return new LectureDto
        {
            Id = lecture.Id,
            CourseId = lecture.CourseId,
            Title = lecture.Title,
            IsPublished = lecture.IsPublished,
            CreatedAt = lecture.CreatedAt,
            Contents = [],
            TagIds = []
        };
    }

    private static LectureDto MapToDtoWithDetails(Lecture lecture)
    {
        return new LectureDto
        {
            Id = lecture.Id,
            CourseId = lecture.CourseId,
            Title = lecture.Title,
            IsPublished = lecture.IsPublished,
            CreatedAt = lecture.CreatedAt,
            Contents = lecture.Contents?.Select(c => new LectureContentDto
            {
                Id = c.Id,
                Type = c.Type.ToString(),
                Text = c is LectureText lt ? lt.Text : null,
                Path = c is LecturePhoto lp ? lp.Path : null,
                Alt = c is LecturePhoto lp2 ? lp2.Alt : null,
                Title = c is LecturePhoto lp3 ? lp3.Title : null
            }).ToList() ?? [],
            TagIds = lecture.Tags?.Select(t => t.Id).ToList() ?? []
        };
    }
}