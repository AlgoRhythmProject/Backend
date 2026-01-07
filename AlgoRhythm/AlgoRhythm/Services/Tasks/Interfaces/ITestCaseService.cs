using AlgoRhythm.Shared.Dtos.Tasks;

namespace AlgoRhythm.Services.Tasks.Interfaces;

public interface ITestCaseService
{
    Task<IEnumerable<TestCaseDto>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<TestCaseDto>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default);
    Task<TestCaseDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TestCaseDto> CreateAsync(CreateTestCaseDto dto, CancellationToken ct = default);
    Task<TestCaseDto> UpdateAsync(Guid id, UpdateTestCaseDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}