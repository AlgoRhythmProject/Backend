using AlgoRhythm.Data;
using AlgoRhythm.Services.Admin.Interfaces;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Admin;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.AdminTests;

public class AdminIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
{
    private readonly IServiceScope _scope;
    private readonly AlgoRhythmTestFixture _fixture;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IAdminService _adminService;

    public AdminIntegrationTests(AlgoRhythmTestFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.ServerFactory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        _adminService = _scope.ServiceProvider.GetRequiredService<IAdminService>();
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
    public async Task GET_IsAdmin_AuthenticatedUser_Returns200()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();

        // Act
        var response = await _httpClient.GetAsync("/api/admin/is-admin");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GET_IsAdmin_AdminUser_ReturnsTrue()
    {
        // Arrange
        var (_, adminUser) = await SetupAuthenticatedAdminUser();

        // Act
        var isAdmin = await _adminService.IsUserAdminAsync(adminUser.Id, CancellationToken.None);

        // Assert
        Assert.True(isAdmin);
    }

    [Fact]
    public async Task GET_IsAdmin_RegularUser_ReturnsFalse()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();

        // Act
        var isAdmin = await _adminService.IsUserAdminAsync(user.Id, CancellationToken.None);

        // Assert
        Assert.False(isAdmin);
    }

    [Fact]
    public async Task GET_AllUsers_AsAdmin_Returns200WithUsers()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();
        await CreateUserWithoutAuth();
        await CreateUserWithoutAuth();

        // Act
        var response = await _httpClient.GetAsync("/api/admin/users");

