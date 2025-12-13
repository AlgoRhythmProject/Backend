using AlgoRhythm.Clients;
using AlgoRhythm.Data;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos;
using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using AlgoRhythm.Shared.Models.Submissions;
using AlgoRhythm.Shared.Models.Tasks;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;


namespace IntegrationTests.CodeExecutorTests
{
    public class CodeExecutorIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
    {
        private readonly IServiceScope _scope;
        private readonly AlgoRhythmTestFixture _fixture;
        private readonly ApplicationDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager; 
        private readonly IAuthService _authService;

        public CodeExecutorIntegrationTests(AlgoRhythmTestFixture fixture)
        {
            _fixture = fixture;
            _scope = fixture.ServerFactory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>(); 
            _authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
            _httpClient = fixture.ServerFactory.CreateClient();
        }


        [Fact]
        public async Task Execute_Addition_Works_Through_Http()
        {
            CodeExecutorClient _client = _fixture.ServerFactory.Services.GetRequiredService<CodeExecutorClient>();
            // Arrange
            var request = new ExecuteCodeRequest
            {
                Code = @"using System;
                         public class Solution {
                         public int Solve(int a, int b) => a + b;
                     }",
                Args =
                [
                    new() { Name = "a", Value = "5" },
                    new() { Name = "b", Value = "7" }
                ],
                Timeout = TimeSpan.FromSeconds(1),
                ExpectedValue = "12",
                MaxPoints = 1
            };

            // Act
            var result = (await _client.ExecuteAsync([request]))!.First();

            // Assert
            Assert.True(result.Passed);
            Assert.Equal(request.ExpectedValue, result.ReturnedValue?.ToString());
            Assert.Equal(0, result.ExitCode);
        }


        [Fact]
        public async Task Submission_Controller_CorrectCode_Returns_Accepted()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var taskItem = new ProgrammingTaskItem
            {
                Id = taskId,
                Title = "Test Add Task",
                TemplateCode = "public class Solution { public int Solve(int a, int b) { } }",
                IsPublished = true,
                Difficulty = Difficulty.Easy
            };

            var testCase = new TestCase
            {
                ProgrammingTaskItemId = taskId,
                InputJson = "{\"a\": 10, \"b\": 2}",
                ExpectedJson = "12",
                MaxPoints = 10,
                IsVisible = false
            };

            taskItem.TestCases.Add(testCase);
            _dbContext.ProgrammingTaskItems.Add(taskItem);
            await _dbContext.SaveChangesAsync();

            var token = await TestHelpers.SetupAuthenticatedUser(
                TestConstants.TestUserEmail + Guid.NewGuid(), 
                TestConstants.TestUserPassword + Guid.NewGuid(),
                _roleManager,
                _userManager,
                _authService
            );

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var submissionRequest = new SubmitProgrammingRequestDto
            {
                TaskId = taskId,
                Code = "public class Solution { public int Solve(int a, int b) => a + b; }"
            };

            var content = JsonContent.Create(submissionRequest);
            var response = await _httpClient.PostAsync("/api/submissions/programming", content);

            // Debug
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(
                response.IsSuccessStatusCode,
                $"Expected success but got {response.StatusCode}. Response: {responseBody}"
            );

            // Wait for background processing
            await Task.Delay(2000);
            await _dbContext.SaveChangesAsync();

            Submission? submission = await _dbContext.Submissions
                .FirstOrDefaultAsync(s => s.TaskItemId == taskId);

            Assert.NotNull(submission);
            Assert.Equal(SubmissionStatus.Accepted, submission.Status);
        }

        [Fact]
        public async Task Submission_Controller_WrongCode_Returns_Rejected()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var taskItem = new ProgrammingTaskItem
            {
                Id = taskId,
                Title = "Test Add Task",
                TemplateCode = "public class Solution { public int Solve(int a, int b) { } }",
                IsPublished = true,
                Difficulty = Difficulty.Easy
            };

            var testCase = new TestCase
            {
                ProgrammingTaskItemId = taskId,
                InputJson = "{\"a\": 10, \"b\": 2}",
                ExpectedJson = "12",
                MaxPoints = 10,
                IsVisible = false
            };

            taskItem.TestCases.Add(testCase);
            _dbContext.ProgrammingTaskItems.Add(taskItem);
            await _dbContext.SaveChangesAsync();

            var token = await TestHelpers.SetupAuthenticatedUser(
                TestConstants.TestUserEmail + Guid.NewGuid(),
                TestConstants.TestUserPassword + Guid.NewGuid(),
                _roleManager,
                _userManager,
                _authService
            );

            _dbContext.ChangeTracker.Clear();

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var submissionRequest = new SubmitProgrammingRequestDto
            {
                TaskId = taskId,
                Code = "public class Solution { public int Solve(int a, int b) => a - b; }"
            };

            var content = JsonContent.Create(submissionRequest);
            var response = await _httpClient.PostAsync("/api/submissions/programming", content);

            // Debug
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(
                response.IsSuccessStatusCode,
                $"Expected success but got {response.StatusCode}. Response: {responseBody}"
            );

            // Wait for background processing
            await Task.Delay(5000);
            await _dbContext.SaveChangesAsync();

            Submission? submission = await _dbContext.Submissions
                .FirstOrDefaultAsync(s => s.TaskItemId == taskId);

            Assert.NotNull(submission);
            Assert.Equal(SubmissionStatus.Rejected, submission.Status);
        }

        [Fact]
        public async Task Submission_Controller_CompileError_Returns_Error()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var taskItem = new ProgrammingTaskItem
            {
                Id = taskId,
                Title = "Test Add Task",
                TemplateCode = "public class Solution { public int Solve(int a, int b) { } }",
                IsPublished = true,
                Difficulty = Difficulty.Easy
            };

            var testCase = new TestCase
            {
                ProgrammingTaskItemId = taskId,
                InputJson = "{\"a\": 10, \"b\": 2}",
                ExpectedJson = "12",
                MaxPoints = 10,
                IsVisible = false
            };

            taskItem.TestCases.Add(testCase);
            _dbContext.ProgrammingTaskItems.Add(taskItem);
            await _dbContext.SaveChangesAsync();

            var token = await TestHelpers.SetupAuthenticatedUser(
                TestConstants.TestUserEmail + Guid.NewGuid(),
                TestConstants.TestUserPassword + Guid.NewGuid(),
                _roleManager,
                _userManager,
                _authService
            );

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Act
            var submissionRequest = new SubmitProgrammingRequestDto
            {
                TaskId = taskId,
                Code = "public class Solution { public int Solve(int a, int b) => a - b "
            };

            var content = JsonContent.Create(submissionRequest);
            var response = await _httpClient.PostAsync("/api/submissions/programming", content);

            // Debug
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(
                response.IsSuccessStatusCode,
                $"Expected success but got {response.StatusCode}. Response: {responseBody}"
            );

            // Wait for background processing
            await Task.Delay(2000);
            await _dbContext.SaveChangesAsync();

            Submission? submission = await _dbContext.Submissions
                .FirstOrDefaultAsync(s => s.TaskItemId == taskId);

            Assert.NotNull(submission);
            Assert.Equal(SubmissionStatus.Error, submission.Status);
        }
    }
}