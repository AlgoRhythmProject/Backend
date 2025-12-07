using AlgoRhythm.Repositories.Tasks.Interfaces;
using AlgoRhythm.Services.Tasks.Interfaces;
using AlgoRhythm.Shared.Dtos.Tasks;
using AlgoRhythm.Shared.Models.Tasks;

namespace AlgoRhythm.Services.Tasks;

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

    public async Task<IEnumerable<TaskWithCoursesDto>> GetAllWithCoursesAsync(CancellationToken ct)
    {
        var tasks = await _repo.GetAllAsync(ct);
        return tasks.Select(MapToWithCoursesDto);
    }

    public async Task<IEnumerable<TaskDto>> GetPublishedAsync(CancellationToken ct)
    {
        var tasks = await _repo.GetPublishedAsync(ct);
        return tasks.Select(MapToDto);
    }

    public async Task<TaskDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var task = await _repo.GetByIdWithDetailsAsync(id, ct);
        return task == null ? null : MapToDetailsDto(task);
    }

    // CREATE ----------------------------------

    public async Task<TaskDto> CreateAsync(TaskInputDto dto, CancellationToken ct)
    {
        TaskItem task;

        if (dto.TaskType == "Programming")
        {
            task = new ProgrammingTaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Difficulty = Enum.Parse<Difficulty>(dto.Difficulty),
                IsPublished = dto.IsPublished,
                TemplateCode = dto.TemplateCode
            };
        }
        else if (dto.TaskType == "Interactive")
        {
            task = new InteractiveTaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Difficulty = Enum.Parse<Difficulty>(dto.Difficulty),
                IsPublished = dto.IsPublished,
                OptionsJson = dto.OptionsJson,
                CorrectAnswer = dto.CorrectAnswer
            };
        }
        else
        {
            throw new ArgumentException($"Invalid task type: {dto.TaskType}. Must be 'Programming' or 'Interactive'.");
        }

        await _repo.CreateAsync(task, ct);
        return MapToDto(task);
    }

    // UPDATE ----------------------------------

    public async Task UpdateAsync(Guid id, TaskInputDto dto, CancellationToken ct)
    {
        var task = await _repo.GetByIdAsync(id, ct);
        if (task == null)
            throw new KeyNotFoundException("Task not found");

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Difficulty = Enum.Parse<Difficulty>(dto.Difficulty);
        task.IsPublished = dto.IsPublished;

        if (task is ProgrammingTaskItem programmingTask && dto.TaskType == "Programming")
        {
            programmingTask.TemplateCode = dto.TemplateCode;
        }
        else if (task is InteractiveTaskItem interactiveTask && dto.TaskType == "Interactive")
        {
            interactiveTask.OptionsJson = dto.OptionsJson;
            interactiveTask.CorrectAnswer = dto.CorrectAnswer;
        }
        else
        {
            throw new ArgumentException("Cannot change task type after creation.");
        }

        await _repo.UpdateAsync(task, ct);
    }

    // DELETE ----------------------------------

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await _repo.DeleteAsync(id, ct);
    }

    public async Task AddTagAsync(Guid taskId, Guid tagId, CancellationToken ct)
    {
        await _repo.AddTagToTaskAsync(taskId, tagId, ct);
    }

    public async Task RemoveTagAsync(Guid taskId, Guid tagId, CancellationToken ct)
    {
        await _repo.RemoveTagFromTaskAsync(taskId, tagId, ct);
    }

    public async Task AddHintAsync(Guid taskId, Guid hintId, CancellationToken ct)
    {
        await _repo.AddHintToTaskAsync(taskId, hintId, ct);
    }

    public async Task RemoveHintAsync(Guid taskId, Guid hintId, CancellationToken ct)
    {
        await _repo.RemoveHintFromTaskAsync(taskId, hintId, ct);
    }

    // MAPPING ---------------------------------

    private static TaskDto MapToDto(TaskItem task)
    {
        var taskType = task is ProgrammingTaskItem ? "Programming" : "Interactive";
        
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Difficulty = task.Difficulty.ToString(),
            TaskType = taskType,
            IsPublished = task.IsPublished,
            IsDeleted = task.IsDeleted,
            CreatedAt = task.CreatedAt,
            TemplateCode = task is ProgrammingTaskItem pt ? pt.TemplateCode : null,
            OptionsJson = task is InteractiveTaskItem it ? it.OptionsJson : null,
            CorrectAnswer = task is InteractiveTaskItem it2 ? it2.CorrectAnswer : null,
            TagIds = task.Tags?.Select(t => t.Id).ToList() ?? [],
            HintIds = task.Hints?.Select(h => h.Id).ToList() ?? []
        };
    }

    private static TaskWithCoursesDto MapToWithCoursesDto(TaskItem task)
    {
        var taskType = task is ProgrammingTaskItem ? "Programming" : "Interactive";
        
        return new TaskWithCoursesDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Difficulty = task.Difficulty.ToString(),
            TaskType = taskType,
            IsPublished = task.IsPublished,
            CreatedAt = task.CreatedAt,
            TagIds = task.Tags?.Select(t => t.Id).ToList() ?? [],
            Courses = task.Courses?.Select(c => new TaskCourseSummaryDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList() ?? []
        };
    }

    private static TaskDetailsDto MapToDetailsDto(TaskItem task)
    {
        var taskType = task is ProgrammingTaskItem ? "Programming" : "Interactive";
        
        return new TaskDetailsDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Difficulty = task.Difficulty.ToString(),
            TaskType = taskType,
            IsPublished = task.IsPublished,
            IsDeleted = task.IsDeleted,
            CreatedAt = task.CreatedAt,
            TemplateCode = task is ProgrammingTaskItem pt ? pt.TemplateCode : null,
            TestCaseIds = task is ProgrammingTaskItem pt2 ? pt2.TestCases?.Select(tc => tc.Id).ToList() ?? [] : [],
            OptionsJson = task is InteractiveTaskItem it ? it.OptionsJson : null,
            CorrectAnswer = task is InteractiveTaskItem it2 ? it2.CorrectAnswer : null,
            TagIds = task.Tags?.Select(t => t.Id).ToList() ?? [],
            HintIds = task.Hints?.Select(h => h.Id).ToList() ?? [],
            Courses = task.Courses?.Select(c => new TaskCourseSummaryDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList() ?? []
        };
    }
}

