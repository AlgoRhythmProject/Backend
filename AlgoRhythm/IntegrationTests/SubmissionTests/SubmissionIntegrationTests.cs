using AlgoRhythm.Data;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Shared.Models.Tasks;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace IntegrationTests.SubmissionsTests;

public class SubmissionIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
{
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly UserManager<User> _userManager;
    private readonly IAuthService _authService;

    public SubmissionIntegrationTests(AlgoRhythmTestFixture fixture)
    {
        _scope = fixture.ServerFactory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
        _httpClient = fixture.ServerFactory.CreateClient();
    }

    private async Task<string> SetupAuthenticatedUser()
    {
        var email = $"user-{Guid.NewGuid()}@test.com";
        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        await _userManager.CreateAsync(user, TestConstants.TestUserPassword);
        await _userManager.AddToRoleAsync(user, "User");

        var loginResponse = await _authService.LoginAsync(new LoginRequest(email, TestConstants.TestUserPassword));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.Token);

        return loginResponse.Token;
    }

    private async Task<Guid> CreateTestTask()
    {
        var task = new ProgrammingTaskItem
        {
            Title = $"Test Task {Guid.NewGuid()}",
            Description = "Test programming task",
            Difficulty = Difficulty.Easy,
            IsPublished = true,
            TemplateCode = @"public class Solution 
            { 
                public int Solve(int a, int b) 
                { 
                    return 0; 
                } 
            }"
        };

        var testCase = new TestCase
        {
            ProgrammingTaskItem = task,
            InputJson = "{\"a\": 5, \"b\": 3}",
            ExpectedJson = "8",
            IsVisible = true
        };

        await _dbContext.ProgrammingTaskItems.AddAsync(task);
        await _dbContext.TestCases.AddAsync(testCase);
        await _dbContext.SaveChangesAsync();

        return task.Id;
    }

    [Fact]
    public async Task POST_SubmitProgramming_ValidCode_Returns200WithResults()
    {
        await SetupAuthenticatedUser();
        var taskId = await CreateTestTask();

        var submission = new SubmitProgrammingRequestDto
        {
            TaskId = taskId,
            Code = @"public class Solution 
            { 
                public int Solve(int a, int b) 
                { 
                    return a + b; 
                } 
            }"
        };

        var content = new StringContent(JsonConvert.SerializeObject(submission), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/submissions/programming", content);

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<SubmissionResponseDto>(responseBody);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.SubmissionId);
        Assert.Equal(taskId, result.TaskItemId);
        Assert.NotNull(result.Status);
    }

    [Fact]
    public async Task POST_SubmitProgramming_IncorrectCode_ReturnsFailedSubmission()
    {
        await SetupAuthenticatedUser();
        var taskId = await CreateTestTask();

        var submission = new SubmitProgrammingRequestDto
        {
            TaskId = taskId,
            Code = @"public class Solution 
            { 
                public int Solve(int a, int b) 
                { 
                    return a - b; // Wrong operation
                } 
            }"
        };

        var content = new StringContent(JsonConvert.SerializeObject(submission), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/submissions/programming", content);

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<SubmissionResponseDto>(responseBody);

        Assert.NotNull(result);
        Assert.False(result.IsSolved);
    }


    [Fact]
    public async Task POST_SubmitProgramming_Unauthorized_Returns401()
    {
        var taskId = await CreateTestTask();

        var submission = new SubmitProgrammingRequestDto
        {
            TaskId = taskId,
            Code = "public class Solution { }"
        };

        var content = new StringContent(JsonConvert.SerializeObject(submission), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/submissions/programming", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_SubmitProgramming_InvalidTaskId_Returns400()
    {
        await SetupAuthenticatedUser();

        var submission = new SubmitProgrammingRequestDto
        {
            TaskId = Guid.NewGuid(), // Non-existent task
            Code = "public class Solution { }"
        };

        var content = new StringContent(JsonConvert.SerializeObject(submission), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/submissions/programming", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GET_Submission_ValidId_Returns200()
    {
        await SetupAuthenticatedUser();
        var taskId = await CreateTestTask();

        // First, create a submission
        var submission = new SubmitProgrammingRequestDto
        {
            TaskId = taskId,
            Code = @"public class Solution 
            { 
                public int Solve(int a, int b) 
                { 
                    return a + b; 
                } 
            }"
        };

        var submitContent = new StringContent(JsonConvert.SerializeObject(submission), Encoding.UTF8, "application/json");
        var submitResponse = await _httpClient.PostAsync("/api/submissions/programming", submitContent);
        var submitResult = JsonConvert.DeserializeObject<SubmissionResponseDto>(await submitResponse.Content.ReadAsStringAsync());

        // Then, retrieve it
        var getResponse = await _httpClient.GetAsync($"/api/submissions/{submitResult!.SubmissionId}");

        getResponse.EnsureSuccessStatusCode();
        var responseBody = await getResponse.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<SubmissionResponseDto>(responseBody);

        Assert.NotNull(result);
        Assert.Equal(submitResult.SubmissionId, result.SubmissionId);
        Assert.Equal(taskId, result.TaskItemId);
    }

    [Fact]
    public async Task GET_Submission_InvalidId_Returns404()
    {
        await SetupAuthenticatedUser();

        var response = await _httpClient.GetAsync($"/api/submissions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_Submission_Unauthorized_Returns401()
    {
        var response = await _httpClient.GetAsync($"/api/submissions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}