using AlgoRhythm.Data;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Tasks;
using AlgoRhythm.Shared.Models.Tasks;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace IntegrationTests.TasksTests;

public class HintIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
{
    private readonly IServiceScope _scope;
    private readonly AlgoRhythmTestFixture _fixture;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    private readonly Guid _testTaskId = Guid.NewGuid();

    public HintIntegrationTests(AlgoRhythmTestFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.ServerFactory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        _httpClient = fixture.ServerFactory.CreateClient();

        // Create a test task for hints
        var testTask = new ProgrammingTaskItem
        {
            Id = _testTaskId,
            Title = "Test Task for Hints",
            IsPublished = true,
            Difficulty = Difficulty.Easy
        };
        _dbContext.ProgrammingTaskItems.Add(testTask);
        _dbContext.SaveChanges();
    }

    private async Task<(string token, User user)> SetupAuthenticatedAdminUser()
    {
        var userEmail = $"admin-{Guid.NewGuid()}@example.com";
        var userPassword = "AdminPassword123!";

        // Ensure Admin role exists
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
            throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await _userManager.AddToRoleAsync(user, "Admin");

        var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
        var loginRequest = new AlgoRhythm.Shared.Dtos.Users.LoginRequest(userEmail, userPassword);
        var authResponse = await authService.LoginAsync(loginRequest);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse.Token);

        return (authResponse.Token, user);
    }

    private async Task<Hint> AddHintToDb(Guid taskId, string content, int order = 0)
    {
        var hint = new Hint
        {
            TaskItemId = taskId,
            Content = content,
            Order = order
        };
        await _dbContext.Hints.AddAsync(hint);
        await _dbContext.SaveChangesAsync();
        return hint;
    }

    [Fact]
    public async Task GET_GetByTaskId_Returns200WithHints()
    {
        var (_, _) = await SetupAuthenticatedAdminUser();
        await AddHintToDb(_testTaskId, "Hint 1", 0);
        await AddHintToDb(_testTaskId, "Hint 2", 1);

        var response = await _httpClient.GetAsync($"/api/hint/task/{_testTaskId}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var hints = JsonConvert.DeserializeObject<List<HintDto>>(responseBody);

        Assert.NotNull(hints);
        Assert.True(hints.Count >= 2);
        Assert.All(hints, h => Assert.Equal(_testTaskId, h.TaskItemId));
    }

    [Fact]
    public async Task GET_GetById_ValidId_Returns200WithHint()
    {
        await SetupAuthenticatedAdminUser();
        var hint = await AddHintToDb(_testTaskId, "Test Hint");

        var response = await _httpClient.GetAsync($"/api/hint/{hint.Id}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var hintDto = JsonConvert.DeserializeObject<HintDto>(responseBody);

        Assert.NotNull(hintDto);
        Assert.Equal(hint.Id, hintDto.Id);
        Assert.Equal(hint.Content, hintDto.Content);
    }

    [Fact]
    public async Task GET_GetById_InvalidId_Returns404()
    {
        await SetupAuthenticatedAdminUser();
        var response = await _httpClient.GetAsync($"/api/hint/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_Create_ValidInput_Returns201AndCreatedHint()
    {
        await SetupAuthenticatedAdminUser();
        var inputDto = new HintInputDto
        {
            TaskItemId = _testTaskId,
            Content = "New Hint Content",
            Order = 0
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(inputDto), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/hint", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var hintDto = JsonConvert.DeserializeObject<HintDto>(responseBody);

        Assert.NotNull(hintDto);
        Assert.Equal(inputDto.Content, hintDto.Content);
        Assert.Equal(inputDto.TaskItemId, hintDto.TaskItemId);

        _dbContext.ChangeTracker.Clear();
        var dbHint = await _dbContext.Hints.FindAsync(hintDto.Id);
        Assert.NotNull(dbHint);
    }

    [Fact]
    public async Task PUT_Update_ValidInput_Returns204()
    {
        await SetupAuthenticatedAdminUser();
        var hint = await AddHintToDb(_testTaskId, "Original Content", 0);
        var updateDto = new HintInputDto
        {
            TaskItemId = _testTaskId,
            Content = "Updated Content",
            Order = 1
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"/api/hint/{hint.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var updatedHint = await _dbContext.Hints.FindAsync(hint.Id);
        Assert.NotNull(updatedHint);
        Assert.Equal(updateDto.Content, updatedHint.Content);
        Assert.Equal(updateDto.Order, updatedHint.Order);
    }

    [Fact]
    public async Task PUT_Update_InvalidId_Returns404()
    {
        await SetupAuthenticatedAdminUser();
        var updateDto = new HintInputDto
        {
            TaskItemId = _testTaskId,
            Content = "Updated Content",
            Order = 1
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"/api/hint/{Guid.NewGuid()}", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Delete_ValidId_Returns204()
    {
        await SetupAuthenticatedAdminUser();
        var hint = await AddHintToDb(_testTaskId, "To Delete");

        var response = await _httpClient.DeleteAsync($"/api/hint/{hint.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var deletedHint = await _dbContext.Hints.FindAsync(hint.Id);
        Assert.Null(deletedHint);
    }

    [Fact]
    public async Task GET_HintsByTaskId_OrderedCorrectly()
    {
        await SetupAuthenticatedAdminUser();
        await AddHintToDb(_testTaskId, "Hint 3", 2);
        await AddHintToDb(_testTaskId, "Hint 1", 0);
        await AddHintToDb(_testTaskId, "Hint 2", 1);

        var response = await _httpClient.GetAsync($"/api/hint/task/{_testTaskId}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var hints = JsonConvert.DeserializeObject<List<HintDto>>(responseBody);

        Assert.NotNull(hints);
        Assert.True(hints.Count >= 3);

        var taskHints = hints.Where(h => h.TaskItemId == _testTaskId).OrderBy(h => h.Order).ToList();
        for (int i = 0; i < taskHints.Count - 1; i++)
        {
            Assert.True(taskHints[i].Order <= taskHints[i + 1].Order);
        }
    }

    [Fact]
    public async Task GET_WithoutAuth_Returns401()
    {
        // Don't set authorization header
        _httpClient.DefaultRequestHeaders.Authorization = null;

        var response = await _httpClient.GetAsync("/api/hint/task/" + Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_Create_WithoutAdminRole_Returns403()
    {
        // Create regular user (not admin)
        var userEmail = $"user-{Guid.NewGuid()}@example.com";
        var userPassword = "UserPassword123!";

        var token = await TestHelpers.SetupAuthenticatedUser(
            userEmail,
            userPassword,
            _roleManager,
            _userManager,
            _scope.ServiceProvider.GetRequiredService<IAuthService>()
        );

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var inputDto = new HintInputDto
        {
            TaskItemId = _testTaskId,
            Content = "New Hint Content",
            Order = 0
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(inputDto), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/hint", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}