using AlgoRhythm.Data;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace IntegrationTests.CoursesTests;

public class LectureIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
{
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IAuthService _authService;

    public LectureIntegrationTests(AlgoRhythmTestFixture fixture)
    {
        _scope = fixture.ServerFactory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        _authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
        _httpClient = fixture.ServerFactory.CreateClient();
    }

    private async Task<string> SetupAuthenticatedAdmin()
    {
        var email = $"admin-{Guid.NewGuid()}@test.com";
        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true
        };

        await _userManager.CreateAsync(user, TestConstants.TestUserPassword);
        await _userManager.AddToRoleAsync(user, "Admin");

        var loginResponse = await _authService.LoginAsync(new LoginRequest(email, TestConstants.TestUserPassword));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.Token);

        return loginResponse.Token;
    }

    private async Task<Guid> CreateTestCourse()
    {
        var course = new Course
        {
            Name = $"Test Course {Guid.NewGuid()}",
            Description = "Test Description",
            IsPublished = true
        };

        await _dbContext.Courses.AddAsync(course);
        await _dbContext.SaveChangesAsync();
        return course.Id;
    }

    [Fact]
    public async Task GET_AllLectures_Returns200()
    {
        await SetupAuthenticatedAdmin();

        var response = await _httpClient.GetAsync("/api/lecture");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var lectures = JsonConvert.DeserializeObject<List<LectureDto>>(responseBody);

        Assert.NotNull(lectures);
    }

    [Fact]
    public async Task GET_LectureById_ValidId_Returns200()
    {
        await SetupAuthenticatedAdmin();
        var courseId = await CreateTestCourse();

        var lecture = new Lecture
        {
            Title = "Test Lecture",
            CourseId = courseId
        };
        await _dbContext.Lectures.AddAsync(lecture);
        await _dbContext.SaveChangesAsync();

        var response = await _httpClient.GetAsync($"/api/lecture/{lecture.Id}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var lectureDto = JsonConvert.DeserializeObject<LectureDto>(responseBody);

        Assert.NotNull(lectureDto);
        Assert.Equal(lecture.Id, lectureDto.Id);
        Assert.Equal("Test Lecture", lectureDto.Title);
    }

    [Fact]
    public async Task GET_LectureById_InvalidId_Returns404()
    {
        await SetupAuthenticatedAdmin();

        var response = await _httpClient.GetAsync($"/api/lecture/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_CreateLecture_ValidData_Returns201()
    {
        await SetupAuthenticatedAdmin();
        var courseId = await CreateTestCourse();

        var lectureInput = new LectureInputDto
        {
            Title = "New Lecture",
            CourseId = courseId
        };
        var content = new StringContent(JsonConvert.SerializeObject(lectureInput), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/lecture", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var createdLecture = JsonConvert.DeserializeObject<LectureDto>(responseBody);

        Assert.NotNull(createdLecture);
        Assert.Equal("New Lecture", createdLecture.Title);

        _dbContext.ChangeTracker.Clear();
        var dbLecture = await _dbContext.Lectures.FindAsync(createdLecture.Id);
        Assert.NotNull(dbLecture);
    }

    [Fact]
    public async Task POST_CreateLecture_Unauthorized_Returns401()
    {
        var lectureInput = new LectureInputDto
        {
            Title = "New Lecture",
            CourseId = Guid.NewGuid()
        };
        var content = new StringContent(JsonConvert.SerializeObject(lectureInput), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/lecture", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PUT_UpdateLecture_ValidData_Returns204()
    {
        await SetupAuthenticatedAdmin();
        var courseId = await CreateTestCourse();

        var lecture = new Lecture
        {
            Title = "Original Title",
            CourseId = courseId
        };
        await _dbContext.Lectures.AddAsync(lecture);
        await _dbContext.SaveChangesAsync();

        var updateDto = new LectureInputDto
        {
            Title = "Updated Title",
            CourseId = courseId
        };
        var content = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"/api/lecture/{lecture.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var updatedLecture = await _dbContext.Lectures.FindAsync(lecture.Id);
        Assert.Equal("Updated Title", updatedLecture!.Title);
    }

    [Fact]
    public async Task DELETE_Lecture_ValidId_Returns204()
    {
        await SetupAuthenticatedAdmin();
        var courseId = await CreateTestCourse();

        var lecture = new Lecture
        {
            Title = "To Delete",
            CourseId = courseId
        };
        await _dbContext.Lectures.AddAsync(lecture);
        await _dbContext.SaveChangesAsync();

        var response = await _httpClient.DeleteAsync($"/api/lecture/{lecture.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var deletedLecture = await _dbContext.Lectures.FindAsync(lecture.Id);
        Assert.Null(deletedLecture);
    }

    [Fact]
    public async Task GET_LecturesByCourse_ValidCourseId_Returns200()
    {
        await SetupAuthenticatedAdmin();
        var courseId = await CreateTestCourse();

        var lecture1 = new Lecture { Title = "Lecture 1", CourseId = courseId };
        var lecture2 = new Lecture { Title = "Lecture 2", CourseId = courseId };
        await _dbContext.Lectures.AddRangeAsync(lecture1, lecture2);
        await _dbContext.SaveChangesAsync();

        var response = await _httpClient.GetAsync($"/api/lecture/course/{courseId}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var lectures = JsonConvert.DeserializeObject<List<LectureDto>>(responseBody);

        Assert.NotNull(lectures);
        Assert.True(lectures.Count >= 2);
    }
}