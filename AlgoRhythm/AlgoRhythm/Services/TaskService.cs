using AlgoRhythm.Repositories.Interfaces;
using AlgoRhythm.Services.Interfaces;
using AlgoRhythm.Shared.Dtos.Tasks;
using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repo;

    public TaskService(ITaskRepository repo)
    {
        _repo = repo;
    }

    // GET -------------------------------------

    public async Task<IEnumerable<TaskDto>> GetAllAsync(CancellationToken ct)
    {
        var tasks = await _repo.GetAllAsync(ct);
        return tasks.Select(MapToDto);
    }

    public async Task<TaskDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var task = await _repo.GetByIdAsync(id, ct);
        return task == null ? null : MapToDto(task);
    }

    // CREATE ----------------------------------

    public async Task<TaskDto> CreateProgrammingAsync(ProgrammingTaskInputDto dto, CancellationToken ct)
    {
        var task = new ProgrammingTaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Difficulty = dto.Difficulty,
            IsPublished = dto.IsPublished,
            TemplateCode = dto.TemplateCode,

        };

        await _repo.CreateAsync(task, ct);
        return MapToDto(task);
    }

    // UPDATE ----------------------------------

    public async Task UpdateProgrammingAsync(Guid id, ProgrammingTaskInputDto dto, CancellationToken ct)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing is not ProgrammingTaskItem p)
            throw new KeyNotFoundException("Programming task not found");

        p.Title = dto.Title;
        p.Description = dto.Description;
        p.Difficulty = dto.Difficulty;
        p.IsPublished = dto.IsPublished;

        p.TemplateCode = dto.TemplateCode;


        await _repo.UpdateAsync(p, ct);
    }

    // DELETE ----------------------------------

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
    }

    // MAPPING ---------------------------------

    private static TaskDto MapToDto(TaskItem task)
    {
        if (task is ProgrammingTaskItem p)
        {
            return new ProgrammingTaskDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Difficulty = p.Difficulty.ToString(),
                IsPublished = p.IsPublished,
                Type = "programming",
                TemplateCode = p.TemplateCode,
                TestCases = p.TestCases
                    .Select(tc => new TestCaseDto
                    {
                        Id = tc.Id,
                        InputJson = tc.InputJson,
                        ExpectedJson = tc.ExpectedJson,
                        MaxPoints = tc.MaxPoints,
                        IsVisible = tc.IsVisible
                    })
                    .ToList()
            };
        }

        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Difficulty = task.Difficulty.ToString(),
            IsPublished = task.IsPublished,
            Type = "basic"
        };
    }
}

