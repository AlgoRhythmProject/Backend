using AlgoRhythm.Data;
using AlgoRhythm.Services.Interfaces;
using AlgoRhythm.Shared.Dtos;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace IntegrationTests.AuthenticationTests
{
    public class AuthenticationIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
    {
        private readonly IServiceScope _scope;
        private readonly AlgoRhythmTestFixture _fixture;
        private readonly ApplicationDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IAuthService _authService;

        public AuthenticationIntegrationTests(AlgoRhythmTestFixture fixture)
        {
            _fixture = fixture;
            _scope = fixture.ServerFactory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            _authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
            _httpClient = fixture.ServerFactory.CreateClient();
        }

        private async Task AddUserToDb(
            string email, 
            string roleName, 
            string password, 
            bool emailConfirmed = true,
            string? securityStamp = null)
        {
            var user = new User
            {
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "User",
                EmailConfirmed = emailConfirmed,
            };

            await _userManager.CreateAsync(user, password);
            await _userManager.AddToRoleAsync(user, roleName);

            if (securityStamp is not null)
            {
                user.SecurityStamp = securityStamp;
                await _userManager.UpdateAsync(user);
            }
        }


        [Fact]
        public async Task POST_Login_ValidCredentials_Returns200WithToken_User()
        {
            await AddUserToDb(TestConstants.TestUserEmail, "User", TestConstants.TestUserPassword);
            LoginRequest req = new(TestConstants.TestUserEmail, TestConstants.TestUserPassword);
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/authentication/login", 
                content
            );

            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseBody);

            Assert.NotNull(authResponse);
            Assert.NotNull(authResponse.Token);
            Assert.NotEmpty(authResponse.Token);
            Assert.Equal(TestConstants.TestUserEmail, authResponse.User.Email);
        }

        [Fact]
        public async Task POST_Login_InvalidCredentials_Returns401_Unauthorized()
        {
            await AddUserToDb(TestConstants.TestUserEmail, "User", TestConstants.TestUserPassword);
            LoginRequest req = new(TestConstants.TestUserEmail, TestConstants.TestUserPassword + " ");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/authentication/login",
                content
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);   
        }

        [Fact]
        public async Task POST_Register_ExistingUser_Returns400_BadRequest()
        {
            await AddUserToDb(TestConstants.TestUserEmail, "User", TestConstants.TestUserPassword);
            RegisterRequest req = new(TestConstants.TestUserEmail, TestConstants.TestUserPassword, "Random", "Random");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/authentication/register",
                content
            );

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_VerifyEmail_ValidCredentials_Returns200_Ok()
        {
            string userEmail = Guid.NewGuid() + TestConstants.TestUserEmail;
            await AddUserToDb(userEmail, "User", TestConstants.TestUserPassword, false, TestConstants.TestUserSecurityStamp);
            
            VerifyEmailRequest req = new(userEmail, TestConstants.TestUserSecurityStamp);
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/authentication/verify-email",
                content
            );

            response.EnsureSuccessStatusCode();
            _dbContext.ChangeTracker.Clear();

            User? user = await _userManager.FindByEmailAsync(userEmail);

            Assert.NotNull(user);
            Assert.True(user.EmailConfirmed);
        }

        [Fact]
        public async Task POST_VerifyEmail_InvalidCredentials_Returns400_BadRequest()
        {
            string userEmail = Guid.NewGuid() + TestConstants.TestUserEmail;
            await AddUserToDb(userEmail, "User", TestConstants.TestUserPassword, false, TestConstants.TestUserSecurityStamp + "x");

            VerifyEmailRequest req = new(userEmail, TestConstants.TestUserSecurityStamp);
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/authentication/verify-email",
                content
            );

            _dbContext.ChangeTracker.Clear();

            User? user = await _userManager.FindByEmailAsync(userEmail);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(user);
            Assert.False(user.EmailConfirmed);
        }

        [Fact]
        public async Task POST_VerifyEmail_UserNotFound_Returns400_BadRequest()
        {
            string userEmail = Guid.NewGuid() + TestConstants.TestUserEmail;

            VerifyEmailRequest req = new(userEmail, TestConstants.TestUserSecurityStamp);
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/authentication/verify-email",
                content
            );

            _dbContext.ChangeTracker.Clear();

            User? user = await _userManager.FindByEmailAsync(userEmail);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Null(user);
        }

        [Fact]
        public async Task POST_VerifyEmail_AlreadyConfirmed()
        {
            string userEmail = Guid.NewGuid() + TestConstants.TestUserEmail;
            await AddUserToDb(userEmail, "User", TestConstants.TestUserPassword);
            User? user_before = await _userManager.FindByEmailAsync(userEmail);

            VerifyEmailRequest req = new(userEmail, TestConstants.TestUserSecurityStamp);
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/authentication/verify-email",
                content
            );

            response.EnsureSuccessStatusCode();
            _dbContext.ChangeTracker.Clear();

            User? user = await _userManager.FindByEmailAsync(userEmail);
           
            Assert.NotNull(user);
            Assert.NotNull(user_before);
            Assert.True(user.EmailConfirmed);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(user_before.PasswordHash, user.PasswordHash);
            Assert.Equal(user_before.Email, user.Email);
            Assert.Equal(user_before.SecurityStamp, user.SecurityStamp);
            Assert.Equal(user_before.UpdatedAt, user.UpdatedAt);
        }

        [Fact]
        public async Task POST_Register_InvalidInput_Returns400_BadRequest()
        {
            List<RegisterRequest> reqs = 
            [
                new(string.Empty, TestConstants.TestUserPassword, "Random", "Random"),
                new(TestConstants.TestUserEmail, string.Empty, "Random", "Random"),
                new(TestConstants.TestUserEmail, TestConstants.TestUserPassword, string.Empty, "Random"),
                new(TestConstants.TestUserEmail, TestConstants.TestUserPassword, "Random", string.Empty), 
            ];

            List<StringContent> contents = reqs.Select(req => new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json")).ToList();
            List<Task<HttpResponseMessage>> tasks = [];

            foreach (StringContent content in contents)
            {
                Task<HttpResponseMessage> postTask = _httpClient.PostAsync(
                    "/api/authentication/register",
                    content
                );

                tasks.Add(postTask);
            }

            HttpResponseMessage[] responses = await Task.WhenAll(tasks);

            Assert.All(responses, r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
        }

        [Fact]
        public async Task POST_Login_UserNotFound_Returns401_Unauthorized()
        {
            LoginRequest req = new(TestConstants.TestUserEmail + Guid.NewGuid(), TestConstants.TestUserPassword);
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/authentication/login",
                content
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task POST_Login_EmailNotConfirmed_Returns401_Unauthorized()
        {
            await AddUserToDb(TestConstants.TestUserEmail + Guid.NewGuid(), "User", TestConstants.TestUserPassword, false, TestConstants.TestUserSecurityStamp);
            LoginRequest req = new(TestConstants.TestUserEmail + Guid.NewGuid(), TestConstants.TestUserPassword);
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/authentication/login",
                content
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task POST_Register_NewUserSuccess_Returns200AndUserCreated()
        {
            string email = TestConstants.TestUserEmail + Guid.NewGuid();
            RegisterRequest req = new(email, TestConstants.TestUserPassword, "Random", "Random");
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/authentication/register",
                content
            );

            _dbContext.ChangeTracker.Clear();

            User? user = await _userManager.FindByEmailAsync(email);
            response.EnsureSuccessStatusCode();

            Assert.NotNull(user);
            Assert.False(user.EmailConfirmed);
            Assert.Equal(user.FirstName, req.FirstName);
            Assert.Equal(user.LastName, req.LastName);

            bool isPasswordValid = await _userManager.CheckPasswordAsync(
                user,
                req.Password 
            );
            Assert.True(isPasswordValid);
        }

    }
}
