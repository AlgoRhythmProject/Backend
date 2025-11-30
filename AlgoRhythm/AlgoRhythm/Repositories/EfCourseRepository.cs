using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Interfaces;
using AlgoRhythm.Shared.Models.Courses;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories;

public class EfCourseRepository : ICourseRepository
{
    private readonly ApplicationDbContext _db;
    
    public EfCourseRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Course>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Courses
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Course>> GetPublishedAsync(CancellationToken ct)
    {
        return await _db.Courses
            .Where(c => c.IsPublished)
            .OrderBy(c => c.CreatedAt)
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
            .Include(c => c.CourseProgresses)
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
        
        var task = await _db.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId, ct);

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
        var lecture = await _db.Lectures.FirstOrDefaultAsync(l => l.Id == lectureId, ct);
        
        if (lecture == null)
            throw new KeyNotFoundException($"Lecture with ID {lectureId} not found");

        var courseExists = await _db.Courses.AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists)
            throw new KeyNotFoundException($"Course with ID {courseId} not found");

        if (lecture.CourseId != courseId)
        {
            lecture.CourseId = courseId;
            _db.Lectures.Update(lecture);
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

    public async Task AddTagToCourseAsync(Guid courseId, Guid tagId, CancellationToken ct)
    {
        // Poniewa¿ Course nie ma bezpoœredniej relacji z Tag,
        // dodajemy tag do wszystkich wyk³adów w kursie
        var course = await _db.Courses
            .Include(c => c.Lectures)
                .ThenInclude(l => l.Tags)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);
        
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == tagId, ct);

        if (course == null)
            throw new KeyNotFoundException($"Course with ID {courseId} not found");
        
        if (tag == null)
            throw new KeyNotFoundException($"Tag with ID {tagId} not found");

        // Dodaj tag do wszystkich wyk³adów, które go jeszcze nie maj¹
        foreach (var lecture in course.Lectures)
        {
            if (!lecture.Tags.Contains(tag))
            {
                lecture.Tags.Add(tag);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveTagFromCourseAsync(Guid courseId, Guid tagId, CancellationToken ct)
    {
        // Usuñ tag ze wszystkich wyk³adów w kursie
        var course = await _db.Courses
            .Include(c => c.Lectures)
                .ThenInclude(l => l.Tags)
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);
        
        if (course == null)
            throw new KeyNotFoundException($"Course with ID {courseId} not found");

        foreach (var lecture in course.Lectures)
        {
            var tag = lecture.Tags.FirstOrDefault(t => t.Id == tagId);
            if (tag != null)
            {
                lecture.Tags.Remove(tag);
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}