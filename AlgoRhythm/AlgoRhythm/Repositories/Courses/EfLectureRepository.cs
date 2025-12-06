using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Courses.Interfaces;
using AlgoRhythm.Shared.Models.Courses;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories.Courses;

public class EfLectureRepository : ILectureRepository
{
    private readonly ApplicationDbContext _db;
    
    public EfLectureRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Lecture>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Lectures
            .Include(l => l.Contents.OrderBy(c => c.Order))
            .Include(l => l.Tags)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Lecture>> GetByCourseIdAsync(Guid courseId, CancellationToken ct)
    {
        return await _db.Lectures
            .Include(l => l.Contents.OrderBy(c => c.Order))
            .Include(l => l.Tags)
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
            .Include(l => l.Contents.OrderBy(c => c.Order))
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

    public async Task<LectureContent> AddContentToLectureAsync(Guid lectureId, LectureContent content, CancellationToken ct)
    {
        var lecture = await _db.Lectures
            .Include(l => l.Contents)
            .FirstOrDefaultAsync(l => l.Id == lectureId, ct);

        if (lecture == null)
            throw new KeyNotFoundException("Lecture not found");

        content.LectureId = lectureId;
        
        // Ustaw order na nastêpny wolny numer
        var maxOrder = lecture.Contents.Any() ? lecture.Contents.Max(c => c.Order) : -1;
        content.Order = maxOrder + 1;
        
        if (content is LectureText lectureText)
        {
            await _db.Set<LectureText>().AddAsync(lectureText, ct);
        }
        else if (content is LecturePhoto lecturePhoto)
        {
            await _db.Set<LecturePhoto>().AddAsync(lecturePhoto, ct);
        }

        await _db.SaveChangesAsync(ct);
        return content;
    }

    public async Task UpdateContentAsync(Guid contentId, LectureContent content, CancellationToken ct)
    {
        var existingContent = await _db.Set<LectureContent>()
            .FirstOrDefaultAsync(c => c.Id == contentId, ct);

        if (existingContent == null)
            throw new KeyNotFoundException("Content not found");

        if (existingContent is LectureText existingText && content is LectureText newText)
        {
            existingText.HtmlContent = newText.HtmlContent;
        }
        else if (existingContent is LecturePhoto existingPhoto && content is LecturePhoto newPhoto)
        {
            existingPhoto.Path = newPhoto.Path;
            existingPhoto.Alt = newPhoto.Alt;
            existingPhoto.Title = newPhoto.Title;
        }
        else
        {
            throw new ArgumentException("Content type mismatch");
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveContentFromLectureAsync(Guid lectureId, Guid contentId, CancellationToken ct)
    {
        var lecture = await _db.Lectures
            .Include(l => l.Contents)
            .FirstOrDefaultAsync(l => l.Id == lectureId, ct);

        var content = lecture?.Contents.FirstOrDefault(c => c.Id == contentId);

        if (lecture != null && content != null)
        {
            _db.Set<LectureContent>().Remove(content);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<LectureContent?> GetContentByIdAsync(Guid contentId, CancellationToken ct)
    {
        return await _db.Set<LectureContent>()
            .Include(c => c.Lecture)
            .FirstOrDefaultAsync(c => c.Id == contentId, ct);
    }

    public async Task<IEnumerable<LectureContent>> GetContentsByLectureIdAsync(Guid lectureId, CancellationToken ct)
    {
        return await _db.Set<LectureContent>()
            .Where(c => c.LectureId == lectureId)
            .OrderBy(c => c.Order)
            .ToListAsync(ct);
    }

    public async Task SwapContentOrderAsync(Guid lectureId, Guid firstContentId, Guid secondContentId, CancellationToken ct)
    {
        var lecture = await _db.Lectures
            .Include(l => l.Contents)
            .FirstOrDefaultAsync(l => l.Id == lectureId, ct);

        if (lecture == null)
            throw new KeyNotFoundException("Lecture not found");

        var firstContent = lecture.Contents.FirstOrDefault(c => c.Id == firstContentId);
        var secondContent = lecture.Contents.FirstOrDefault(c => c.Id == secondContentId);

        if (firstContent == null || secondContent == null)
            throw new KeyNotFoundException("One or both contents not found in this lecture");

        // Zamieñ ordery
        (firstContent.Order, secondContent.Order) = (secondContent.Order, firstContent.Order);

        await _db.SaveChangesAsync(ct);
    }
}