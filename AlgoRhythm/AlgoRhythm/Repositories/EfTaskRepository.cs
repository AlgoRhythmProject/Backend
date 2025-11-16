using AlgoRhythm.Data;
using AlgoRhythm.Repositories.Interfaces;
using AlgoRhythm.Shared.Models.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AlgoRhythm.Repositories;

public class EfTaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _db;

    public EfTaskRepository(ApplicationDbContext db) => _db = db;

    public async Task<ProgrammingTaskItem?> GetProgrammingTaskAsync(Guid id, CancellationToken ct)
    {
        return await _db.ProgrammingTaskItems
            .OfType<ProgrammingTaskItem>()
            .Include(t => t.TestCases)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

}
