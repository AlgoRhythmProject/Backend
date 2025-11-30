using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Interfaces;
using AlgoRhythm.Shared.Models.Courses;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories;

public class EfLectureRepository : ILectureRepository
{
    private readonly ApplicationDbContext _db;
    
    public EfLectureRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Lecture>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Lectures
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Lecture>> GetByCourseIdAsync(Guid courseId, CancellationToken ct)
    {
        return await _db.Lectures
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Lecture?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Lectures
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task<Lecture?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct)
    {
        return await _db.Lectures
            .Include(l => l.Contents)
            .Include(l => l.Tags)
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task CreateAsync(Lecture lecture, CancellationToken ct)
    {
        await _db.Lectures.AddAsync(lecture, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Lecture lecture, CancellationToken ct)
    {
        _db.Lectures.Update(lecture);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var lecture = await _db.Lectures.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lecture != null)
        {
            _db.Lectures.Remove(lecture);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task AddTagToLectureAsync(Guid lectureId, Guid tagId, CancellationToken ct)
    {
        var lecture = await _db.Lectures
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Id == lectureId, ct);
        
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == tagId, ct);

        if (lecture != null && tag != null && !lecture.Tags.Contains(tag))
        {
            lecture.Tags.Add(tag);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveTagFromLectureAsync(Guid lectureId, Guid tagId, CancellationToken ct)
    {
        var lecture = await _db.Lectures
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Id == lectureId, ct);
        
        var tag = lecture?.Tags.FirstOrDefault(t => t.Id == tagId);

        if (lecture != null && tag != null)
        {
            lecture.Tags.Remove(tag);
            await _db.SaveChangesAsync(ct);
        }
    }
}