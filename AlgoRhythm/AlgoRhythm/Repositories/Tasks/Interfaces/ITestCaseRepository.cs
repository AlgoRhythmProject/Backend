using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Repositories.Tasks.Interfaces;

public interface ITestCaseRepository
{
    Task<IEnumerable<TestCase>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<TestCase>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default);
    Task<TestCase?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TestCase> CreateAsync(TestCase testCase, CancellationToken ct = default);
    Task<TestCase> UpdateAsync(TestCase testCase, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> TaskExistsAsync(Guid taskId, CancellationToken ct = default);
}