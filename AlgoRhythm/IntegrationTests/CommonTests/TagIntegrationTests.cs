using AlgoRhythm.Data;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Common;
using AlgoRhythm.Shared.Models.Common;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Tasks;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace IntegrationTests.CommonTests;

public class TagIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
{
    private readonly IServiceScope _scope;
    private readonly AlgoRhythmTestFixture _fixture;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    private readonly Guid _testTaskId = Guid.NewGuid();
    private readonly Guid _testCourseId = Guid.NewGuid();
    private readonly Guid _testLectureId = Guid.NewGuid();

    public TagIntegrationTests(AlgoRhythmTestFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.ServerFactory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        _httpClient = fixture.ServerFactory.CreateClient();

        // Create test task for tags
        var testTask = new ProgrammingTaskItem
        {
            Id = _testTaskId,
            Title = "Test Task for Tags",
            IsPublished = true,
            Difficulty = Difficulty.Easy
        };
        _dbContext.ProgrammingTaskItems.Add(testTask);

        // Create test course and lecture for tags
        var testCourse = new Course
        {
            Id = _testCourseId,
            Name = "Test Course for Tags",
            IsPublished = true
        };
        _dbContext.Courses.Add(testCourse);

        var testLecture = new Lecture
        {
            Id = _testLectureId,
            Title = "Test Lecture for Tags",
            IsPublished = true
        };
        _dbContext.Lectures.Add(testLecture);

        _dbContext.SaveChanges();
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

    private async Task<Tag> AddTagToDb(string name, string? description = null)
    {
        var tag = new Tag
        {
            Name = name,
            Description = description
        };
        await _dbContext.Tags.AddAsync(tag);
        await _dbContext.SaveChangesAsync();
        return tag;
    }

    [Fact]
    public async Task GET_GetAll_Returns200WithTags()
    {
        await SetupAuthenticatedUser();
        await AddTagToDb("Tag1" + Guid.NewGuid(), "Description 1");
        await AddTagToDb("Tag2" + Guid.NewGuid(), "Description 2");

        var response = await _httpClient.GetAsync("/api/tag");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var tags = JsonConvert.DeserializeObject<List<TagDto>>(responseBody);

        Assert.NotNull(tags);
        Assert.True(tags.Count >= 2);
    }

    [Fact]
    public async Task GET_GetById_ValidId_Returns200WithTag()
    {
        await SetupAuthenticatedUser();
        var tag = await AddTagToDb("TestTag" + Guid.NewGuid(), "Test Description");

        var response = await _httpClient.GetAsync($"/api/tag/{tag.Id}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var tagDto = JsonConvert.DeserializeObject<TagDto>(responseBody);

        Assert.NotNull(tagDto);
        Assert.Equal(tag.Id, tagDto.Id);
        Assert.Equal(tag.Name, tagDto.Name);
    }

    [Fact]
    public async Task GET_GetById_InvalidId_Returns404()
    {
        await SetupAuthenticatedUser();
        var response = await _httpClient.GetAsync($"/api/tag/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_GetByName_ValidName_Returns200WithTag()
    {
        await SetupAuthenticatedUser();
        var uniqueName = "UniqueTag" + Guid.NewGuid();
        var tag = await AddTagToDb(uniqueName, "Test Description");

        var response = await _httpClient.GetAsync($"/api/tag/by-name/{Uri.EscapeDataString(uniqueName)}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var tagDto = JsonConvert.DeserializeObject<TagDto>(responseBody);

        Assert.NotNull(tagDto);
        Assert.Equal(tag.Name, tagDto.Name);
    }

    [Fact]
    public async Task POST_Create_ValidInput_Returns201AndCreatedTag()
    {
        await SetupAuthenticatedAdminUser();
        var inputDto = new TagInputDto
        {
            Name = "NewTag" + Guid.NewGuid(),
            Description = "New Description"
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(inputDto), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/tag", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var tagDto = JsonConvert.DeserializeObject<TagDto>(responseBody);

        Assert.NotNull(tagDto);
        Assert.Equal(inputDto.Name, tagDto.Name);
        Assert.Equal(inputDto.Description, tagDto.Description);

        _dbContext.ChangeTracker.Clear();
        var dbTag = await _dbContext.Tags.FindAsync(tagDto.Id);
        Assert.NotNull(dbTag);
    }

    [Fact]
    public async Task POST_Create_DuplicateName_Returns400()
    {
        await SetupAuthenticatedAdminUser();
        var uniqueName = "DuplicateTag" + Guid.NewGuid();
        await AddTagToDb(uniqueName);

        var inputDto = new TagInputDto
        {
            Name = uniqueName,
            Description = "Another Description"
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(inputDto), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/tag", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PUT_Update_ValidInput_Returns204()
    {
        await SetupAuthenticatedAdminUser();
        var tag = await AddTagToDb("OriginalTag" + Guid.NewGuid(), "Original Description");
        var updateDto = new TagInputDto
        {
            Name = "UpdatedTag" + Guid.NewGuid(),
            Description = "Updated Description"
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"/api/tag/{tag.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var updatedTag = await _dbContext.Tags.FindAsync(tag.Id);
        Assert.NotNull(updatedTag);
        Assert.Equal(updateDto.Name, updatedTag.Name);
        Assert.Equal(updateDto.Description, updatedTag.Description);
    }

    [Fact]
    public async Task PUT_Update_InvalidId_Returns404()
    {
        await SetupAuthenticatedAdminUser();
        var updateDto = new TagInputDto
        {
            Name = "UpdatedTag" + Guid.NewGuid(),
            Description = "Updated Description"
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"/api/tag/{Guid.NewGuid()}", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Delete_ValidId_Returns204()
    {
        await SetupAuthenticatedAdminUser();
        var tag = await AddTagToDb("ToDelete" + Guid.NewGuid());

        var response = await _httpClient.DeleteAsync($"/api/tag/{tag.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var deletedTag = await _dbContext.Tags.FindAsync(tag.Id);
        Assert.Null(deletedTag);
    }

    [Fact]
    public async Task POST_AssignTagToTask_ValidIds_Returns204()
    {
        await SetupAuthenticatedAdminUser(); 
        var tag = await AddTagToDb("TaskTag" + Guid.NewGuid());

        // Endpoint is in TaskController, requires Admin
        var response = await _httpClient.PostAsync($"/api/task/{_testTaskId}/tags/{tag.Id}", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var updatedTask = await _dbContext.TaskItems
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == _testTaskId);

        Assert.NotNull(updatedTask);
        Assert.Contains(updatedTask.Tags, t => t.Id == tag.Id);
    }

    [Fact]
    public async Task POST_AssignTagToLecture_ValidIds_Returns204()
    {
        await SetupAuthenticatedAdminUser();
        var tag = await AddTagToDb("LectureTag" + Guid.NewGuid());

        // Endpoint is in LectureController, requires Admin
        var response = await _httpClient.PostAsync($"/api/lecture/{_testLectureId}/tags/{tag.Id}", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var updatedLecture = await _dbContext.Lectures
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Id == _testLectureId);

        Assert.NotNull(updatedLecture);
        Assert.Contains(updatedLecture.Tags, t => t.Id == tag.Id);
    }

    [Fact]
    public async Task DELETE_RemoveTagFromTask_ValidIds_Returns204()
    {
        await SetupAuthenticatedAdminUser();
        var tag = await AddTagToDb("TaskTagToRemove" + Guid.NewGuid());

        var task = await _dbContext.TaskItems
            .Include(t => t.Tags)
            .FirstAsync(t => t.Id == _testTaskId);
        task.Tags.Add(tag);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        // Endpoint is in TaskController, requires Admin
        var response = await _httpClient.DeleteAsync($"/api/task/{_testTaskId}/tags/{tag.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var updatedTask = await _dbContext.TaskItems
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == _testTaskId);

        Assert.NotNull(updatedTask);
        Assert.DoesNotContain(updatedTask.Tags, t => t.Id == tag.Id);
    }

    [Fact]
    public async Task DELETE_RemoveTagFromLecture_ValidIds_Returns204()
    {
        await SetupAuthenticatedAdminUser();
        var tag = await AddTagToDb("LectureTagToRemove" + Guid.NewGuid());

        var lecture = await _dbContext.Lectures
            .Include(l => l.Tags)
            .FirstAsync(l => l.Id == _testLectureId);
        lecture.Tags.Add(tag);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        // Endpoint is in LectureController, requires Admin
        var response = await _httpClient.DeleteAsync($"/api/lecture/{_testLectureId}/tags/{tag.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var updatedLecture = await _dbContext.Lectures
            .Include(l => l.Tags)
            .FirstOrDefaultAsync(l => l.Id == _testLectureId);

        Assert.NotNull(updatedLecture);
        Assert.DoesNotContain(updatedLecture.Tags, t => t.Id == tag.Id);
    }

    [Fact]
    public async Task GET_Without_Returns401()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
        var response = await _httpClient.GetAsync("/api/tag");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task POST_Create_WithoutAdminRole_Returns403()
    {
        await SetupAuthenticatedUser();
        var inputDto = new TagInputDto
        {
            Name = "NewTag" + Guid.NewGuid(),
            Description = "New Description"
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(inputDto), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/tag", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}