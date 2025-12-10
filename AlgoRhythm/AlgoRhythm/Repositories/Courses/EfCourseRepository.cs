using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Courses.Interfaces;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories.Courses;

public class EfCourseRepository : ICourseRepository
{
    private readonly ApplicationDbContext _db;

    public EfCourseRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Course>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Courses
            .Include(c => c.Lectures)
                .ThenInclude(l => l.Tags)
            .Include(c => c.TaskItems)
                .ThenInclude(t => t.Tags)
            .OrderBy(c => c.CreatedAt)
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Course>> GetPublishedAsync(CancellationToken ct)
    {
        return await _db.Courses
            .Include(c => c.Lectures.Where(l => l.IsPublished))
                .ThenInclude(l => l.Tags)
            .Include(c => c.TaskItems.Where(t => !t.IsDeleted && t.IsPublished))
                .ThenInclude(t => t.Tags)
            .Where(c => c.IsPublished)
            .OrderBy(c => c.CreatedAt)
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    public async Task<Course?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Course?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct)
    {
        return await _db.Courses
            .Include(c => c.Lectures)
                .ThenInclude(l => l.Contents)
            .Include(c => c.Lectures)
                .ThenInclude(l => l.Tags)
            .Include(c => c.TaskItems)
                .ThenInclude(t => t.Tags)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task CreateAsync(Course course, CancellationToken ct)
    {
        await _db.Courses.AddAsync(course, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Course course, CancellationToken ct)
    {
        _db.Courses.Update(course);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course != null)
        {
            _db.Courses.Remove(course);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task AddTaskToCourseAsync(Guid courseId, Guid taskId, CancellationToken ct)
    {
        var course = await _db.Courses
            .Include(c => c.TaskItems)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        var task = await _db.Set<TaskItem>().FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, ct);

        if (course != null && task != null && !course.TaskItems.Contains(task))
        {
            course.TaskItems.Add(task);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveTaskFromCourseAsync(Guid courseId, Guid taskId, CancellationToken ct)
    {
        var course = await _db.Courses
            .Include(c => c.TaskItems)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        var task = course?.TaskItems.FirstOrDefault(t => t.Id == taskId);

        if (course != null && task != null)
        {
            course.TaskItems.Remove(task);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task AddLectureToCourseAsync(Guid courseId, Guid lectureId, CancellationToken ct)
    {
        var course = await _db.Courses
            .Include(c => c.Lectures)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        var lecture = await _db.Lectures.FirstOrDefaultAsync(l => l.Id == lectureId, ct);

        if (course != null && lecture != null && !course.Lectures.Contains(lecture))
        {
            lecture.CourseId = courseId;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveLectureFromCourseAsync(Guid courseId, Guid lectureId, CancellationToken ct)
    {
        var lecture = await _db.Lectures
            .FirstOrDefaultAsync(l => l.Id == lectureId && l.CourseId == courseId, ct);

        if (lecture != null)
        {
            _db.Lectures.Remove(lecture);
            await _db.SaveChangesAsync(ct);
        }
    }
}