using AlgoRhythm.Data;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.UsersTests;

public class UserStreakIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
{
    private readonly IServiceScope _scope;
    private readonly AlgoRhythmTestFixture _fixture;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IUserStreakService _streakService;

    public UserStreakIntegrationTests(AlgoRhythmTestFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.ServerFactory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        _streakService = _scope.ServiceProvider.GetRequiredService<IUserStreakService>();
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
        var loginRequest = new LoginRequest(userEmail, userPassword);
        var authResponse = await authService.LoginAsync(loginRequest);

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse.Token);

        return (authResponse.Token, user);
    }

    private async Task<User> CreateUserWithoutAuth(string? role = "User")
    {
        var userEmail = $"user-{Guid.NewGuid()}@example.com";
        var userPassword = "UserPassword123!";

        if (role == "User" && !await _roleManager.RoleExistsAsync("User"))
        {
            await _roleManager.CreateAsync(new Role
            {
                Name = "User",
                NormalizedName = "USER",
                Description = "Regular user"
            });
        }

        var user = new User
        {
            UserName = userEmail,
            Email = userEmail,
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, userPassword);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (role != null)
        {
            await _userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    [Fact]
    public async Task UpdateLoginStreak_FirstLogin_SetsStreakToOne()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        
        // Act
        await _streakService.UpdateLoginStreakAsync(user.Id);

        // Assert
        _dbContext.ChangeTracker.Clear();
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        
        Assert.NotNull(updatedUser);
        Assert.Equal(1, updatedUser.CurrentStreak);
        Assert.Equal(1, updatedUser.LongestStreak);
        Assert.NotNull(updatedUser.LastLoginDate);
        Assert.Equal(DateTime.UtcNow.Date, updatedUser.LastLoginDate.Value.Date);
    }

    [Fact]
    public async Task UpdateLoginStreak_ConsecutiveDays_IncrementsStreak()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        
        // First login
        await _streakService.UpdateLoginStreakAsync(user.Id);
        
        // Manually set last login to yesterday
        _dbContext.ChangeTracker.Clear();
        user = await _dbContext.Users.FindAsync(user.Id);
        user!.LastLoginDate = DateTime.UtcNow.Date.AddDays(-1);
        await _dbContext.SaveChangesAsync();

        // Act - Second login (today)
        await _streakService.UpdateLoginStreakAsync(user.Id);

        // Assert
        _dbContext.ChangeTracker.Clear();
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        
        Assert.NotNull(updatedUser);
        Assert.Equal(2, updatedUser.CurrentStreak);
        Assert.Equal(2, updatedUser.LongestStreak);
        Assert.Equal(DateTime.UtcNow.Date, updatedUser.LastLoginDate!.Value.Date);
    }

    [Fact]
    public async Task UpdateLoginStreak_SkippedDay_ResetsStreakToOne()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        
        // Set up a streak of 5 days
        user.CurrentStreak = 5;
        user.LongestStreak = 5;
        user.LastLoginDate = DateTime.UtcNow.Date.AddDays(-3); // Skipped 2 days
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        await _streakService.UpdateLoginStreakAsync(user.Id);

        // Assert
        _dbContext.ChangeTracker.Clear();
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        
        Assert.NotNull(updatedUser);
        Assert.Equal(1, updatedUser.CurrentStreak); // Reset to 1
        Assert.Equal(5, updatedUser.LongestStreak); // Longest stays the same
        Assert.Equal(DateTime.UtcNow.Date, updatedUser.LastLoginDate!.Value.Date);
    }

    [Fact]
    public async Task UpdateLoginStreak_SameDay_NoChange()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        
        // First login
        await _streakService.UpdateLoginStreakAsync(user.Id);
        
        _dbContext.ChangeTracker.Clear();
        var userBeforeSecondLogin = await _dbContext.Users.FindAsync(user.Id);
        var streakBefore = userBeforeSecondLogin!.CurrentStreak;
        var lastLoginBefore = userBeforeSecondLogin.LastLoginDate;

        // Act - Second login same day
        await _streakService.UpdateLoginStreakAsync(user.Id);

        // Assert
        _dbContext.ChangeTracker.Clear();
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        
        Assert.NotNull(updatedUser);
        Assert.Equal(streakBefore, updatedUser.CurrentStreak);
        Assert.Equal(lastLoginBefore, updatedUser.LastLoginDate);
    }

    [Fact]
    public async Task UpdateLoginStreak_UpdatesLongestStreak_WhenCurrentExceedsLongest()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        
        user.CurrentStreak = 5;
        user.LongestStreak = 5;
        user.LastLoginDate = DateTime.UtcNow.Date.AddDays(-1);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        await _streakService.UpdateLoginStreakAsync(user.Id);

        // Assert
        _dbContext.ChangeTracker.Clear();
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        
        Assert.NotNull(updatedUser);
        Assert.Equal(6, updatedUser.CurrentStreak);
        Assert.Equal(6, updatedUser.LongestStreak); // Updated to match current
    }

    [Fact]
    public async Task UpdateLoginStreak_DoesNotUpdateLongestStreak_WhenCurrentIsLower()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        
        user.CurrentStreak = 3;
        user.LongestStreak = 10; // Previous best
        user.LastLoginDate = DateTime.UtcNow.Date.AddDays(-1);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        await _streakService.UpdateLoginStreakAsync(user.Id);

        // Assert
        _dbContext.ChangeTracker.Clear();
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        
        Assert.NotNull(updatedUser);
        Assert.Equal(4, updatedUser.CurrentStreak);
        Assert.Equal(10, updatedUser.LongestStreak); // Stays the same
    }

    [Fact]
    public async Task GetUserStreak_ReturnsCorrectData()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        
        user.CurrentStreak = 7;
        user.LongestStreak = 15;
        user.LastLoginDate = DateTime.UtcNow.Date;
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var streak = await _streakService.GetUserStreakAsync(user.Id);

        // Assert
        Assert.NotNull(streak);
        Assert.Equal(user.Id, streak.UserId);
        Assert.Equal(7, streak.CurrentStreak);
        Assert.Equal(15, streak.LongestStreak);
        Assert.Equal(DateTime.UtcNow.Date, streak.LastLoginDate!.Value.Date);
    }

    [Fact]
    public async Task GetUserStreak_UserNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _streakService.GetUserStreakAsync(nonExistentUserId));
    }

    [Fact]
    public async Task GET_MyStreak_AuthenticatedUser_Returns200WithStreak()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        
        user.CurrentStreak = 5;
        user.LongestStreak = 10;
        user.LastLoginDate = DateTime.UtcNow.Date;
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _httpClient.GetAsync("/api/userstreak/my-streak");

        // Assert
        response.EnsureSuccessStatusCode();
        var streak = await response.Content.ReadFromJsonAsync<UserStreakDto>();
        
        Assert.NotNull(streak);
        Assert.Equal(user.Id, streak.UserId);
        Assert.Equal(5, streak.CurrentStreak);
        Assert.Equal(10, streak.LongestStreak);
    }

    [Fact]
    public async Task GET_MyStreak_Unauthenticated_Returns401()
    {
        // Arrange
        _httpClient.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _httpClient.GetAsync("/api/userstreak/my-streak");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GET_UserStreak_AsAdmin_Returns200WithStreak()
    {
        // Arrange
        var (_, adminUser) = await SetupAuthenticatedAdminUser();
        
        // Create target user without changing authentication
        var targetUser = await CreateUserWithoutAuth();
        targetUser.CurrentStreak = 8;
        targetUser.LongestStreak = 12;
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _httpClient.GetAsync($"/api/userstreak/{targetUser.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var streak = await response.Content.ReadFromJsonAsync<UserStreakDto>();
        
        Assert.NotNull(streak);
        Assert.Equal(targetUser.Id, streak.UserId);
        Assert.Equal(8, streak.CurrentStreak);
        Assert.Equal(12, streak.LongestStreak);
    }

    [Fact]
    public async Task GET_UserStreak_AsRegularUser_Returns403()
    {
        // Arrange
        await SetupAuthenticatedUser(); // Regular user logged in
        
        // Create another user to try to access
        var targetUser = await CreateUserWithoutAuth();

        // Act
        var response = await _httpClient.GetAsync($"/api/userstreak/{targetUser.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task POST_UpdateStreak_AsAdmin_Returns200()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();
        var targetUser = await CreateUserWithoutAuth();

        // Act
        var response = await _httpClient.PostAsync($"/api/userstreak/{targetUser.Id}/update", null);

        // Assert
        response.EnsureSuccessStatusCode();
        
        _dbContext.ChangeTracker.Clear();
        var updatedUser = await _dbContext.Users.FindAsync(targetUser.Id);
        Assert.Equal(1, updatedUser!.CurrentStreak);
    }

    [Fact]
    public async Task POST_UpdateStreak_AsRegularUser_Returns403()
    {
        // Arrange
        var (_, user1) = await SetupAuthenticatedUser(); // Regular user
        var user2 = await CreateUserWithoutAuth();

        // Act
        var response = await _httpClient.PostAsync($"/api/userstreak/{user2.Id}/update", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_UpdatesStreak_Automatically()
    {
        // Arrange
        var userEmail = $"logintest-{Guid.NewGuid()}@example.com";
        var userPassword = "TestPassword123!";

        // Ensure User role exists
        if (!await _roleManager.RoleExistsAsync("User"))
        {
            await _roleManager.CreateAsync(new Role
            {
                Name = "User",
                NormalizedName = "USER",
                Description = "Regular user"
            });
        }

        // Create user manually
        var user = new User
        {
            UserName = userEmail,
            Email = userEmail,
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true
        };
        await _userManager.CreateAsync(user, userPassword);
        await _userManager.AddToRoleAsync(user, "User");

        // Act - Login
        var loginRequest = new LoginRequest(userEmail, userPassword);
        var loginResponse = await _httpClient.PostAsJsonAsync("/api/authentication/login", loginRequest);

        // Assert
        loginResponse.EnsureSuccessStatusCode();
        
        _dbContext.ChangeTracker.Clear();
        var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        
        Assert.NotNull(updatedUser);
        Assert.Equal(1, updatedUser.CurrentStreak);
        Assert.Equal(DateTime.UtcNow.Date, updatedUser.LastLoginDate!.Value.Date);
    }

    [Fact]
    public async Task MultipleConsecutiveLogins_BuildsStreak()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();

        // Simulate 5 consecutive days
        for (int i = 0; i < 5; i++)
        {
            _dbContext.ChangeTracker.Clear();
            var currentUser = await _dbContext.Users.FindAsync(user.Id);
            
            if (i > 0)
            {
                // Set last login to previous day
                currentUser!.LastLoginDate = DateTime.UtcNow.Date.AddDays(-1);
                await _dbContext.SaveChangesAsync();
            }

            // Act
            await _streakService.UpdateLoginStreakAsync(user.Id);
        }

        // Assert
        _dbContext.ChangeTracker.Clear();
        var finalUser = await _dbContext.Users.FindAsync(user.Id);
        
        Assert.NotNull(finalUser);
        Assert.Equal(5, finalUser.CurrentStreak);
        Assert.Equal(5, finalUser.LongestStreak);
    }

    [Fact]
    public async Task StreakBreak_ThenRebuild_MaintainsLongestStreak()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();

        // Build initial streak of 10
        user.CurrentStreak = 10;
        user.LongestStreak = 10;
        user.LastLoginDate = DateTime.UtcNow.Date.AddDays(-5); // Break the streak
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act - Start new streak
        await _streakService.UpdateLoginStreakAsync(user.Id); // Day 1
        
        _dbContext.ChangeTracker.Clear();
        user = await _dbContext.Users.FindAsync(user.Id);
        user!.LastLoginDate = DateTime.UtcNow.Date.AddDays(-1);
        await _dbContext.SaveChangesAsync();
        
        await _streakService.UpdateLoginStreakAsync(user.Id); // Day 2

        // Assert
        _dbContext.ChangeTracker.Clear();
        var finalUser = await _dbContext.Users.FindAsync(user.Id);
        
        Assert.NotNull(finalUser);
        Assert.Equal(2, finalUser.CurrentStreak); // New streak
        Assert.Equal(10, finalUser.LongestStreak); // Original longest maintained
    }
}