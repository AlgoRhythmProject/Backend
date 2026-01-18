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

public class CourseIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
{
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly UserManager<User> _userManager;
    private readonly IAuthService _authService;

    public CourseIntegrationTests(AlgoRhythmTestFixture fixture)
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
    public async Task GET_AllCourses_Returns200()
    {
        await SetupAuthenticatedAdmin();

        var response = await _httpClient.GetAsync("/api/course");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var courses = JsonConvert.DeserializeObject<List<CourseSummaryDto>>(responseBody);

        Assert.NotNull(courses);
    }

    [Fact]
    public async Task GET_PublishedCourses_ReturnsOnlyPublished()
    {
        await SetupAuthenticatedAdmin();

        var publishedCourse = new Course
        {
            Name = "Published Course",
            Description = "This is published",
            IsPublished = true
        };

        var unpublishedCourse = new Course
        {
            Name = "Unpublished Course",
            Description = "This is not published",
            IsPublished = false
        };

        await _dbContext.Courses.AddRangeAsync(publishedCourse, unpublishedCourse);
        await _dbContext.SaveChangesAsync();

        var response = await _httpClient.GetAsync("/api/course/published");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var courses = JsonConvert.DeserializeObject<List<CourseSummaryDto>>(responseBody);

        Assert.NotNull(courses);
        Assert.All(courses, c => Assert.True(c.IsPublished));
    }

