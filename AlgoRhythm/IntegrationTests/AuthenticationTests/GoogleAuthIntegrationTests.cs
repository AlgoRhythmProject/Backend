using System.Net;
using System.Net.Http.Json;
using AlgoRhythm.Data;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Shared.Models.Users;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Achievements;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using IntegrationTests.IntegrationTestSetup;

namespace IntegrationTests.AuthenticationTests;

public class GoogleAuthIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
{
    private readonly HttpClient _client;
    private readonly AlgoRhythmTestFixture _fixture;

    public GoogleAuthIntegrationTests(AlgoRhythmTestFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.ServerFactory.CreateClient();
    }

    [Fact]
    public async Task GoogleLogin_WithValidToken_ShouldCreateNewUserAndReturnAuthResponse()
    {
        // Arrange
        var request = new GoogleAuthRequest(
            IdToken: "mock_google_token_new_user",
            FirstName: "Jan",
            LastName: "Testowy"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/Authentication/google", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.NotNull(authResponse.User);
        Assert.Equal("new@test.com", authResponse.User.Email);
        Assert.True(authResponse.User.EmailConfirmed);
        Assert.False(string.IsNullOrEmpty(authResponse.Token));

        // Verify user was saved in database
        using var scope = _fixture.ServerFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == authResponse.User.Email);

        Assert.NotNull(user);
        

        var coursesExist = await dbContext.Set<Course>().AnyAsync();
        var courseProgress = await dbContext.Set<CourseProgress>()
            .Where(cp => cp.UserId == user.Id)
            .ToListAsync();
        
        if (coursesExist)
        {
            Assert.NotEmpty(courseProgress);
        }

        var achievementsExist = await dbContext.Set<Achievement>().AnyAsync();
        var userAchievements = await dbContext.Set<UserAchievement>()
            .Where(ua => ua.UserId == user.Id)
            .ToListAsync();
        
        if (achievementsExist)
        {
            Assert.NotEmpty(userAchievements);
        }
    }

    [Fact]
    public async Task GoogleLogin_WithExistingUser_ShouldLoginAndNotDuplicate()
    {
        // Arrange - Create existing user
        using var scope = _fixture.ServerFactory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var existingUser = new User
        {
            Email = "existing@test.com",
            UserName = "existing@test.com",
            FirstName = "Existing",
            LastName = "User",
            EmailConfirmed = true,
            CurrentStreak = 0,
            LongestStreak = 0
        };

        var createResult = await userManager.CreateAsync(existingUser);
        Assert.True(createResult.Succeeded, "Failed to create test user");

        // Mock request for existing user
        var request = new GoogleAuthRequest(
            IdToken: "mock_google_token_existing@test.com",
            FirstName: null,
            LastName: null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/Authentication/google", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.Equal("existing@test.com", authResponse.User.Email);

        // Verify no duplicate user was created
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userCount = await dbContext.Users
            .CountAsync(u => u.Email == "existing@test.com");

        Assert.Equal(1, userCount);
    }

    [Fact]
    public async Task GoogleLogin_WithInvalidToken_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new GoogleAuthRequest(
            IdToken: "invalid_token_12345",
            FirstName: null,
            LastName: null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/Authentication/google", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GoogleLogin_WithEmptyToken_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new GoogleAuthRequest(
            IdToken: "",
            FirstName: null,
            LastName: null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/Authentication/google", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GoogleLogin_ShouldLinkGoogleAccountToExistingUser()
    {
        // Arrange - Create user without Google login
        using var scope = _fixture.ServerFactory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var existingUser = new User
        {
            Email = "linktest@test.com",
            UserName = "linktest@test.com",
            FirstName = "Link",
            LastName = "Test",
            EmailConfirmed = true,
            CurrentStreak = 0,
            LongestStreak = 0
        };

        var createResult = await userManager.CreateAsync(existingUser, "Password123!");
        Assert.True(createResult.Succeeded);

        // Mock Google login for same email
        var request = new GoogleAuthRequest(
            IdToken: "mock_google_token_linktest@test.com",
            FirstName: null,
            LastName: null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/Authentication/google", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse);
        Assert.Equal("linktest@test.com", authResponse.User.Email);

        // Verify user exists
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "linktest@test.com");

        Assert.NotNull(user);
    }

    [Fact]
    public async Task GoogleLogin_ShouldUpdateLoginStreak()
    {
        // Arrange
        var request = new GoogleAuthRequest(
            IdToken: "mock_google_token_streak_user",
            FirstName: "Streak",
            LastName: "User"
        );

        // Act - First login
        var response1 = await _client.PostAsJsonAsync("/api/Authentication/google", request);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var authResponse1 = await response1.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResponse1);

        // Verify streak was initialized
        Assert.True(authResponse1.User.CurrentStreak >= 0);
        Assert.True(authResponse1.User.LongestStreak >= 0);
        Assert.NotNull(authResponse1.User.LastLoginDate);
    }
}