using AlgoRhythm.Data;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Tasks;
using AlgoRhythm.Shared.Models.Tasks;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.TaskTests
{
    public class TaskControllerIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
    {
        private readonly IServiceScope _scope;
        private readonly AlgoRhythmTestFixture _fixture;
        private readonly ApplicationDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        private readonly string _controllerRoute = "/api/task";

        public TaskControllerIntegrationTests(AlgoRhythmTestFixture fixture)
        {
            _fixture = fixture;
            _scope = fixture.ServerFactory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            _httpClient = fixture.ServerFactory.CreateClient();
        }

        private async Task<(string token, User user)> SetupAuthenticatedUser()
        {
            var userEmail = $"testuser-{Guid.NewGuid()}@example.com";
            var userPassword = "TestPassword123!";

            var token = await TestHelpers.SetupAuthenticatedUser(
                userEmail,
                userPassword,
                _roleManager,
                _userManager,
                _scope.ServiceProvider.GetRequiredService<IAuthService>()
            );

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var user = await _userManager.FindByEmailAsync(userEmail);
            return (token, user!);
        }

        private async Task<TaskItem> AddTaskToDb(string title, bool isPublished = true, TaskType taskType = TaskType.Programming)
        {
            TaskItem task;

            if (taskType == TaskType.Programming)
            {
                task = new ProgrammingTaskItem
                {
                    Title = title,
                    Description = "Test description",
                    Difficulty = Difficulty.Easy,
                    IsPublished = isPublished,
                    TemplateCode = "public class Solution { }"
                };
            }
            else
            {
                task = new InteractiveTaskItem
                {
                    Title = title,
                    Description = "Test description",
                    Difficulty = Difficulty.Easy,
                    IsPublished = isPublished,
                    OptionsJson = "[\"A\", \"B\", \"C\"]",
                    CorrectAnswer = "A"
                };
            }

            await _dbContext.AddAsync(task);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
            return task;
        }

        [Fact]
        public async Task GET_GetAll_WithoutAuth_Returns401_Unauthorized()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;

            var response = await _httpClient.GetAsync(_controllerRoute);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GET_GetAll_WithAuth_Returns200WithTasks()
        {
            await SetupAuthenticatedUser();

            await AddTaskToDb("Task 1", true);
            await AddTaskToDb("Task 2", false);

            var response = await _httpClient.GetAsync(_controllerRoute);

            response.EnsureSuccessStatusCode();
            var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>();

            Assert.NotNull(tasks);
            Assert.True(tasks.Count >= 2);
            Assert.Contains(tasks, t => t.Title == "Task 1");
            Assert.Contains(tasks, t => t.Title == "Task 2");
        }

        [Fact]
        public async Task GET_GetPublished_Returns200WithOnlyPublishedTasks()
        {
            await SetupAuthenticatedUser();

            var publishedTask = await AddTaskToDb("Published Task", true);
            await AddTaskToDb("Unpublished Task", false);

            var response = await _httpClient.GetAsync($"{_controllerRoute}/published");

            response.EnsureSuccessStatusCode();
            var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>();

            Assert.NotNull(tasks);
            Assert.Contains(tasks, t => t.Title == "Published Task" && t.IsPublished);
            Assert.DoesNotContain(tasks, t => t.Title == "Unpublished Task");
        }

        [Fact]
        public async Task GET_GetAllWithCourses_Returns200WithTasks()
        {
            await SetupAuthenticatedUser();

            await AddTaskToDb("Task with courses");

            var response = await _httpClient.GetAsync($"{_controllerRoute}/with-courses");

            response.EnsureSuccessStatusCode();
            var tasks = await response.Content.ReadFromJsonAsync<List<TaskWithCoursesDto>>();

            Assert.NotNull(tasks);
            Assert.NotEmpty(tasks);
        }

        [Fact]
        public async Task GET_GetById_ExistingTask_Returns200WithTask()
        {
            await SetupAuthenticatedUser();

            var task = await AddTaskToDb("Test Task");

            var response = await _httpClient.GetAsync($"{_controllerRoute}/{task.Id}");

            response.EnsureSuccessStatusCode();
            var taskDto = await response.Content.ReadFromJsonAsync<TaskDetailsDto>();

            Assert.NotNull(taskDto);
            Assert.Equal(task.Id, taskDto.Id);
            Assert.Equal("Test Task", taskDto.Title);
        }

        [Fact]
        public async Task GET_GetById_NonExistingTask_Returns404_NotFound()
        {
            await SetupAuthenticatedUser();

            var response = await _httpClient.GetAsync($"{_controllerRoute}/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task POST_Create_ValidProgrammingTask_Returns201WithTask()
        {
            await SetupAuthenticatedUser();

            var taskInput = new TaskInputDto
            {
                Title = "New Programming Task",
                Description = "Description",
                Difficulty = Difficulty.Medium,
                IsPublished = true,
                TaskType = TaskType.Programming,
                TemplateCode = "public class Solution { }"
            };

            var response = await _httpClient.PostAsJsonAsync(_controllerRoute, taskInput);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var createdTask = await response.Content.ReadFromJsonAsync<TaskDto>();

            Assert.NotNull(createdTask);
            Assert.Equal("New Programming Task", createdTask.Title);
            Assert.Equal(TaskType.Programming, createdTask.TaskType);
            Assert.Equal(Difficulty.Medium, createdTask.Difficulty);
        }

        [Fact]
        public async Task POST_Create_ValidInteractiveTask_Returns201WithTask()
        {
            await SetupAuthenticatedUser();

            var taskInput = new TaskInputDto
            {
                Title = "New Interactive Task",
                Description = "Description",
                Difficulty = Difficulty.Easy,
                IsPublished = true,
                TaskType = TaskType.Interactive,
                OptionsJson = "[\"Option A\", \"Option B\"]",
                CorrectAnswer = "Option A"
            };

            var response = await _httpClient.PostAsJsonAsync(_controllerRoute, taskInput);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var createdTask = await response.Content.ReadFromJsonAsync<TaskDto>();

            Assert.NotNull(createdTask);
            Assert.Equal("New Interactive Task", createdTask.Title);
            Assert.Equal(TaskType.Interactive, createdTask.TaskType);
        }

        [Fact]
        public async Task POST_Create_InvalidTaskType_Returns400_BadRequest()
        {
            await SetupAuthenticatedUser();

            var taskInput = new TaskInputDto
            {
                Title = "Invalid Task",
                Description = "Description",
                Difficulty = Difficulty.Easy,
                IsPublished = true,
                TaskType = (TaskType)999
            };

            var response = await _httpClient.PostAsJsonAsync(_controllerRoute, taskInput);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Update_ExistingTask_Returns204_NoContent()
        {
            await SetupAuthenticatedUser();

            var task = await AddTaskToDb("Original Title");

            var updateDto = new TaskInputDto
            {
                Title = "Updated Title",
                Description = "Updated Description",
                Difficulty = Difficulty.Hard,
                IsPublished = false,
                TaskType = TaskType.Programming,
                TemplateCode = "public class UpdatedSolution { }"
            };

            var response = await _httpClient.PutAsJsonAsync($"{_controllerRoute}/{task.Id}", updateDto);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            _dbContext.ChangeTracker.Clear();
            var updatedTask = await _dbContext.FindAsync<TaskItem>(task.Id);
            Assert.NotNull(updatedTask);
            Assert.Equal("Updated Title", updatedTask.Title);
        }

        [Fact]
        public async Task PUT_Update_NonExistingTask_Returns404_NotFound()
        {
            await SetupAuthenticatedUser();

            var updateDto = new TaskInputDto
            {
                Title = "Title",
                Description = "Description",
                Difficulty = Difficulty.Easy,
                IsPublished = true,
                TaskType = TaskType.Programming,
                TemplateCode = "code"
            };

            var response = await _httpClient.PutAsJsonAsync($"{_controllerRoute}/{Guid.NewGuid()}", updateDto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PUT_Update_ChangeTaskType_Returns400_BadRequest()
        {
            await SetupAuthenticatedUser();

            var task = await AddTaskToDb("Programming Task", true, TaskType.Programming);

            var updateDto = new TaskInputDto
            {
                Title = "Changed to Interactive",
                Description = "Description",
                Difficulty = Difficulty.Easy,
                IsPublished = true,
                TaskType = TaskType.Interactive,
                OptionsJson = "[\"A\"]",
                CorrectAnswer = "A"
            };

            var response = await _httpClient.PutAsJsonAsync($"{_controllerRoute}/{task.Id}", updateDto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DELETE_Delete_ExistingTask_Returns204_NoContent()
        {
            await SetupAuthenticatedUser();

            var task = await AddTaskToDb("Task to Delete");

            var response = await _httpClient.DeleteAsync($"{_controllerRoute}/{task.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task POST_AddTag_Returns204_NoContent()
        {
            await SetupAuthenticatedUser();

            var task = await AddTaskToDb("Task");
            var tagId = Guid.NewGuid();

            var response = await _httpClient.PostAsync($"{_controllerRoute}/{task.Id}/tags/{tagId}", null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DELETE_RemoveTag_Returns204_NoContent()
        {
            await SetupAuthenticatedUser();

            var task = await AddTaskToDb("Task");
            var tagId = Guid.NewGuid();

            var response = await _httpClient.DeleteAsync($"{_controllerRoute}/{task.Id}/tags/{tagId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task POST_AddHint_Returns204_NoContent()
        {
            await SetupAuthenticatedUser();

            var task = await AddTaskToDb("Task");
            var hintId = Guid.NewGuid();

            var response = await _httpClient.PostAsync($"{_controllerRoute}/{task.Id}/hints/{hintId}", null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DELETE_RemoveHint_Returns204_NoContent()
        {
            await SetupAuthenticatedUser();

            var task = await AddTaskToDb("Task");
            var hintId = Guid.NewGuid();

            var response = await _httpClient.DeleteAsync($"{_controllerRoute}/{task.Id}/hints/{hintId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}