    [Fact]
    public async Task GET_CourseById_ValidId_Returns200()
    {
        await SetupAuthenticatedAdmin();

        var course = new Course
        {
            Name = "Test Course",
            Description = "Test Description",
            IsPublished = true
        };

        await _dbContext.Courses.AddAsync(course);
        await _dbContext.SaveChangesAsync();

        var response = await _httpClient.GetAsync($"/api/course/{course.Id}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var courseDto = JsonConvert.DeserializeObject<CourseDto>(responseBody);

        Assert.NotNull(courseDto);
        Assert.Equal(course.Id, courseDto.Id);
        Assert.Equal("Test Course", courseDto.Name);
    }

    [Fact]
    public async Task GET_CourseById_InvalidId_Returns404()
    {
        await SetupAuthenticatedAdmin();

        var response = await _httpClient.GetAsync($"/api/course/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_CreateCourse_ValidData_Returns201()
    {
        await SetupAuthenticatedAdmin();

        var courseInput = new CourseInputDto
        {
            Name = "New Course",
            Description = "New course description",
            IsPublished = false
        };

        var content = new StringContent(JsonConvert.SerializeObject(courseInput), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/course", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var createdCourse = JsonConvert.DeserializeObject<CourseDto>(responseBody);

        Assert.NotNull(createdCourse);
        Assert.Equal("New Course", createdCourse.Name);

        _dbContext.ChangeTracker.Clear();
        var dbCourse = await _dbContext.Courses.FindAsync(createdCourse.Id);
        Assert.NotNull(dbCourse);
    }

    [Fact]
    public async Task POST_CreateCourse_Unauthorized_Returns401()
    {
        var courseInput = new CourseInputDto
        {
            Name = "New Course",
            Description = "Description"
        };

        var content = new StringContent(JsonConvert.SerializeObject(courseInput), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/course", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PUT_UpdateCourse_ValidData_Returns204()
    {
        await SetupAuthenticatedAdmin();

        var course = new Course
        {
            Name = "Original Name",
            Description = "Original Description",
            IsPublished = false
        };

        await _dbContext.Courses.AddAsync(course);
        await _dbContext.SaveChangesAsync();

        var updateDto = new CourseInputDto
        {
            Name = "Updated Name",
            Description = "Updated Description",
            IsPublished = true
        };

        var content = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync($"/api/course/{course.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var updatedCourse = await _dbContext.Courses.FindAsync(course.Id);
        Assert.Equal("Updated Name", updatedCourse!.Name);
        Assert.True(updatedCourse.IsPublished);
    }

    [Fact]
    public async Task DELETE_Course_ValidId_Returns204()
    {
        await SetupAuthenticatedAdmin();

        var course = new Course
        {
            Name = "To Delete",
            Description = "Will be deleted"
        };

        await _dbContext.Courses.AddAsync(course);
        await _dbContext.SaveChangesAsync();

        var response = await _httpClient.DeleteAsync($"/api/course/{course.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var deletedCourse = await _dbContext.Courses.FindAsync(course.Id);
        Assert.Null(deletedCourse);
    }

    [Fact]
    public async Task GET_CourseWithLectures_ValidId_ReturnsLectures()
    {
        await SetupAuthenticatedAdmin();

        var course = new Course
        {
            Name = "Course with Lectures",
            Description = "Has lectures"
        };

        // Create lectures independently (many-to-many)
        var lecture1 = new Lecture
        {
            Title = "Lecture 1",
            IsPublished = true
        };

        var lecture2 = new Lecture
        {
            Title = "Lecture 2",
            IsPublished = true
        };

        await _dbContext.Courses.AddAsync(course);
        await _dbContext.Lectures.AddRangeAsync(lecture1, lecture2);
        await _dbContext.SaveChangesAsync();

        // Assign lectures to course (many-to-many)
        course.Lectures.Add(lecture1);
        course.Lectures.Add(lecture2);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var response = await _httpClient.GetAsync($"/api/course/{course.Id}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var courseDto = JsonConvert.DeserializeObject<CourseDto>(responseBody);

        Assert.NotNull(courseDto);
        Assert.True(courseDto.Lectures.Count >= 2);
        Assert.Contains(courseDto.Lectures, l => l.Title == "Lecture 1");
        Assert.Contains(courseDto.Lectures, l => l.Title == "Lecture 2");
    }

    [Fact]
    public async Task POST_AddLectureToCourse_ValidIds_Returns204()
    {
        await SetupAuthenticatedAdmin();

        var course = new Course
        {
            Name = "Course",
            Description = "Test"
        };

        var lecture = new Lecture
        {
            Title = "Lecture to Add",
            IsPublished = true
        };

        await _dbContext.Courses.AddAsync(course);
        await _dbContext.Lectures.AddAsync(lecture);
        await _dbContext.SaveChangesAsync();

        var response = await _httpClient.PostAsync($"/api/course/{course.Id}/lectures/{lecture.Id}", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var updatedCourse = await _dbContext.Courses
            .Include(c => c.Lectures)
            .FirstOrDefaultAsync(c => c.Id == course.Id);

        Assert.NotNull(updatedCourse);
        Assert.Contains(updatedCourse.Lectures, l => l.Id == lecture.Id);
    }

    [Fact]
    public async Task DELETE_RemoveLectureFromCourse_ValidIds_Returns204()
    {
        await SetupAuthenticatedAdmin();

        var course = new Course
        {
            Name = "Course",
            Description = "Test"
        };

        var lecture = new Lecture
        {
            Title = "Lecture to Remove",
            IsPublished = true
        };

        await _dbContext.Courses.AddAsync(course);
        await _dbContext.Lectures.AddAsync(lecture);
        await _dbContext.SaveChangesAsync();

        // Add lecture to course
        course.Lectures.Add(lecture);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        var response = await _httpClient.DeleteAsync($"/api/course/{course.Id}/lectures/{lecture.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var updatedCourse = await _dbContext.Courses
            .Include(c => c.Lectures)
            .FirstOrDefaultAsync(c => c.Id == course.Id);

        Assert.NotNull(updatedCourse);
        Assert.DoesNotContain(updatedCourse.Lectures, l => l.Id == lecture.Id);
    }

    [Fact]
    public async Task Lecture_CanBelongToMultipleCourses()
    {
        await SetupAuthenticatedAdmin();

        var course1 = new Course { Name = "Course 1", Description = "Test" };
        var course2 = new Course { Name = "Course 2", Description = "Test" };

        var sharedLecture = new Lecture
        {
            Title = "Shared Lecture",
            IsPublished = true
        };

        await _dbContext.Courses.AddRangeAsync(course1, course2);
        await _dbContext.Lectures.AddAsync(sharedLecture);
        await _dbContext.SaveChangesAsync();

        // Add same lecture to both courses
        course1.Lectures.Add(sharedLecture);
        course2.Lectures.Add(sharedLecture);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

        // Verify lecture is in both courses
        var updatedCourse1 = await _dbContext.Courses
            .Include(c => c.Lectures)
            .FirstAsync(c => c.Id == course1.Id);

        var updatedCourse2 = await _dbContext.Courses
            .Include(c => c.Lectures)
            .FirstAsync(c => c.Id == course2.Id);

        Assert.Contains(updatedCourse1.Lectures, l => l.Id == sharedLecture.Id);
        Assert.Contains(updatedCourse2.Lectures, l => l.Id == sharedLecture.Id);

        // Verify lecture has both courses
        var updatedLecture = await _dbContext.Lectures
            .Include(l => l.Courses)
            .FirstAsync(l => l.Id == sharedLecture.Id);

        Assert.Equal(2, updatedLecture.Courses.Count);
    }
}