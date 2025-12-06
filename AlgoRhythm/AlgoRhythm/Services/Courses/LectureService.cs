using AlgoRhythm.Repositories.Courses.Interfaces;
using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using AlgoRhythm.Shared.Models.Courses;

namespace AlgoRhythm.Services.Courses;

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
        return lectures.Select(MapToDtoWithDetails);
    }

    public async Task<IEnumerable<LectureDto>> GetByCourseIdAsync(Guid courseId, CancellationToken ct)
    {
        var lectures = await _repo.GetByCourseIdAsync(courseId, ct);
        return lectures.Select(MapToDtoWithDetails);
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

    public async Task<LectureContentDto> AddContentAsync(Guid lectureId, LectureContentInputDto dto, CancellationToken ct)
    {
        LectureContent content;

        if (dto.Type == "Text")
        {
            if (string.IsNullOrWhiteSpace(dto.HtmlContent))
                throw new ArgumentException("HtmlContent is required for Text content type.");

            content = new LectureText
            {
                Type = ContentType.Text,
                HtmlContent = dto.HtmlContent
            };
        }
        else if (dto.Type == "Photo")
        {
            if (string.IsNullOrWhiteSpace(dto.Path))
                throw new ArgumentException("Path is required for Photo content type.");

            content = new LecturePhoto
            {
                Type = ContentType.Photo,
                Path = dto.Path,
                Alt = dto.Alt,
                Title = dto.Title
            };
        }
        else
        {
            throw new ArgumentException($"Invalid content type: {dto.Type}. Must be 'Text' or 'Photo'.");
        }

        var created = await _repo.AddContentToLectureAsync(lectureId, content, ct);
        return MapContentToDto(created);
    }

    public async Task<LectureContentDto?> GetContentByIdAsync(Guid contentId, CancellationToken ct)
    {
        var content = await _repo.GetContentByIdAsync(contentId, ct);
        return content == null ? null : MapContentToDto(content);
    }

    public async Task<IEnumerable<LectureContentDto>> GetAllContentsByLectureIdAsync(Guid lectureId, CancellationToken ct)
    {
        var contents = await _repo.GetContentsByLectureIdAsync(lectureId, ct);
        return contents.Select(MapContentToDto);
    }

    public async Task UpdateContentAsync(Guid lectureId, Guid contentId, LectureContentInputDto dto, CancellationToken ct)
    {
        LectureContent content;

        if (dto.Type == "Text")
        {
            if (string.IsNullOrWhiteSpace(dto.HtmlContent))
                throw new ArgumentException("HtmlContent is required for Text content type.");

            content = new LectureText
            {
                Id = contentId,
                LectureId = lectureId,
                Type = ContentType.Text,
                HtmlContent = dto.HtmlContent
            };
        }
        else if (dto.Type == "Photo")
        {
            if (string.IsNullOrWhiteSpace(dto.Path))
                throw new ArgumentException("Path is required for Photo content type.");

            content = new LecturePhoto
            {
                Id = contentId,
                LectureId = lectureId,
                Type = ContentType.Photo,
                Path = dto.Path,
                Alt = dto.Alt,
                Title = dto.Title
            };
        }
        else
        {
            throw new ArgumentException($"Invalid content type: {dto.Type}. Must be 'Text' or 'Photo'.");
        }

        await _repo.UpdateContentAsync(contentId, content, ct);
    }

    public async Task RemoveContentAsync(Guid lectureId, Guid contentId, CancellationToken ct)
    {
        await _repo.RemoveContentFromLectureAsync(lectureId, contentId, ct);
    }

    public async Task SwapContentOrderAsync(Guid lectureId, Guid firstContentId, Guid secondContentId, CancellationToken ct)
    {
        await _repo.SwapContentOrderAsync(lectureId, firstContentId, secondContentId, ct);
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
            Contents = lecture.Contents?.OrderBy(c => c.Order).Select(MapContentToDto).ToList() ?? [],
            TagIds = lecture.Tags?.Select(t => t.Id).ToList() ?? []
        };
    }

    private static LectureContentDto MapContentToDto(LectureContent content)
    {
        return new LectureContentDto
        {
            Id = content.Id,
            LectureId = content.LectureId,
            Type = content.Type.ToString(),
            Order = content.Order,
            CreatedAt = content.CreatedAt,
            HtmlContent = content is LectureText lt ? lt.HtmlContent : null,
            Path = content is LecturePhoto lp ? lp.Path : null,
            Alt = content is LecturePhoto lp2 ? lp2.Alt : null,
            Title = content is LecturePhoto lp3 ? lp3.Title : null
        };
    }
}