using AlgoRhythm.Data;
using AlgoRhythm.Services.Interfaces;
using AlgoRhythm.Shared.Models.Common;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IntegrationTests.CommentControllerTests
{
    public class CommentControllerIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
    {
        private readonly IServiceScope _scope;
        private readonly AlgoRhythmTestFixture _fixture;
        private readonly HttpClient _httpClient;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ApplicationDbContext _dbContext;

        private readonly Guid _testTaskId = Guid.NewGuid();
        private readonly string _controllerRoute = "/api/Comment";

        public CommentControllerIntegrationTests(AlgoRhythmTestFixture fixture)
        {
            _fixture = fixture;
            _scope = fixture.ServerFactory.Services.CreateScope();
            _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            _httpClient = fixture.ServerFactory.CreateClient();
            _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        private async Task<(string token, User user)> SetupAuthenticatedUser()
        {
            var userEmail = "testuser" + Guid.NewGuid() + "@example.com";
            var userPassword = "Password123!";

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

        private async Task<Comment> CreateCommentInDb(User author, string content, Guid? taskId = null)
        {
            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                Content = content,
                AuthorId = author.Id,
                TaskItemId = taskId ?? _testTaskId,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Comments.Add(comment);
            await _dbContext.SaveChangesAsync();
            return comment;
        }

        // --- POST ---

        [Fact]
        public async Task Create_Comment_Returns_Created()
        {
            var (token, user) = await SetupAuthenticatedUser();
            var dto = new Comment
            {
                Content = "New comment",
                TaskItemId = _testTaskId
            };

            var response = await _httpClient.PostAsJsonAsync(_controllerRoute, dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdComment = await response.Content.ReadFromJsonAsync<Comment>();
            Assert.Equal(dto.Content, createdComment!.Content);
        }

        [Fact]
        public async Task Create_Comment_Unauthenticated_Returns_Unauthorized()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            var dto = new Comment
            {
                Content = "No auth",
                TaskItemId = _testTaskId
            };

            var response = await _httpClient.PostAsJsonAsync(_controllerRoute, dto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // --- GET ---

        [Fact]
        public async Task GetById_ExistingComment_Returns_Ok()
        {
            var (_, user) = await SetupAuthenticatedUser();
            var commentId = await CreateCommentInDb(user, "Retrieve me");

            var response = await _httpClient.GetAsync($"{_controllerRoute}/{commentId}");

            response.EnsureSuccessStatusCode();
            var comment = await response.Content.ReadFromJsonAsync<Comment>();
            Assert.NotNull(comment);
            Assert.Equal("Retrieve me", comment!.Content);
        }

        [Fact]
        public async Task GetById_NonExistingComment_Returns_NotFound()
        {
            await SetupAuthenticatedUser();
            var response = await _httpClient.GetAsync($"{_controllerRoute}/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetByTask_Returns_Comments()
        {
            var (_, user) = await SetupAuthenticatedUser();
            await CreateCommentInDb(user, "Comment 1");
            await CreateCommentInDb(user, "Comment 2");

            var response = await _httpClient.GetAsync($"{_controllerRoute}/task/{_testTaskId}");

            response.EnsureSuccessStatusCode();
            var comments = await response.Content.ReadFromJsonAsync<IEnumerable<Comment>>();
            Assert.NotNull(comments);
            Assert.Contains(comments, c => c.Content == "Comment 1");
            Assert.Contains(comments, c => c.Content == "Comment 2");
        }

        // --- PUT ---

        [Fact]
        public async Task Update_OwnComment_Returns_NoContent()
        {
            var (_, user) = await SetupAuthenticatedUser();
            var commentId = await CreateCommentInDb(user, "Old");

            var response = await _httpClient.PutAsJsonAsync($"{_controllerRoute}/{commentId}", "Updated");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Update_OtherUsersComment_Returns_Forbidden()
        {
            var (_, otherUser) = await SetupAuthenticatedUser();
            var commentId = await CreateCommentInDb(otherUser, "Other's comment");

            await SetupAuthenticatedUser(); // now logged as different user
            var response = await _httpClient.PutAsJsonAsync($"{_controllerRoute}/{commentId}", "Hacked");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // --- DELETE ---

        [Fact]
        public async Task Delete_OwnComment_Returns_NoContent()
        {
            var (_, user) = await SetupAuthenticatedUser();
            var commentId = await CreateCommentInDb(user, "To delete");

            var response = await _httpClient.DeleteAsync($"{_controllerRoute}/{commentId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Delete_OtherUsersComment_Returns_Forbidden()
        {
            var (_, otherUser) = await SetupAuthenticatedUser();
            var commentId = await CreateCommentInDb(otherUser, "Other user's comment");

            await SetupAuthenticatedUser(); // different user
            var response = await _httpClient.DeleteAsync($"{_controllerRoute}/{commentId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
