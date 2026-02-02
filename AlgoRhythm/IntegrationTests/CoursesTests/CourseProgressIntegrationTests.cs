using AlgoRhythm.Data;
using AlgoRhythm.Services.Courses.Interfaces;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Courses;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Shared.Models.Courses;
using AlgoRhythm.Shared.Models.Tasks;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.CoursesTests;

public class CourseProgressIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
{
    private readonly IServiceScope _scope;
    private readonly AlgoRhythmTestFixture _fixture;
    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ICourseProgressService _progressService;

    public CourseProgressIntegrationTests(AlgoRhythmTestFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.ServerFactory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        _progressService = _scope.ServiceProvider.GetRequiredService<ICourseProgressService>();
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

    private async Task<Course> CreateTestCourse()
    {
        var course = new Course
        {
            Name = $"Test Course {Guid.NewGuid()}",
            Description = "Test Description",
            IsPublished = true
        };

        await _dbContext.Courses.AddAsync(course);
        await _dbContext.SaveChangesAsync();
        return course;
    }

    private async Task<Lecture> CreateTestLecture(Course course)
    {
        var lecture = new Lecture
        {
            Title = $"Test Lecture {Guid.NewGuid()}",
            IsPublished = true,
            Courses = new List<Course> { course }
        };

        await _dbContext.Lectures.AddAsync(lecture);
        course.Lectures.Add(lecture);
        await _dbContext.SaveChangesAsync();
        return lecture;
    }

    private async Task<ProgrammingTaskItem> CreateTestTask(Course course)
    {
        var task = new ProgrammingTaskItem
        {
            Title = $"Test Task {Guid.NewGuid()}",
            Description = "Test Description",
            Difficulty = Difficulty.Easy,
            IsPublished = true,
            TemplateCode = "public class Solution { }",
            Courses = new List<Course> { course }
        };

        await _dbContext.ProgrammingTaskItems.AddAsync(task);
        course.TaskItems.Add(task);
        await _dbContext.SaveChangesAsync();
        return task;
    }

    [Fact]
    public async Task GET_MyProgress_AuthenticatedUser_Returns200()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var course = await CreateTestCourse();

        await _progressService.InitializeCourseForAllUsersAsync(course.Id, CancellationToken.None);

        // Act
        var response = await _httpClient.GetAsync("/api/courseprogress/my-progress");

        // Assert
        response.EnsureSuccessStatusCode();
        var progresses = await response.Content.ReadFromJsonAsync<List<CourseProgressDto>>();
        Assert.NotNull(progresses);
        Assert.NotEmpty(progresses);
    }

    [Fact]
    public async Task GET_MyCourseProgress_SpecificCourse_Returns200()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var course = await CreateTestCourse();

        await _progressService.InitializeCourseForAllUsersAsync(course.Id, CancellationToken.None);

        // Act
        var response = await _httpClient.GetAsync($"/api/courseprogress/my-progress/{course.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var progress = await response.Content.ReadFromJsonAsync<CourseProgressDto>();
        Assert.NotNull(progress);
        Assert.Equal(course.Id, progress.CourseId);
        Assert.Equal(0, progress.Percentage);
    }

    [Fact]
    public async Task POST_ToggleLectureCompletion_MarkAsCompleted_Returns200()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var course = await CreateTestCourse();
        var lecture = await CreateTestLecture(course);

        await _progressService.InitializeCourseForAllUsersAsync(course.Id, CancellationToken.None);

        // Act - Mark as completed
        var response = await _httpClient.PostAsync($"/api/courseprogress/lecture/{lecture.Id}/toggle", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);

