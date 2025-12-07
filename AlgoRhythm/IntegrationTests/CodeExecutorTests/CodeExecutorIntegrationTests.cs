using AlgoRhythm.Clients;
using AlgoRhythm.Data;
using AlgoRhythm.Services.Interfaces;
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

        private async Task<string> SetupAuthenticatedUser(string email, string password)
        {
            // U¿yj _roleManager z pola klasy (z tego samego scope)
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new Role
                {
                    Name = "User",
                    NormalizedName = "USER",
                    Description = "Default user role"
                });
            }

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
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "User",
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await _userManager.AddToRoleAsync(user, "User");

            var loginRequest = new LoginRequest(email, password);
            var authResponse = await _authService.LoginAsync(loginRequest);

            return authResponse.Token;
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
        public async Task Execute_Submission_Via_Controller()
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

            var token = await SetupAuthenticatedUser("test.submission@test.com", "SecurePwd123!");
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
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Response: {responseBody}");

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
    }
}