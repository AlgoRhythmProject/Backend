using AlgoRhythm.Repositories.Courses.Interfaces;
using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using AlgoRhythm.Shared.Models.Courses;

namespace AlgoRhythm.Services.Courses;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _repo;

    public CourseService(ICourseRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<CourseSummaryDto>> GetAllAsync(CancellationToken ct)
    {
        var courses = await _repo.GetAllAsync(ct);
        return courses.Select(MapToSummaryDto);
    }

    public async Task<IEnumerable<CourseSummaryDto>> GetPublishedAsync(CancellationToken ct)
    {
        var courses = await _repo.GetPublishedAsync(ct);
        return courses.Select(MapToSummaryDto);
    }

    public async Task<CourseDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var course = await _repo.GetByIdWithDetailsAsync(id, ct);
        return course == null ? null : MapToDtoWithDetails(course);
    }

    public async Task<CourseDto> CreateAsync(CourseInputDto dto, CancellationToken ct)
    {
        var course = new Course
        {
            Name = dto.Name,
            Description = dto.Description,
            IsPublished = dto.IsPublished
        };

        await _repo.CreateAsync(course, ct);
        return MapToDto(course);
    }

    public async Task UpdateAsync(Guid id, CourseInputDto dto, CancellationToken ct)
    {
        var course = await _repo.GetByIdAsync(id, ct);
        if (course == null)
            throw new KeyNotFoundException("Course not found");

        course.Name = dto.Name;
        course.Description = dto.Description;
        course.IsPublished = dto.IsPublished;

        await _repo.UpdateAsync(course, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
    }

    public async Task AddTaskToCourseAsync(Guid courseId, Guid taskId, CancellationToken ct)
    {
        await _repo.AddTaskToCourseAsync(courseId, taskId, ct);
    }

    public async Task RemoveTaskFromCourseAsync(Guid courseId, Guid taskId, CancellationToken ct)
    {
        await _repo.RemoveTaskFromCourseAsync(courseId, taskId, ct);
    }

    public async Task AddLectureToCourseAsync(Guid courseId, Guid lectureId, CancellationToken ct)
    {
        await _repo.AddLectureToCourseAsync(courseId, lectureId, ct);
    }

    public async Task RemoveLectureFromCourseAsync(Guid courseId, Guid lectureId, CancellationToken ct)
    {
        await _repo.RemoveLectureFromCourseAsync(courseId, lectureId, ct);
    }

    private static CourseSummaryDto MapToSummaryDto(Course course)
    {
        return new CourseSummaryDto
        {
            Id = course.Id,
            Name = course.Name,
            Description = course.Description,
            IsPublished = course.IsPublished,
            CreatedAt = course.CreatedAt,
            Lectures = course.Lectures?.OrderBy(l => l.CreatedAt).Select(l => new LectureSummaryDto
            {
                Id = l.Id,
                Title = l.Title,
                TagIds = l.Tags?.Select(t => t.Id).ToList() ?? []
            }).ToList() ?? [],
            Tasks = course.TaskItems?.OrderBy(t => t.CreatedAt).Select(t => new TaskSummaryDto
            {
                Id = t.Id,
                Title = t.Title,
                TagIds = t.Tags?.Select(tag => tag.Id).ToList() ?? []
            }).ToList() ?? []
        };
    }

    private static CourseDto MapToDto(Course course)
    {
        return new CourseDto
        {
            Id = course.Id,
            Name = course.Name,
            Description = course.Description,
            IsPublished = course.IsPublished,
            CreatedAt = course.CreatedAt,
            Lectures = [],
            TaskItemIds = []
        };
    }

    private static CourseDto MapToDtoWithDetails(Course course)
    {
        return new CourseDto
        {
            Id = course.Id,
            Name = course.Name,
            Description = course.Description,
            IsPublished = course.IsPublished,
            CreatedAt = course.CreatedAt,
            Lectures = course.Lectures?.OrderBy(l => l.CreatedAt).Select(l => new LectureDto
            {
                Id = l.Id,
                CourseId = l.CourseId,
                Title = l.Title,
                IsPublished = l.IsPublished,
                CreatedAt = l.CreatedAt,
                Contents = l.Contents?.OrderBy(c => c.Order).Select(c => new LectureContentDto
                {
                    Id = c.Id,
                    LectureId = c.LectureId,
                    Type = c.Type.ToString(),
                    Order = c.Order,
                    CreatedAt = c.CreatedAt,
                    HtmlContent = c is LectureText lt ? lt.HtmlContent : null,
                    Path = c is LecturePhoto lp ? lp.Path : null,
                    Alt = c is LecturePhoto lp2 ? lp2.Alt : null,
                    Title = c is LecturePhoto lp3 ? lp3.Title : null
                }).ToList() ?? [],
                TagIds = l.Tags?.Select(t => t.Id).ToList() ?? []
            }).ToList() ?? [],
            TaskItemIds = course.TaskItems?.Select(t => t.Id).ToList() ?? []
        };
    }
}