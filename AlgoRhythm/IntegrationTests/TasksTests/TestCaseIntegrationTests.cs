using AlgoRhythm.Data;
using AlgoRhythm.Services.Tasks.Interfaces;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Tasks;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Shared.Models.Tasks;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.TasksTests;

public class TestCaseIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
{
    private readonly IServiceScope _scope;
    private readonly AlgoRhythmTestFixture _fixture;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ITestCaseService _testCaseService;

    public TestCaseIntegrationTests(AlgoRhythmTestFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.ServerFactory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        _testCaseService = _scope.ServiceProvider.GetRequiredService<ITestCaseService>();
        _httpClient = fixture.ServerFactory.CreateClient();
    }

    private async Task<(string token, User user)> SetupAuthenticatedAdminUser()
    {
        var userEmail = $"admin-{Guid.NewGuid()}@example.com";
        var userPassword = "AdminPassword123!";

        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            await _roleManager.CreateAsync(new Role
            {
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "Administrator with full access"
            });
        }

        var user = new User
        {
            UserName = userEmail,
            Email = userEmail,
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, userPassword);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create admin user");
        }

        await _userManager.AddToRoleAsync(user, "Admin");

        var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
        var loginRequest = new LoginRequest(userEmail, userPassword);
        var authResponse = await authService.LoginAsync(loginRequest);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse.Token);

        return (authResponse.Token, user);
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

    private async Task<ProgrammingTaskItem> CreateTestTask()
    {
        var task = new ProgrammingTaskItem
        {
            Title = $"Test Task {Guid.NewGuid()}",
            Description = "Test Description",
            Difficulty = Difficulty.Easy,
            IsPublished = true,
            TemplateCode = "public class Solution { }"
        };

        await _dbContext.ProgrammingTaskItems.AddAsync(task);
        await _dbContext.SaveChangesAsync();
        return task;
    }

    [Fact]
    public async Task POST_CreateTestCase_AsAdmin_Returns201()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();
        var task = await CreateTestTask();

        var createDto = new CreateTestCaseDto
        {
            ProgrammingTaskItemId = task.Id,
            InputJson = "{\"a\": 5, \"b\": 10}",
            ExpectedJson = "15",
            IsVisible = true,
            MaxPoints = 10,
            Timeout = TimeSpan.FromSeconds(5)
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/testcase", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var testCase = await response.Content.ReadFromJsonAsync<TestCaseDto>();
        Assert.NotNull(testCase);
        Assert.Equal(task.Id, testCase.ProgrammingTaskItemId);
        Assert.Equal(createDto.InputJson, testCase.InputJson);
    }

    [Fact]
    public async Task POST_CreateTestCase_AsRegularUser_Returns403()
    {
        // Arrange
        await SetupAuthenticatedUser();
        var task = await CreateTestTask();

        var createDto = new CreateTestCaseDto
        {
            ProgrammingTaskItemId = task.Id,
            InputJson = "{\"a\": 5, \"b\": 10}",
            ExpectedJson = "15"
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/testcase", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_AllTestCases_AsAdmin_Returns200()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();
        var task = await CreateTestTask();

        await _testCaseService.CreateAsync(new CreateTestCaseDto
        {
            ProgrammingTaskItemId = task.Id,
            InputJson = "{\"a\": 1, \"b\": 2}",
            ExpectedJson = "3",
            MaxPoints = 10
        });

        // Act
        var response = await _httpClient.GetAsync("/api/testcase");

        // Assert
        response.EnsureSuccessStatusCode();
        var testCases = await response.Content.ReadFromJsonAsync<List<TestCaseDto>>();
        Assert.NotNull(testCases);
        Assert.NotEmpty(testCases);
    }

    [Fact]
    public async Task GET_TestCasesByTaskId_AsAdmin_Returns200()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();
        var task = await CreateTestTask();

        await _testCaseService.CreateAsync(new CreateTestCaseDto
        {
            ProgrammingTaskItemId = task.Id,
            InputJson = "{\"a\": 1, \"b\": 2}",
            ExpectedJson = "3"
        });

        await _testCaseService.CreateAsync(new CreateTestCaseDto
        {
            ProgrammingTaskItemId = task.Id,
            InputJson = "{\"a\": 5, \"b\": 10}",
            ExpectedJson = "15"
        });

        // Act
        var response = await _httpClient.GetAsync($"/api/testcase/task/{task.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var testCases = await response.Content.ReadFromJsonAsync<List<TestCaseDto>>();
        Assert.NotNull(testCases);
        Assert.Equal(2, testCases.Count);
    }

    [Fact]
    public async Task GET_TestCaseById_AsAdmin_Returns200()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();
        var task = await CreateTestTask();

        var created = await _testCaseService.CreateAsync(new CreateTestCaseDto
        {
            ProgrammingTaskItemId = task.Id,
            InputJson = "{\"a\": 1, \"b\": 2}",
            ExpectedJson = "3"
        });

        // Act
        var response = await _httpClient.GetAsync($"/api/testcase/{created.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var testCase = await response.Content.ReadFromJsonAsync<TestCaseDto>();
        Assert.NotNull(testCase);
        Assert.Equal(created.Id, testCase.Id);
    }

    [Fact]
    public async Task PUT_UpdateTestCase_AsAdmin_Returns200()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();
        var task = await CreateTestTask();

        var created = await _testCaseService.CreateAsync(new CreateTestCaseDto
        {
            ProgrammingTaskItemId = task.Id,
            InputJson = "{\"a\": 1, \"b\": 2}",
            ExpectedJson = "3",
            MaxPoints = 10
        });

        var updateDto = new UpdateTestCaseDto
        {
            InputJson = "{\"a\": 10, \"b\": 20}",
            ExpectedJson = "30",
            IsVisible = false,
            MaxPoints = 20,
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/api/testcase/{created.Id}", updateDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<TestCaseDto>();
        Assert.NotNull(updated);
        Assert.Equal(updateDto.InputJson, updated.InputJson);
        Assert.Equal(updateDto.ExpectedJson, updated.ExpectedJson);
        Assert.Equal(updateDto.MaxPoints, updated.MaxPoints);
    }

    [Fact]
    public async Task DELETE_TestCase_AsAdmin_Returns204()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();
        var task = await CreateTestTask();

        var created = await _testCaseService.CreateAsync(new CreateTestCaseDto
        {
            ProgrammingTaskItemId = task.Id,
            InputJson = "{\"a\": 1, \"b\": 2}",
            ExpectedJson = "3"
        });

        // Act
        var response = await _httpClient.DeleteAsync($"/api/testcase/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deleted
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _testCaseService.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task CreateTestCase_WithCustomTimeout_SavesCorrectly()
    {
        // Arrange
        var task = await CreateTestTask();

        var createDto = new CreateTestCaseDto
        {
            ProgrammingTaskItemId = task.Id,
            InputJson = "{\"a\": 1}",
            ExpectedJson = "1",
            Timeout = TimeSpan.FromSeconds(15)
        };

        // Act
        var created = await _testCaseService.CreateAsync(createDto);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(15), created.Timeout);
    }

    [Fact]
    public async Task CreateTestCase_WithoutTimeout_UsesNull()
    {
        // Arrange
        var task = await CreateTestTask();

        var createDto = new CreateTestCaseDto
        {
            ProgrammingTaskItemId = task.Id,
            InputJson = "{\"a\": 1}",
            ExpectedJson = "1",
            Timeout = null
        };

        // Act
        var created = await _testCaseService.CreateAsync(createDto);

        // Assert
        Assert.Null(created.Timeout);
    }

    [Fact]
    public async Task GetTestCasesByTaskId_NonExistentTask_Returns404()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();
        var nonExistentTaskId = Guid.NewGuid();

        // Act
        var response = await _httpClient.GetAsync($"/api/testcase/task/{nonExistentTaskId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}