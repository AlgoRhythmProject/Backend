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

    public async Task<IEnumerable<LectureDto>> GetAllAsync(bool publishedOnly, CancellationToken ct)
    {
        var lectures = await _repo.GetAllAsync(publishedOnly, ct);
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
        return lecture == null ? null : MapToDto(lecture);
    }

    public async Task<LectureDto> CreateAsync(LectureInputDto dto, CancellationToken ct)
    {
        var lecture = new Lecture
        {
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
                throw new ArgumentException("HtmlContent is required for Text type content");

            content = new LectureText
            {
                HtmlContent = dto.HtmlContent,
                Type = ContentType.Text
            };
        }
        else if (dto.Type == "Photo")
        {
            if (string.IsNullOrWhiteSpace(dto.Path))
                throw new ArgumentException("Path is required for Photo type content");

            content = new LecturePhoto
            {
                Path = dto.Path,
                Alt = dto.Alt,
                Title = dto.Title,
                Type = ContentType.Photo
            };
        }
        else if (dto.Type == "Video")
        {
            if (string.IsNullOrWhiteSpace(dto.FileName))
                throw new ArgumentException("FileName is required for Video type content");

            content = new LectureVideo
            {
                FileName = dto.FileName,
                StreamUrl = dto.StreamUrl ?? string.Empty,
                Type = ContentType.Video
            };
        }
        else
        {
            throw new ArgumentException($"Invalid content type: {dto.Type}");
        }

        var createdContent = await _repo.AddContentToLectureAsync(lectureId, content, ct);
        return MapContentToDto(createdContent);
    }

    public async Task UpdateContentAsync(Guid lectureId, Guid contentId, LectureContentInputDto dto, CancellationToken ct)
    {
        var existingContent = await _repo.GetContentByIdAsync(contentId, ct);
        if (existingContent == null || existingContent.LectureId != lectureId)
            throw new KeyNotFoundException("Content not found in this lecture");

        LectureContent updatedContent;

        if (dto.Type == "Text")
        {
            if (string.IsNullOrWhiteSpace(dto.HtmlContent))
                throw new ArgumentException("HtmlContent is required for Text type content");

            updatedContent = new LectureText
            {
                Id = contentId,
                LectureId = lectureId,
                HtmlContent = dto.HtmlContent,
                Type = ContentType.Text,
                Order = dto.Order
            };
        }
        else if (dto.Type == "Photo")
        {
            if (string.IsNullOrWhiteSpace(dto.Path))
                throw new ArgumentException("Path is required for Photo type content");

            updatedContent = new LecturePhoto
            {
                Id = contentId,
                LectureId = lectureId,
                Path = dto.Path,
                Alt = dto.Alt,
                Title = dto.Title,
                Type = ContentType.Photo,
                Order = dto.Order
            };
        }
        else if (dto.Type == "Video")
        {
            if (string.IsNullOrWhiteSpace(dto.FileName))
                throw new ArgumentException("FileName is required for Video type content");

            updatedContent = new LectureVideo
            {
                Id = contentId,
                LectureId = lectureId,
                FileName = dto.FileName,
                StreamUrl = dto.StreamUrl ?? string.Empty,
                Type = ContentType.Video,
                Order = dto.Order
            };
        }
        else
        {
            throw new ArgumentException($"Invalid content type: {dto.Type}");
        }

        await _repo.UpdateContentAsync(contentId, updatedContent, ct);
    }

    public async Task RemoveContentAsync(Guid lectureId, Guid contentId, CancellationToken ct)
    {
        await _repo.RemoveContentFromLectureAsync(lectureId, contentId, ct);
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

    public async Task SwapContentOrderAsync(Guid lectureId, Guid firstContentId, Guid secondContentId, CancellationToken ct)
    {
        await _repo.SwapContentOrderAsync(lectureId, firstContentId, secondContentId, ct);
    }

    private static LectureDto MapToDto(Lecture lecture)
    {
        return new LectureDto
        {
            Id = lecture.Id,
            Title = lecture.Title,
            IsPublished = lecture.IsPublished,
            CreatedAt = lecture.CreatedAt,
            Contents = lecture.Contents?.OrderBy(c => c.Order).Select(MapContentToDto).ToList() ?? new List<LectureContentDto>(),
            TagIds = lecture.Tags?.Select(t => t.Id).ToList() ?? new List<Guid>(),
            CourseIds = lecture.Courses?.Select(c => c.Id).ToList() ?? new List<Guid>()
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
            HtmlContent = content is LectureText text ? text.HtmlContent : null,
            Path = content is LecturePhoto photo ? photo.Path : null,
            Alt = content is LecturePhoto photo2 ? photo2.Alt : null,
            Title = content is LecturePhoto photo3 ? photo3.Title : null,
            FileName = content is LectureVideo video ? video.FileName : null,
            StreamUrl = content is LectureVideo video2 ? video2.StreamUrl : null,
            FileSize = content is LectureVideo video3 ? video3.FileSize : null,
            LastModified = content is LectureVideo video4 ? video4.LastModified : null
        };
    }
}