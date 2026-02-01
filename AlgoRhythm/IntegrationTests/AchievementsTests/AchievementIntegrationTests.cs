using AlgoRhythm.Data;
using AlgoRhythm.Services.Achievements.Interfaces;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Achievements;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Shared.Models.Achievements;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Tasks;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.AchievementsTests;

public class AchievementIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
{
    private readonly IServiceScope _scope;
    private readonly AlgoRhythmTestFixture _fixture;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IAchievementService _achievementService;

    public AchievementIntegrationTests(AlgoRhythmTestFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.ServerFactory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        _achievementService = _scope.ServiceProvider.GetRequiredService<IAchievementService>();
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
                Description = "Administrator"
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

        await _userManager.CreateAsync(user, userPassword);
        await _userManager.AddToRoleAsync(user, "Admin");

        var authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
        var loginRequest = new LoginRequest(userEmail, userPassword);
        var authResponse = await authService.LoginAsync(loginRequest);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse.Token);

        return (authResponse.Token, user);
    }

    private async Task<Achievement> CreateTestAchievement()
    {
        var achievement = new Achievement
        {
            Name = $"Test Achievement {Guid.NewGuid()}",
            Description = "Test Description",
            IconPath = "/icons/test.png",
            Requirements = new List<Requirement>
            {
                new()
                {
                    Description = "Complete 5 tasks",
                    Condition = new RequirementCondition
                    {
                        Type = RequirementType.CompleteTasks,
                        TargetValue = 5
                    }
                }
            }
        };

        await _dbContext.Achievements.AddAsync(achievement);
        await _dbContext.SaveChangesAsync();
        return achievement;
    }

    [Fact]
    public async Task GET_AllAchievements_Returns200()
    {
        // Arrange
        await SetupAuthenticatedUser();
        await CreateTestAchievement();

        // Act
        var response = await _httpClient.GetAsync("/api/achievement");

        // Assert
        response.EnsureSuccessStatusCode();
        var achievements = await response.Content.ReadFromJsonAsync<List<AchievementDto>>();
        Assert.NotNull(achievements);
        Assert.NotEmpty(achievements);
    }

    [Fact]
    public async Task GET_AchievementById_Returns200()
    {
        // Arrange
        await SetupAuthenticatedUser();
        var achievement = await CreateTestAchievement();

        // Act
        var response = await _httpClient.GetAsync($"/api/achievement/{achievement.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AchievementDto>();
        Assert.NotNull(result);
        Assert.Equal(achievement.Id, result.Id);
    }

    [Fact]
    public async Task GET_MyAchievements_ReturnsUserProgress()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var achievement = await CreateTestAchievement();

        await _achievementService.InitializeAchievementsForUserAsync(user.Id, CancellationToken.None);

        // Act
        var response = await _httpClient.GetAsync("/api/achievement/my-achievements");

        // Assert
        response.EnsureSuccessStatusCode();
        var achievements = await response.Content.ReadFromJsonAsync<List<UserAchievementDto>>();
        Assert.NotNull(achievements);
        Assert.NotEmpty(achievements);
        Assert.All(achievements, a => Assert.Equal(user.Id, a.UserId));
    }

    [Fact]
    public async Task GET_MyAchievement_SpecificAchievement_Returns200()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var achievement = await CreateTestAchievement();

        await _achievementService.InitializeAchievementsForUserAsync(user.Id, CancellationToken.None);

        // Act
        var response = await _httpClient.GetAsync($"/api/achievement/my-achievements/{achievement.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UserAchievementDto>();
        Assert.NotNull(result);
        Assert.Equal(achievement.Id, result.AchievementId);
        Assert.False(result.IsCompleted);
    }

    [Fact]
    public async Task GET_MyEarnedAchievements_OnlyReturnsCompleted()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var achievement = await CreateTestAchievement();

        await _achievementService.InitializeAchievementsForUserAsync(user.Id, CancellationToken.None);

        // Act - Get earned (should be empty)
        var response = await _httpClient.GetAsync("/api/achievement/my-earned");

        // Assert
        response.EnsureSuccessStatusCode();
        var earned = await response.Content.ReadFromJsonAsync<List<EarnedAchievementDto>>();
        Assert.NotNull(earned);
        Assert.Empty(earned); // No achievements completed yet
    }

    [Fact]
    public async Task POST_RefreshAchievements_Updates_Progress()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var achievement = await CreateTestAchievement();

        await _achievementService.InitializeAchievementsForUserAsync(user.Id, CancellationToken.None);

        // Complete some tasks
        var task1 = new ProgrammingTaskItem { Title = "Task 1", Difficulty = Difficulty.Easy, IsPublished = true, TemplateCode = "" };
        var task2 = new ProgrammingTaskItem { Title = "Task 2", Difficulty = Difficulty.Easy, IsPublished = true, TemplateCode = "" };
        await _dbContext.ProgrammingTaskItems.AddRangeAsync(task1, task2);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();
        var userEntity = await _dbContext.Users.FindAsync(user.Id);
        userEntity!.CompletedTasks.Add(task1);
        userEntity.CompletedTasks.Add(task2);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _httpClient.PostAsync("/api/achievement/refresh", null);

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify progress updated
        var userAchievement = await _achievementService.GetUserAchievementAsync(user.Id, achievement.Id, CancellationToken.None);
        Assert.NotNull(userAchievement);
        Assert.True(userAchievement.RequirementProgresses.Any(rp => rp.CurrentValue == 2));
    }

    [Fact]
    public async Task POST_CreateAchievement_AsAdmin_Returns201()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();

        var createDto = new CreateAchievementDto
        {
            Name = "New Achievement",
            Description = "New Description",
            IconPath = "/icons/new.png",
            Requirements = new List<CreateRequirementDto>
            {
                new()
                {
                    Description = "Complete 10 lectures",
                    Type = "CompleteLectures",
                    TargetValue = 10
                }
            }
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/achievement", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AchievementDto>();
        Assert.NotNull(created);
        Assert.Equal(createDto.Name, created.Name);
    }

    [Fact]
    public async Task POST_CreateAchievement_AsRegularUser_Returns403()
    {
        // Arrange
        await SetupAuthenticatedUser();

        var createDto = new CreateAchievementDto
        {
            Name = "New Achievement",
            Description = "Description"
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/achievement", createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PUT_UpdateAchievement_AsAdmin_Returns200()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();
        var achievement = await CreateTestAchievement();

        var updateDto = new UpdateAchievementDto
        {
            Name = "Updated Name",
            Description = "Updated Description"
        };

        // Act
        var response = await _httpClient.PutAsJsonAsync($"/api/achievement/{achievement.Id}", updateDto);

        // Assert
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<AchievementDto>();
        Assert.NotNull(updated);
        Assert.Equal(updateDto.Name, updated.Name);
    }

    [Fact]
    public async Task DELETE_Achievement_AsAdmin_Returns204()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();
        var achievement = await CreateTestAchievement();

        // Act
        var response = await _httpClient.DeleteAsync($"/api/achievement/{achievement.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deleted
        var deleted = await _achievementService.GetAchievementByIdAsync(achievement.Id, CancellationToken.None);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task InitializeAchievements_CreatesUserAchievements()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var achievement = await CreateTestAchievement();

        // Act
        await _achievementService.InitializeAchievementsForUserAsync(user.Id, CancellationToken.None);

        // Assert
        var userAchievements = await _achievementService.GetUserAchievementsAsync(user.Id, CancellationToken.None);
        Assert.NotEmpty(userAchievements);
        Assert.Contains(userAchievements, ua => ua.AchievementId == achievement.Id);
    }

    [Fact]
    public async Task CompleteRequirement_MarksAchievementAsEarned()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var achievement = await CreateTestAchievement(); // Requires 5 tasks

        await _achievementService.InitializeAchievementsForUserAsync(user.Id, CancellationToken.None);

        // Complete 5 tasks
        for (int i = 0; i < 5; i++)
        {
            var task = new ProgrammingTaskItem
            {
                Title = $"Task {i}",
                Difficulty = Difficulty.Easy,
                IsPublished = true,
                TemplateCode = ""
            };
            await _dbContext.ProgrammingTaskItems.AddAsync(task);
            await _dbContext.SaveChangesAsync();

            _dbContext.ChangeTracker.Clear();
            var userEntity = await _dbContext.Users.FindAsync(user.Id);
            userEntity!.CompletedTasks.Add(task);
            await _dbContext.SaveChangesAsync();
        }

        // Act
        await _achievementService.CheckAndUpdateAchievementsAsync(user.Id, CancellationToken.None);

        // Assert
        var userAchievement = await _achievementService.GetUserAchievementAsync(user.Id, achievement.Id, CancellationToken.None);
        Assert.NotNull(userAchievement);
        Assert.True(userAchievement.IsCompleted);
        Assert.NotNull(userAchievement.EarnedAt);
    }

    [Fact]
    public async Task POST_RecalculateUserAchievements_AsAdmin_Returns200()
    {
        // Arrange
        var (_, adminUser) = await SetupAuthenticatedAdminUser();
        
        var regularUserEmail = $"regular-{Guid.NewGuid()}@example.com";
        var regularUserPassword = "UserPassword123!";

        if (!await _roleManager.RoleExistsAsync("User"))
        {
            await _roleManager.CreateAsync(new Role
            {
                Name = "User",
                NormalizedName = "USER",
                Description = "Regular user"
            });
        }

        var regularUser = new User
        {
            UserName = regularUserEmail,
            Email = regularUserEmail,
            FirstName = "Regular",
            LastName = "User",
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(regularUser, regularUserPassword);
        Assert.True(createResult.Succeeded, "Failed to create regular user");
        
        await _userManager.AddToRoleAsync(regularUser, "User");

        var achievement = await CreateTestAchievement();
        await _achievementService.InitializeAchievementsForUserAsync(regularUser.Id, CancellationToken.None);

        // Act
        var response = await _httpClient.PostAsync($"/api/achievement/admin/recalculate/{regularUser.Id}", null);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CheckAndUpdate_LoginStreakRequirement_WorksCorrectly()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();

        var streakAchievement = new Achievement
        {
            Name = "Streak Master",
            Description = "Login 5 days in a row",
            IconPath = "/icons/streak.png",
            Requirements = new List<Requirement>
            {
                new()
                {
                    Description = "5 day login streak",
                    Condition = new RequirementCondition
                    {
                        Type = RequirementType.LoginStreak,
                        TargetValue = 5
                    }
                }
            }
        };

        await _dbContext.Achievements.AddAsync(streakAchievement);
        await _dbContext.SaveChangesAsync();

        await _achievementService.InitializeAchievementsForUserAsync(user.Id, CancellationToken.None);

        // Set user streak
        _dbContext.ChangeTracker.Clear();
        var userEntity = await _dbContext.Users.FindAsync(user.Id);
        userEntity!.CurrentStreak = 5;
        await _dbContext.SaveChangesAsync();

        // Act
        await _achievementService.CheckAndUpdateAchievementsAsync(user.Id, CancellationToken.None);

        // Assert
        var userAchievement = await _achievementService.GetUserAchievementAsync(user.Id, streakAchievement.Id, CancellationToken.None);
        Assert.NotNull(userAchievement);
        Assert.True(userAchievement.IsCompleted);
    }
}