using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Courses.Interfaces;
using AlgoRhythm.Shared.Models.Common;
using AlgoRhythm.Shared.Models.Courses;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories.Courses;

public class EfLectureRepository : ILectureRepository
{
    private readonly ApplicationDbContext _db;

    public EfLectureRepository(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<Lecture>> GetAllAsync(bool publishedOnly, CancellationToken ct)
    {
        return await _db.Lectures
            .Include(l => l.Courses)
            .Include(l => l.Tags)
            .Where(l => !publishedOnly || l.IsPublished)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Lecture>> GetByCourseIdAsync(Guid courseId, CancellationToken ct)
    {
        return await _db.Lectures
            .Include(l => l.Courses)
            .Include(l => l.Tags)
            .Include(l => l.Contents.OrderBy(c => c.Order))
            .Where(l => l.Courses.Any(c => c.Id == courseId))
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Lecture?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Lectures
            .Include(l => l.Courses)
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task<Lecture?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct)
    {
        return await _db.Lectures
            .Include(l => l.Courses)
            .Include(l => l.Tags)
            .Include(l => l.Contents.OrderBy(c => c.Order))
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
        
        // Set order to be last
        if (lecture.Contents.Any())
        {
            content.Order = lecture.Contents.Max(c => c.Order) + 1;
        }
        else
        {
            content.Order = 0;
        }

        await _db.LectureContents.AddAsync(content, ct);
        await _db.SaveChangesAsync(ct);
        
        return content;
    }

    public async Task UpdateContentAsync(Guid contentId, LectureContent content, CancellationToken ct)
    {
        var existingContent = await _db.LectureContents.FirstOrDefaultAsync(c => c.Id == contentId, ct);
        if (existingContent == null)
            throw new KeyNotFoundException("Content not found");

        // Update properties based on type
        if (existingContent is LectureText textContent && content is LectureText newTextContent)
        {
            textContent.HtmlContent = newTextContent.HtmlContent;
        }
        else if (existingContent is LecturePhoto photoContent && content is LecturePhoto newPhotoContent)
        {
            photoContent.Path = newPhotoContent.Path;
            photoContent.Alt = newPhotoContent.Alt;
            photoContent.Title = newPhotoContent.Title;
        }

        existingContent.Order = content.Order;
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
            lecture.Contents.Remove(content);
            _db.LectureContents.Remove(content);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<LectureContent?> GetContentByIdAsync(Guid contentId, CancellationToken ct)
    {
        return await _db.LectureContents.FirstOrDefaultAsync(c => c.Id == contentId, ct);
    }

    public async Task<IEnumerable<LectureContent>> GetContentsByLectureIdAsync(Guid lectureId, CancellationToken ct)
    {
        return await _db.LectureContents
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

        // Swap orders
        (firstContent.Order, secondContent.Order) = (secondContent.Order, firstContent.Order);

        await _db.SaveChangesAsync(ct);
    }
}