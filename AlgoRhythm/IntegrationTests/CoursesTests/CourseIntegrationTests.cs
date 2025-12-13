using AlgoRhythm.Data;
using AlgoRhythm.Shared.Dtos.Courses;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Users;
using AlgoRhythm.Shared.Models.Tasks;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace IntegrationTests.CoursesTests;

public class CourseIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
{
    private readonly IServiceScope _scope;
    private readonly AlgoRhythmTestFixture _fixture;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public CourseIntegrationTests(AlgoRhythmTestFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.ServerFactory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        _httpClient = fixture.ServerFactory.CreateClient();
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var authService = _scope.ServiceProvider.GetRequiredService<AlgoRhythm.Services.Users.Interfaces.IAuthService>();
        return await TestHelpers.SetupAuthenticatedUser(
            TestConstants.TestUserEmail + Guid.NewGuid(),
            TestConstants.TestUserPassword,
            _roleManager,
            _userManager,
            authService
        );
    }

    private async Task<Course> AddCourseToDb(string name, string? description = null, bool isPublished = false)
    {
        var course = new Course
        {
            Name = name,
            Description = description,
            IsPublished = isPublished
        };

        await _dbContext.Courses.AddAsync(course);
        await _dbContext.SaveChangesAsync();
        return course;
    }

    [Fact]
    public async Task GET_GetAll_Returns200WithCourses()
    {
        var token = await GetAuthTokenAsync();
        await AddCourseToDb("Test Course 1", "Description 1");
        await AddCourseToDb("Test Course 2", "Description 2");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.GetAsync("/api/course");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var courses = JsonConvert.DeserializeObject<List<CourseSummaryDto>>(responseBody);

        Assert.NotNull(courses);
        Assert.True(courses.Count >= 2);
    }

    [Fact]
    public async Task GET_GetPublished_Returns200WithOnlyPublishedCourses()
    {
        var token = await GetAuthTokenAsync();
        await AddCourseToDb("Published Course", "Published", true);
        await AddCourseToDb("Unpublished Course", "Unpublished", false);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.GetAsync("/api/course/published");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var courses = JsonConvert.DeserializeObject<List<CourseSummaryDto>>(responseBody);

        Assert.NotNull(courses);
        Assert.All(courses, c => Assert.True(c.IsPublished));
    }

    [Fact]
    public async Task GET_GetById_ValidId_Returns200WithCourse()
    {
        var token = await GetAuthTokenAsync();
        var course = await AddCourseToDb("Test Course", "Description");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.GetAsync($"/api/course/{course.Id}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var courseDto = JsonConvert.DeserializeObject<CourseDto>(responseBody);

        Assert.NotNull(courseDto);
        Assert.Equal(course.Id, courseDto.Id);
        Assert.Equal(course.Name, courseDto.Name);
    }

    [Fact]
    public async Task GET_GetById_InvalidId_Returns404()
    {
        var token = await GetAuthTokenAsync();
        var invalidId = Guid.NewGuid();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.GetAsync($"/api/course/{invalidId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_Create_ValidInput_Returns201AndCreatedCourse()
    {
        var token = await GetAuthTokenAsync();
        var inputDto = new CourseInputDto
        {
            Name = "New Course",
            Description = "New Description",
            IsPublished = false
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(inputDto), Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.PostAsync("/api/course", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var courseDto = JsonConvert.DeserializeObject<CourseDto>(responseBody);

        Assert.NotNull(courseDto);
        Assert.Equal(inputDto.Name, courseDto.Name);
        Assert.Equal(inputDto.Description, courseDto.Description);

        _dbContext.ChangeTracker.Clear();
        var dbCourse = await _dbContext.Courses.FindAsync(courseDto.Id);
        Assert.NotNull(dbCourse);
    }

    [Fact]
    public async Task PUT_Update_ValidInput_Returns204()
    {
        var token = await GetAuthTokenAsync();
        var course = await AddCourseToDb("Original Name", "Original Description");
        var updateDto = new CourseInputDto
        {
            Name = "Updated Name",
            Description = "Updated Description",
            IsPublished = true
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.PutAsync($"/api/course/{course.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var updatedCourse = await _dbContext.Courses.FindAsync(course.Id);
        Assert.NotNull(updatedCourse);
        Assert.Equal(updateDto.Name, updatedCourse.Name);
        Assert.Equal(updateDto.Description, updatedCourse.Description);
        Assert.Equal(updateDto.IsPublished, updatedCourse.IsPublished);
    }

    [Fact]
    public async Task PUT_Update_InvalidId_Returns404()
    {
        var token = await GetAuthTokenAsync();
        var updateDto = new CourseInputDto
        {
            Name = "Updated Name",
            Description = "Updated Description",
            IsPublished = true
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.PutAsync($"/api/course/{Guid.NewGuid()}", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Delete_ValidId_Returns204()
    {
        var token = await GetAuthTokenAsync();
        var course = await AddCourseToDb("To Delete");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.DeleteAsync($"/api/course/{course.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var deletedCourse = await _dbContext.Courses.FindAsync(course.Id);
        Assert.Null(deletedCourse);
    }

    [Fact]
    public async Task POST_AddTask_ValidIds_Returns204()
    {
        var token = await GetAuthTokenAsync();
        var course = await AddCourseToDb("Course");
        var task = new ProgrammingTaskItem
        {
            Title = "Task",
            Description = "Task Description",
            Difficulty = Difficulty.Easy,
            IsPublished = true
        };
        await _dbContext.TaskItems.AddAsync(task);
        await _dbContext.SaveChangesAsync();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.PostAsync($"/api/course/{course.Id}/tasks/{task.Id}", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_RemoveTask_ValidIds_Returns204()
    {
        var token = await GetAuthTokenAsync();
        var course = await AddCourseToDb("Course");
        var task = new ProgrammingTaskItem
        {
            Title = "Task",
            Description = "Task Description",
            Difficulty = Difficulty.Easy,
            IsPublished = true
        };
        task.Courses.Add(course);
        await _dbContext.TaskItems.AddAsync(task);
        await _dbContext.SaveChangesAsync();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.DeleteAsync($"/api/course/{course.Id}/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GET_WithoutAuth_Returns401()
    {
        var response = await _httpClient.GetAsync("/api/course");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}