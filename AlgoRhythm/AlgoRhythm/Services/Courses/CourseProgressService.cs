using AlgoRhythm.Repositories.Courses.Interfaces;
using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using AlgoRhythm.Shared.Models.Courses;

namespace AlgoRhythm.Services.Courses;

public class CourseProgressService : ICourseProgressService
{
    private readonly ICourseProgressRepository _repo;

    public CourseProgressService(ICourseProgressRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<CourseProgressDto>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var progresses = await _repo.GetByUserIdAsync(userId, ct);
        return progresses.Select(MapToDto);
    }

    public async Task<CourseProgressDto?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        var progress = await _repo.GetByUserAndCourseAsync(userId, courseId, ct);
        return progress == null ? null : MapToDto(progress);
    }

    public async Task<CourseProgressDto> StartCourseAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        var existing = await _repo.GetByUserAndCourseAsync(userId, courseId, ct);
        if (existing != null)
            return MapToDto(existing);

        var progress = new CourseProgress
        {
            UserId = userId,
            CourseId = courseId,
            Percentage = 0,
            StartedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(progress, ct);
        return MapToDto(progress);
    }

    public async Task UpdateProgressAsync(Guid userId, Guid courseId, int percentage, CancellationToken ct)
    {
        var progress = await _repo.GetByUserAndCourseAsync(userId, courseId, ct);
        if (progress == null)
            throw new KeyNotFoundException("Course progress not found");

        progress.Percentage = Math.Clamp(percentage, 0, 100);
        
        if (progress.Percentage == 100 && progress.CompletedAt == null)
        {
            progress.CompletedAt = DateTime.UtcNow;
        }

        await _repo.UpdateAsync(progress, ct);
    }

    public async Task CompleteCourseAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        await UpdateProgressAsync(userId, courseId, 100, ct);
    }

    private static CourseProgressDto MapToDto(CourseProgress progress)
    {
        return new CourseProgressDto
        {
            Id = progress.Id,
            UserId = progress.UserId,
            CourseId = progress.CourseId,
            CourseName = progress.Course?.Name,
            Percentage = progress.Percentage,
            StartedAt = progress.StartedAt,
            CompletedAt = progress.CompletedAt
        };
    }
}