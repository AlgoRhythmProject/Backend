using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Courses.Interfaces;
using AlgoRhythm.Shared.Models.Courses;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories.Courses;

public class EfCourseProgressRepository : ICourseProgressRepository
{
    private readonly ApplicationDbContext _db;
    
    public EfCourseProgressRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<CourseProgress>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await _db.CourseProgresses
            .Include(cp => cp.Course)
            .Where(cp => cp.UserId == userId)
            .OrderByDescending(cp => cp.StartedAt)
            .ToListAsync(ct);
    }

    public async Task<CourseProgress?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        return await _db.CourseProgresses
            .Include(cp => cp.Course)
            .FirstOrDefaultAsync(cp => cp.UserId == userId && cp.CourseId == courseId, ct);
    }

    public async Task<CourseProgress?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.CourseProgresses
            .Include(cp => cp.Course)
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
}