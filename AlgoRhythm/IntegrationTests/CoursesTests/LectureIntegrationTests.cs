using AlgoRhythm.Data;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using AlgoRhythm.Shared.Models.Common;
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
    private readonly AlgoRhythmTestFixture _fixture;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    private readonly Guid _testCourseId = Guid.NewGuid();
    private readonly Guid _testLectureId = Guid.NewGuid();

    public LectureIntegrationTests(AlgoRhythmTestFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.ServerFactory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        _httpClient = fixture.ServerFactory.CreateClient();

        // Create test course and lecture
        var testCourse = new Course
        {
            Id = _testCourseId,
            Name = "Test Course for Lectures",
            Description = "Test Description",
            IsPublished = true
        };
        _dbContext.Courses.Add(testCourse);

        var testLecture = new Lecture
        {
            Id = _testLectureId,
            CourseId = _testCourseId,
            Title = "Test Lecture",
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

    private async Task<Course> AddCourseToDb(string name = "Test Course")
    {
        var course = new Course
        {
            Name = name + Guid.NewGuid(),
            Description = "Test Description",
            IsPublished = true
        };
        await _dbContext.Courses.AddAsync(course);
        await _dbContext.SaveChangesAsync();
        return course;
    }

    private async Task<Lecture> AddLectureToDb(Guid courseId, string title = "Test Lecture", bool isPublished = false)
    {
        var lecture = new Lecture
        {
            CourseId = courseId,
            Title = title + Guid.NewGuid(),
            IsPublished = isPublished
        };
        await _dbContext.Lectures.AddAsync(lecture);
        await _dbContext.SaveChangesAsync();
        return lecture;
    }

    [Fact]
    public async Task GET_GetAll_Returns200WithLectures()
    {
        await SetupAuthenticatedUser();
        var course = await AddCourseToDb();
        await AddLectureToDb(course.Id, "Lecture 1");
        await AddLectureToDb(course.Id, "Lecture 2");

        var response = await _httpClient.GetAsync("/api/lecture");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var lectures = JsonConvert.DeserializeObject<List<LectureDto>>(responseBody);

        Assert.NotNull(lectures);
        Assert.True(lectures.Count >= 2);
    }

    [Fact]
    public async Task GET_GetByCourseId_Returns200WithCourseLectures()
    {
        await SetupAuthenticatedUser();
        var course = await AddCourseToDb();
        await AddLectureToDb(course.Id, "Course Lecture");

        var response = await _httpClient.GetAsync($"/api/lecture/course/{course.Id}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var lectures = JsonConvert.DeserializeObject<List<LectureDto>>(responseBody);

        Assert.NotNull(lectures);
        Assert.All(lectures.Where(l => l.CourseId == course.Id), l => Assert.Equal(course.Id, l.CourseId));
    }

    [Fact]
    public async Task GET_GetById_ValidId_Returns200WithLecture()
    {
        await SetupAuthenticatedUser();
        var course = await AddCourseToDb();
        var lecture = await AddLectureToDb(course.Id);

        var response = await _httpClient.GetAsync($"/api/lecture/{lecture.Id}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var lectureDto = JsonConvert.DeserializeObject<LectureDto>(responseBody);

        Assert.NotNull(lectureDto);
        Assert.Equal(lecture.Id, lectureDto.Id);
        Assert.Equal(lecture.Title, lectureDto.Title);
    }

    [Fact]
    public async Task GET_GetById_InvalidId_Returns404()
    {
        await SetupAuthenticatedUser();
        var response = await _httpClient.GetAsync($"/api/lecture/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task POST_Create_ValidInput_Returns201AndCreatedLecture()
    {
        await SetupAuthenticatedUser();
        var course = await AddCourseToDb();
        var inputDto = new LectureInputDto
        {
            CourseId = course.Id,
            Title = "New Lecture",
            IsPublished = false
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(inputDto), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/lecture", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        var lectureDto = JsonConvert.DeserializeObject<LectureDto>(responseBody);

        Assert.NotNull(lectureDto);
        Assert.Equal(inputDto.Title, lectureDto.Title);
        Assert.Equal(inputDto.CourseId, lectureDto.CourseId);

        _dbContext.ChangeTracker.Clear();
        var dbLecture = await _dbContext.Lectures.FindAsync(lectureDto.Id);
        Assert.NotNull(dbLecture);
    }

    [Fact]
    public async Task PUT_Update_ValidInput_Returns204()
    {
        await SetupAuthenticatedUser();
        var course = await AddCourseToDb();
        var lecture = await AddLectureToDb(course.Id, "Original Title");
        var updateDto = new LectureInputDto
        {
            CourseId = course.Id,
            Title = "Updated Title",
            IsPublished = true
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"/api/lecture/{lecture.Id}", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var updatedLecture = await _dbContext.Lectures.FindAsync(lecture.Id);
        Assert.NotNull(updatedLecture);
        Assert.Equal(updateDto.Title, updatedLecture.Title);
        Assert.Equal(updateDto.IsPublished, updatedLecture.IsPublished);
    }

    [Fact]
    public async Task DELETE_Delete_ValidId_Returns204()
    {
        await SetupAuthenticatedUser();
        var course = await AddCourseToDb();
        var lecture = await AddLectureToDb(course.Id, "To Delete");

        var response = await _httpClient.DeleteAsync($"/api/lecture/{lecture.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        _dbContext.ChangeTracker.Clear();
        var deletedLecture = await _dbContext.Lectures.FindAsync(lecture.Id);
        Assert.Null(deletedLecture);
    }

    [Fact]
    public async Task POST_AddTag_ValidIds_Returns204()
    {
        await SetupAuthenticatedUser();
        var tag = new Tag { Name = "Test Tag" + Guid.NewGuid() };
        await _dbContext.Tags.AddAsync(tag);
        await _dbContext.SaveChangesAsync();

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
    public async Task DELETE_RemoveTag_ValidIds_Returns204()
    {
        await SetupAuthenticatedUser();

        // KLUCZOWA ZMIANA: Tag musi byæ najpierw zapisany do bazy danych
        var tag = new Tag { Name = "Test Tag" + Guid.NewGuid() };
        await _dbContext.Tags.AddAsync(tag);
        await _dbContext.SaveChangesAsync();

        // Teraz dodajemy tag do lektury
        var lecture = await _dbContext.Lectures
            .Include(l => l.Tags)
            .FirstAsync(l => l.Id == _testLectureId);
        lecture.Tags.Add(tag);
        await _dbContext.SaveChangesAsync();

        _dbContext.ChangeTracker.Clear();

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
    public async Task POST_AddContent_ValidTextContent_Returns201()
    {
        await SetupAuthenticatedUser();
        var contentDto = new LectureContentInputDto
        {
            Type = "Text",
            HtmlContent = "<p>Test Content</p>"
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(contentDto), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/api/lecture/{_testLectureId}/contents", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var lectureContentDto = JsonConvert.DeserializeObject<LectureContentDto>(responseBody);

        Assert.NotNull(lectureContentDto);
        Assert.Equal("Text", lectureContentDto.Type);
        Assert.Equal("<p>Test Content</p>", lectureContentDto.HtmlContent);
    }

    [Fact]
    public async Task GET_WithoutAuth_Returns401()
    {
        var response = await _httpClient.GetAsync("/api/lecture");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}