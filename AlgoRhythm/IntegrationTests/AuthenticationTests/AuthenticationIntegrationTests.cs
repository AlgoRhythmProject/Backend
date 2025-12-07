using AlgoRhythm.Data;
using AlgoRhythm.Services.Interfaces;
using AlgoRhythm.Shared.Dtos;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Json;
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
        public async Task POST_Register_InvalidCredentials_Returns400_BadRequest()
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
            await AddUserToDb(TestConstants.TestUserEmail, "User", TestConstants.TestUserPassword, false, TestConstants.TestUserSecurityStamp);
            
            VerifyEmailRequest req = new(TestConstants.TestUserEmail, TestConstants.TestUserSecurityStamp);
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/authentication/verify-email",
                content
            );

            response.EnsureSuccessStatusCode();
            _dbContext.ChangeTracker.Clear();

            User? user = await _userManager.FindByEmailAsync(TestConstants.TestUserEmail);

            Assert.NotNull(user);
            Assert.True(user.EmailConfirmed);
        }

        [Fact]
        public async Task POST_VerifyEmail_InvalidCredentials_Returns400_BadRequest()
        {
            await AddUserToDb(TestConstants.TestUserEmail, "User", TestConstants.TestUserPassword, false, TestConstants.TestUserSecurityStamp + "x");

            VerifyEmailRequest req = new(TestConstants.TestUserEmail, TestConstants.TestUserSecurityStamp);
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/authentication/verify-email",
                content
            );

            _dbContext.ChangeTracker.Clear();

            User? user = await _userManager.FindByEmailAsync(TestConstants.TestUserEmail);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(user);
            Assert.False(user.EmailConfirmed);
        }

    }
}
