using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Courses.Interfaces;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Submissions;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;

namespace AlgoRhythm.Repositories.Courses;

public class EfCourseProgressRepository : ICourseProgressRepository
{
    private readonly ApplicationDbContext _db;
    
    public EfCourseProgressRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<CourseProgress>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await _db.CourseProgresses
            .Include(cp => cp.Course)
                .ThenInclude(c => c.Lectures)
            .Include(cp => cp.Course)
                .ThenInclude(c => c.TaskItems)
            .Where(cp => cp.UserId == userId)
            .OrderByDescending(cp => cp.StartedAt)
            .ToListAsync(ct);
    }

    public async Task<CourseProgress?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        return await _db.CourseProgresses
            .Include(cp => cp.Course)
                .ThenInclude(c => c.Lectures)
            .Include(cp => cp.Course)
                .ThenInclude(c => c.TaskItems)
            .FirstOrDefaultAsync(cp => cp.UserId == userId && cp.CourseId == courseId, ct);
    }

    public async Task<CourseProgress?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.CourseProgresses
            .Include(cp => cp.Course)
                .ThenInclude(c => c.Lectures)
            .Include(cp => cp.Course)
                .ThenInclude(c => c.TaskItems)
            .Include(cp => cp.User)
            .FirstOrDefaultAsync(cp => cp.Id == id, ct);
    }

    public async Task CreateAsync(CourseProgress progress, CancellationToken ct)
    {
        await _db.CourseProgresses.AddAsync(progress, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CourseProgress progress, CancellationToken ct)
    {
        _db.CourseProgresses.Update(progress);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var progress = await _db.CourseProgresses.FirstOrDefaultAsync(cp => cp.Id == id, ct);
        if (progress != null)
        {
            _db.CourseProgresses.Remove(progress);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<HashSet<Guid>> GetCompletedLectureIdsAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.CompletedLectures)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            return new HashSet<Guid>();

        var courseLectureIds = await _db.Lectures
            .Where(l => l.CourseId == courseId)
            .Select(l => l.Id)
            .ToListAsync(ct);

        return user.CompletedLectures
            .Where(l => courseLectureIds.Contains(l.Id))
            .Select(l => l.Id)
            .ToHashSet();
    }

    public async Task<HashSet<Guid>> GetCompletedTaskIdsAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        var completedFromSubmissions = await _db.Submissions
            .Where(s => s.UserId == userId && s.TaskItem.Courses.Any(c => c.Id == courseId))
            .OfType<ProgrammingSubmission>()
            .Where(ps => ps.IsSolved)
            .Select(s => s.TaskItemId)
            .Distinct()
            .ToListAsync(ct);

        var user = await _db.Users
            .Include(u => u.CompletedTasks)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
            return completedFromSubmissions.ToHashSet();

        var courseTaskIds = await _db.TaskItems
            .Where(t => t.Courses.Any(c => c.Id == courseId))
            .Select(t => t.Id)
            .ToListAsync(ct);

        var manuallyCompleted = user.CompletedTasks
            .Where(t => courseTaskIds.Contains(t.Id))
            .Select(t => t.Id);

        return completedFromSubmissions
            .Union(manuallyCompleted)
            .ToHashSet();
    }

    public async Task<bool> IsLectureCompletedAsync(Guid userId, Guid lectureId, CancellationToken ct)
    {
        return await _db.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.CompletedLectures)
            .AnyAsync(l => l.Id == lectureId, ct);
    }
}