        // Assert
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<List<UserWithRolesDto>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users);
    }

    [Fact]
    public async Task GET_AllUsers_AsRegularUser_Returns403()
    {
        // Arrange
        await SetupAuthenticatedUser();

        // Act
        var response = await _httpClient.GetAsync("/api/admin/users");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_UserWithRoles_AsAdmin_Returns200()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();
        var targetUser = await CreateUserWithoutAuth();

        // Act
        var response = await _httpClient.GetAsync($"/api/admin/users/{targetUser.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var userDto = await response.Content.ReadFromJsonAsync<UserWithRolesDto>();
        Assert.NotNull(userDto);
        Assert.Equal(targetUser.Id, userDto.Id);
        Assert.Contains("User", userDto.Roles);
    }

    [Fact]
    public async Task POST_AssignAdminRole_AsAdmin_Returns200()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();
        var targetUser = await CreateUserWithoutAuth();

        // Act
        var response = await _httpClient.PostAsync($"/api/admin/users/{targetUser.Id}/assign-admin", null);

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify user is now admin
        var isAdmin = await _adminService.IsUserAdminAsync(targetUser.Id, CancellationToken.None);
        Assert.True(isAdmin);
    }

    [Fact]
    public async Task POST_AssignAdminRole_ToAlreadyAdmin_Returns400()
    {
        // Arrange
        var (_, adminUser) = await SetupAuthenticatedAdminUser();

        // Act
        var response = await _httpClient.PostAsync($"/api/admin/users/{adminUser.Id}/assign-admin", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_RevokeAdminRole_AsAdmin_Returns200()
    {
        // Arrange
        await SetupAuthenticatedAdminUser();

        var secondAdminEmail = $"admin-{Guid.NewGuid()}@example.com";
        var secondAdminPassword = "AdminPassword123!";

        var secondAdmin = new User
        {
            UserName = secondAdminEmail,
            Email = secondAdminEmail,
            FirstName = "Second",
            LastName = "Admin",
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(secondAdmin, secondAdminPassword);
        Assert.True(createResult.Succeeded, $"Failed to create second admin: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

        var addRoleResult = await _userManager.AddToRoleAsync(secondAdmin, "Admin");
        Assert.True(addRoleResult.Succeeded, "Failed to add Admin role");

        _dbContext.ChangeTracker.Clear();

        var isAdminBefore = await _adminService.IsUserAdminAsync(secondAdmin.Id, CancellationToken.None);
        Assert.True(isAdminBefore, "Second user should be admin before revoke");

        // Act
        var response = await _httpClient.PostAsync($"/api/admin/users/{secondAdmin.Id}/revoke-admin", null);

        // Assert
        response.EnsureSuccessStatusCode();

        await Task.Delay(100);

        _dbContext.ChangeTracker.Clear();

        var secondAdminReloaded = await _userManager.FindByIdAsync(secondAdmin.Id.ToString());
        Assert.NotNull(secondAdminReloaded);

        var isAdminAfter = await _userManager.IsInRoleAsync(secondAdminReloaded, "Admin");
        Assert.False(isAdminAfter, "Second user should NOT be admin after revoke");

        var isAdminViaService = await _adminService.IsUserAdminAsync(secondAdmin.Id, CancellationToken.None);
        Assert.False(isAdminViaService, "Service should also report user is not admin");
    }

    [Fact]
    public async Task POST_RevokeAdminRole_FromLastAdmin_Returns400()
    {
        // Arrange
        using var newScope = _fixture.ServerFactory.Services.CreateScope();
        var newDbContext = newScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var newUserManager = newScope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var newRoleManager = newScope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var newAuthService = newScope.ServiceProvider.GetRequiredService<IAuthService>();
        var newHttpClient = _fixture.ServerFactory.CreateClient();

        if (!await newRoleManager.RoleExistsAsync("Admin"))
        {
            await newRoleManager.CreateAsync(new Role
            {
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "Administrator"
            });
        }

        var adminEmail = $"only-admin-{Guid.NewGuid()}@example.com";
        var adminPassword = "AdminPassword123!";

        var adminUser = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Only",
            LastName = "Admin",
            EmailConfirmed = true
        };

        var createResult = await newUserManager.CreateAsync(adminUser, adminPassword);
        Assert.True(createResult.Succeeded, $"Failed to create admin: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

        var addRoleResult = await newUserManager.AddToRoleAsync(adminUser, "Admin");
        Assert.True(addRoleResult.Succeeded, "Failed to add Admin role");

        var loginRequest = new LoginRequest(adminEmail, adminPassword);
        var authResponse = await newAuthService.LoginAsync(loginRequest);
        newHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.Token);

        newDbContext.ChangeTracker.Clear();
        var allAdmins = await newUserManager.GetUsersInRoleAsync("Admin");

        if (allAdmins.Count > 1)
        {
            foreach (var extraAdmin in allAdmins.Where(a => a.Id != adminUser.Id))
            {
                await newUserManager.RemoveFromRoleAsync(extraAdmin, "Admin");
            }

            allAdmins = await newUserManager.GetUsersInRoleAsync("Admin");
        }

        Assert.Single(allAdmins);
        Assert.Equal(adminUser.Id, allAdmins.First().Id);

        // Act - Try to revoke from the only admin
        var response = await newHttpClient.PostAsync($"/api/admin/users/{adminUser.Id}/revoke-admin", null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        newDbContext.ChangeTracker.Clear();
        var adminUserReloaded = await newUserManager.FindByIdAsync(adminUser.Id.ToString());
        var isStillAdmin = await newUserManager.IsInRoleAsync(adminUserReloaded!, "Admin");
        Assert.True(isStillAdmin, "Admin should still have admin role after failed revoke");
    }

    [Fact]
    public async Task POST_RevokeAdminRole_FromSelf_Returns200AndLogsOut()
    {
        // Arrange
        var (_, adminUser1) = await SetupAuthenticatedAdminUser();

        // Create second admin so we're not revoking from last admin
        var adminUser2 = await CreateUserWithoutAuth();
        await _userManager.AddToRoleAsync(adminUser2, "Admin");

        // Act - Revoke admin1's role while logged in as admin1
        var response = await _httpClient.PostAsync($"/api/admin/users/{adminUser1.Id}/revoke-admin", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task POST_AssignAdminRole_AsRegularUser_Returns403()
    {
        // Arrange
        await SetupAuthenticatedUser();
        var targetUser = await CreateUserWithoutAuth();

        // Act
        var response = await _httpClient.PostAsync($"/api/admin/users/{targetUser.Id}/assign-admin", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUserWithRoles_NonExistentUser_ThrowsException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _adminService.GetUserWithRolesAsync(nonExistentUserId, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllUsers_ReturnsUsersWithRoles()
    {
        // Arrange
        if (!await _roleManager.RoleExistsAsync("User"))
        {
            await _roleManager.CreateAsync(new Role
            {
                Name = "User",
                NormalizedName = "USER",
                Description = "Regular user"
            });
        }

        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            await _roleManager.CreateAsync(new Role
            {
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "Administrator"
            });
        }

        await CreateUserWithoutAuth();
        await CreateUserWithoutAuth();

        // Act
        var users = await _adminService.GetAllUsersAsync(CancellationToken.None);

        // Assert
        Assert.NotEmpty(users);
        Assert.All(users, user =>
        {
            Assert.NotNull(user.Email);
        });
    }
}