        // Verify completion
        var isCompleted = await _progressService.IsLectureCompletedAsync(user.Id, lecture.Id, CancellationToken.None);
        Assert.True(isCompleted);
    }

    [Fact]
    public async Task POST_ToggleLectureCompletion_UnmarkCompleted_Returns200()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var course = await CreateTestCourse();
        var lecture = await CreateTestLecture(course);

        await _progressService.InitializeCourseForAllUsersAsync(course.Id, CancellationToken.None);

        // Mark as completed first
        await _progressService.ToggleLectureCompletionAsync(user.Id, lecture.Id, CancellationToken.None);

        // Act - Toggle again to unmark
        var response = await _httpClient.PostAsync($"/api/courseprogress/lecture/{lecture.Id}/toggle", null);

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify not completed
        var isCompleted = await _progressService.IsLectureCompletedAsync(user.Id, lecture.Id, CancellationToken.None);
        Assert.False(isCompleted);
    }

    [Fact]
    public async Task POST_MarkLectureAsCompleted_Returns200()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var course = await CreateTestCourse();
        var lecture = await CreateTestLecture(course);

        await _progressService.InitializeCourseForAllUsersAsync(course.Id, CancellationToken.None);

        // Act
        var response = await _httpClient.PostAsync($"/api/courseprogress/lecture/{lecture.Id}/complete", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var isCompleted = await _progressService.IsLectureCompletedAsync(user.Id, lecture.Id, CancellationToken.None);
        Assert.True(isCompleted);
    }

    [Fact]
    public async Task POST_MarkLectureAsIncomplete_Returns200()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var course = await CreateTestCourse();
        var lecture = await CreateTestLecture(course);

        await _progressService.InitializeCourseForAllUsersAsync(course.Id, CancellationToken.None);

        // Mark as completed first
        await _progressService.MarkLectureAsCompletedAsync(user.Id, lecture.Id, CancellationToken.None);

        // Act
        var response = await _httpClient.PostAsync($"/api/courseprogress/lecture/{lecture.Id}/uncomplete", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var isCompleted = await _progressService.IsLectureCompletedAsync(user.Id, lecture.Id, CancellationToken.None);
        Assert.False(isCompleted);
    }

    [Fact]
    public async Task POST_RecalculateProgress_Updates_Percentage()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var course = await CreateTestCourse();
        var lecture1 = await CreateTestLecture(course);
        var lecture2 = await CreateTestLecture(course);

        await _progressService.InitializeCourseForAllUsersAsync(course.Id, CancellationToken.None);

        // Complete one lecture
        await _progressService.MarkLectureAsCompletedAsync(user.Id, lecture1.Id, CancellationToken.None);

        // Act
        var response = await _httpClient.PostAsync($"/api/courseprogress/recalculate/{course.Id}", null);

        // Assert
        response.EnsureSuccessStatusCode();


        _dbContext.ChangeTracker.Clear();
        var courseFromDb = await _dbContext.Courses
            .Include(c => c.Lectures)
            .Include(c => c.TaskItems)
            .FirstOrDefaultAsync(c => c.Id == course.Id);

        var totalItems = courseFromDb!.Lectures.Count + courseFromDb.TaskItems.Count;
        var expectedPercentage = totalItems > 0 ? (int)Math.Round(100.0 / totalItems) : 0;

        var progress = await _progressService.GetByUserAndCourseAsync(user.Id, course.Id, CancellationToken.None);
        Assert.NotNull(progress);
        Assert.Equal(expectedPercentage, progress.Percentage);
    }

    [Fact]
    public async Task GET_IsLectureCompleted_CompletedLecture_ReturnsTrue()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var course = await CreateTestCourse();
        var lecture = await CreateTestLecture(course);

        await _progressService.MarkLectureAsCompletedAsync(user.Id, lecture.Id, CancellationToken.None);

        // Act
        var response = await _httpClient.GetAsync($"/api/courseprogress/lecture/{lecture.Id}/is-completed");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LectureCompletionDto>();
        Assert.NotNull(result);
        Assert.True(result.IsCompleted);
    }

    [Fact]
    public async Task GET_CompletedLectureIds_ReturnsCorrectIds()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var course = await CreateTestCourse();
        var lecture1 = await CreateTestLecture(course);
        var lecture2 = await CreateTestLecture(course);

        await _progressService.MarkLectureAsCompletedAsync(user.Id, lecture1.Id, CancellationToken.None);

        // Act
        var response = await _httpClient.GetAsync($"/api/courseprogress/course/{course.Id}/completed-lectures");

        // Assert
        response.EnsureSuccessStatusCode();
        var completedIds = await response.Content.ReadFromJsonAsync<HashSet<Guid>>();
        Assert.NotNull(completedIds);
        Assert.Contains(lecture1.Id, completedIds);
        Assert.DoesNotContain(lecture2.Id, completedIds);
    }

    [Fact]
    public async Task GET_CompletedTaskIds_ReturnsCorrectIds()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var course = await CreateTestCourse();
        var task = await CreateTestTask(course);

        // Mark task as completed
        _dbContext.ChangeTracker.Clear();
        var userEntity = await _dbContext.Users.FindAsync(user.Id);
        var taskEntity = await _dbContext.TaskItems.FindAsync(task.Id);
        userEntity!.CompletedTasks.Add(taskEntity!);
        await _dbContext.SaveChangesAsync();

        // Act
        var response = await _httpClient.GetAsync($"/api/courseprogress/course/{course.Id}/completed-tasks");

        // Assert
        response.EnsureSuccessStatusCode();
        var completedIds = await response.Content.ReadFromJsonAsync<HashSet<Guid>>();
        Assert.NotNull(completedIds);
        Assert.Contains(task.Id, completedIds);
    }

    [Fact]
    public async Task GET_MyCompletedLectures_ReturnsAllCompleted()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var course = await CreateTestCourse();
        var lecture1 = await CreateTestLecture(course);
        var lecture2 = await CreateTestLecture(course);

        await _progressService.MarkLectureAsCompletedAsync(user.Id, lecture1.Id, CancellationToken.None);
        await _progressService.MarkLectureAsCompletedAsync(user.Id, lecture2.Id, CancellationToken.None);

        // Act
        var response = await _httpClient.GetAsync("/api/courseprogress/my-completed-lectures");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UserCompletedLecturesDto>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCompleted);
        Assert.Contains(lecture1.Id, result.CompletedLectureIds);
        Assert.Contains(lecture2.Id, result.CompletedLectureIds);
    }

    [Fact]
    public async Task CompleteAllCourseItems_MarksAs100Percent()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var course = await CreateTestCourse();
        var lecture = await CreateTestLecture(course);

        await _progressService.InitializeCourseForAllUsersAsync(course.Id, CancellationToken.None);


        _dbContext.ChangeTracker.Clear();

        var courseFromDb = await _dbContext.Courses
            .Include(c => c.Lectures)
            .Include(c => c.TaskItems)
            .FirstOrDefaultAsync(c => c.Id == course.Id);

        // Act - Complete all items (lecture only in this case)
        await _progressService.MarkLectureAsCompletedAsync(user.Id, lecture.Id, CancellationToken.None);
        await _progressService.RecalculateProgressAsync(user.Id, course.Id, CancellationToken.None);

        // Assert
        var progress = await _progressService.GetByUserAndCourseAsync(user.Id, course.Id, CancellationToken.None);
        Assert.NotNull(progress);
        
        var totalItems = courseFromDb!.Lectures.Count + courseFromDb.TaskItems.Count;
        Assert.Equal(totalItems, 1);
        Assert.Equal(100, progress.Percentage);
        Assert.NotNull(progress.CompletedAt);
    }

    [Fact]
    public async Task UncompleteLecture_UpdatesProgressPercentage()
    {
        // Arrange
        var (_, user) = await SetupAuthenticatedUser();
        var course = await CreateTestCourse();
        var lecture = await CreateTestLecture(course);

        await _progressService.InitializeCourseForAllUsersAsync(course.Id, CancellationToken.None);
        await _progressService.MarkLectureAsCompletedAsync(user.Id, lecture.Id, CancellationToken.None);
        await _progressService.RecalculateProgressAsync(user.Id, course.Id, CancellationToken.None);

        // Act - Uncomplete
        await _progressService.MarkLectureAsIncompletedAsync(user.Id, lecture.Id, CancellationToken.None);
        await _progressService.RecalculateProgressAsync(user.Id, course.Id, CancellationToken.None);

        // Assert
        var progress = await _progressService.GetByUserAndCourseAsync(user.Id, course.Id, CancellationToken.None);
        Assert.NotNull(progress);
        Assert.Equal(0, progress.Percentage);
        Assert.Null(progress.CompletedAt);
    }
}