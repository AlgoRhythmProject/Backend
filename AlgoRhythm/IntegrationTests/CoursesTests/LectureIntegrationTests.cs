using AlgoRhythm.Data;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    private readonly IAuthService _authService;

    public LectureIntegrationTests(AlgoRhythmTestFixture fixture)
    {
        _scope = fixture.ServerFactory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
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

    private async Task<(string token, Guid userId)> SetupAuthenticatedUser()
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

        return (loginResponse.Token, user.Id);
    }

    [Fact]
    public async Task GET_AllPublishedLectures_Returns200()
    {
        await SetupAuthenticatedUser();

        var response = await _httpClient.GetAsync("/api/lecture/published");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var lectures = JsonConvert.DeserializeObject<List<LectureDto>>(responseBody);

        Assert.NotNull(lectures);
    }

    [Fact]
    public async Task GET_All_AdminOnly_Returns200()
    {
        await SetupAuthenticatedAdmin();

        var response = await _httpClient.GetAsync("/api/lecture");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var lectures = JsonConvert.DeserializeObject<List<LectureDto>>(responseBody);

        Assert.NotNull(lectures);
    }

    [Fact]
    public async Task GET_All_AdminOnly_Returns403_For_User()
    {
        await SetupAuthenticatedUser();

        var response = await _httpClient.GetAsync("/api/lecture");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_LectureById_ValidId_Returns200()
    {
        await SetupAuthenticatedUser();

        var lecture = new Lecture
        {
            Title = "Test Lecture",
            IsPublished = true
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
    public async Task POST_CreateLecture_ValidData_Returns201()
    {
        await SetupAuthenticatedAdmin();

        var lectureInput = new LectureInputDto
        {
            Title = "New Lecture",
            IsPublished = false
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
    public async Task POST_CreateLecture_WithoutCourse_CreatesIndependentLecture()
    {
        await SetupAuthenticatedAdmin();

        var lectureInput = new LectureInputDto
        {
            Title = "Independent Lecture",
            IsPublished = true
        };

        var content = new StringContent(JsonConvert.SerializeObject(lectureInput), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/lecture", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var createdLecture = JsonConvert.DeserializeObject<LectureDto>(responseBody);

        Assert.NotNull(createdLecture);
        Assert.Empty(createdLecture.CourseIds); // No courses initially
    }

    [Fact]
    public async Task PUT_UpdateLecture_ValidData_Returns204()
    {
        await SetupAuthenticatedAdmin();

        var lecture = new Lecture
        {
            Title = "Original Title",
            IsPublished = false
        };

        await _dbContext.Lectures.AddAsync(lecture);
        await _dbContext.SaveChangesAsync();

        var updateDto = new LectureInputDto
        {
            Title = "Updated Title",
            IsPublished = true
        };

        var content = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync($"/api/lecture/{lecture.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var updatedLecture = await _dbContext.Lectures.FindAsync(lecture.Id);
        Assert.Equal("Updated Title", updatedLecture!.Title);
        Assert.True(updatedLecture.IsPublished);
    }

    [Fact]
    public async Task DELETE_Lecture_ValidId_Returns204()
    {
        await SetupAuthenticatedAdmin();

        var lecture = new Lecture
        {
            Title = "To Delete",
            IsPublished = true
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
    public async Task GET_LecturesByCourseId_Returns200WithLectures()
    {
        await SetupAuthenticatedUser();

        var course = new Course
        {
            Name = "Test Course",
            Description = "Test"
        };

        var lecture1 = new Lecture { Title = "Lecture 1", IsPublished = true };
        var lecture2 = new Lecture { Title = "Lecture 2", IsPublished = true };
        var otherLecture = new Lecture { Title = "Other Lecture", IsPublished = true };

        await _dbContext.Courses.AddAsync(course);
        await _dbContext.Lectures.AddRangeAsync(lecture1, lecture2, otherLecture);
        await _dbContext.SaveChangesAsync();

        // Assign lectures to course
        course.Lectures.Add(lecture1);
        course.Lectures.Add(lecture2);
        // otherLecture is NOT added to this course
        await _dbContext.SaveChangesAsync();

        var response = await _httpClient.GetAsync($"/api/lecture/course/{course.Id}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var lectures = JsonConvert.DeserializeObject<List<LectureDto>>(responseBody);

        Assert.NotNull(lectures);
        Assert.Equal(2, lectures.Count);
        Assert.Contains(lectures, l => l.Title == "Lecture 1");
        Assert.Contains(lectures, l => l.Title == "Lecture 2");
        Assert.DoesNotContain(lectures, l => l.Title == "Other Lecture");
    }

    [Fact]
    public async Task Lecture_CourseIds_ReflectsAllAssignedCourses()
    {
        await SetupAuthenticatedUser();

        var course1 = new Course { Name = "Course 1", Description = "Test" };
        var course2 = new Course { Name = "Course 2", Description = "Test" };
        var lecture = new Lecture { Title = "Multi-Course Lecture", IsPublished = true };

        await _dbContext.Courses.AddRangeAsync(course1, course2);
        await _dbContext.Lectures.AddAsync(lecture);
        await _dbContext.SaveChangesAsync();

        // Assign lecture to both courses
        course1.Lectures.Add(lecture);
        course2.Lectures.Add(lecture);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var response = await _httpClient.GetAsync($"/api/lecture/{lecture.Id}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var lectureDto = JsonConvert.DeserializeObject<LectureDto>(responseBody);

        Assert.NotNull(lectureDto);
        Assert.Equal(2, lectureDto.CourseIds.Count);
        Assert.Contains(course1.Id, lectureDto.CourseIds);
        Assert.Contains(course2.Id, lectureDto.CourseIds);
    }

    [Fact]
    public async Task POST_CreateLecture_AsRegularUser_Returns403()
    {
        await SetupAuthenticatedUser(); // Regular user, not admin

        var lectureInput = new LectureInputDto
        {
            Title = "New Lecture",
            IsPublished = false
        };

        var content = new StringContent(JsonConvert.SerializeObject(lectureInput), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/lecture", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DELETE_Lecture_RemovesFromAllCourses()
    {
        await SetupAuthenticatedAdmin();

        var course1 = new Course { Name = "Course 1", Description = "Test" };
        var course2 = new Course { Name = "Course 2", Description = "Test" };
        var lecture = new Lecture { Title = "Lecture to Delete", IsPublished = true };

        await _dbContext.Courses.AddRangeAsync(course1, course2);
        await _dbContext.Lectures.AddAsync(lecture);
        await _dbContext.SaveChangesAsync();

        // Add to both courses
        course1.Lectures.Add(lecture);
        course2.Lectures.Add(lecture);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        // Delete lecture
        var response = await _httpClient.DeleteAsync($"/api/lecture/{lecture.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();

        // Verify lecture is deleted
        var deletedLecture = await _dbContext.Lectures.FindAsync(lecture.Id);
        Assert.Null(deletedLecture);

        // Verify courses no longer have this lecture
        var updatedCourse1 = await _dbContext.Courses
            .Include(c => c.Lectures)
            .FirstAsync(c => c.Id == course1.Id);

        var updatedCourse2 = await _dbContext.Courses
            .Include(c => c.Lectures)
            .FirstAsync(c => c.Id == course2.Id);

        Assert.DoesNotContain(updatedCourse1.Lectures, l => l.Id == lecture.Id);
        Assert.DoesNotContain(updatedCourse2.Lectures, l => l.Id == lecture.Id);
    }
}