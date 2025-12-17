using AlgoRhythm.Data;
using AlgoRhythm.Services.Users.Interfaces;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
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

            var response = await _httpClient.PostAsync("/api/authentication/login", content);

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


        [Fact]
        public async Task POST_ForgotPassword_ValidEmail_Returns200()
        {
            string email = $"forgot-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, "User", TestConstants.TestUserPassword, emailConfirmed: true);

            var request = new ResetPasswordRequestDto { Email = email };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/authentication/forgot-password", content);

            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("password reset code has been sent", responseBody);
        }

        [Fact]
        public async Task POST_ForgotPassword_UnverifiedEmail_Returns400()
        {
            string email = $"unverified-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, "User", TestConstants.TestUserPassword, emailConfirmed: false);

            var request = new ResetPasswordRequestDto { Email = email };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/authentication/forgot-password", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_ResetPassword_ValidCode_Returns200AndPasswordChanged()
        {
            string email = $"reset-{Guid.NewGuid()}@test.com";
            string resetCode = "123456";
            await AddUserToDb(email, "User", TestConstants.TestUserPassword, emailConfirmed: true, securityStamp: resetCode);

            var request = new ResetPasswordDto
            {
                Email = email,
                Code = resetCode,
                NewPassword = "NewPassword123!"
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/authentication/reset-password", content);

            response.EnsureSuccessStatusCode();

            _dbContext.ChangeTracker.Clear();
            var user = await _userManager.FindByEmailAsync(email);
            Assert.NotNull(user);

            var isNewPasswordValid = await _userManager.CheckPasswordAsync(user, "NewPassword123!");
            Assert.True(isNewPasswordValid);
        }

        [Fact]
        public async Task POST_ResetPassword_InvalidCode_Returns400()
        {
            string email = $"reset-invalid-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, "User", TestConstants.TestUserPassword, emailConfirmed: true, securityStamp: "123456");

            var request = new ResetPasswordDto
            {
                Email = email,
                Code = "wrong-code",
                NewPassword = "NewPassword123!"
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/authentication/reset-password", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_ChangePassword_ValidCurrentPassword_Returns200()
        {
            string email = $"change-pwd-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, "User", TestConstants.TestUserPassword, emailConfirmed: true);

            var loginResponse = await _authService.LoginAsync(new LoginRequest(email, TestConstants.TestUserPassword));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.Token);

            var request = new ChangePasswordDto
            {
                CurrentPassword = TestConstants.TestUserPassword,
                NewPassword = "NewSecurePassword123!"
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/authentication/change-password", content);

            response.EnsureSuccessStatusCode();

            _dbContext.ChangeTracker.Clear();
            var user = await _userManager.FindByEmailAsync(email);
            var isNewPasswordValid = await _userManager.CheckPasswordAsync(user!, "NewSecurePassword123!");
            Assert.True(isNewPasswordValid);
        }

        [Fact]
        public async Task POST_ChangePassword_InvalidCurrentPassword_Returns400()
        {
            string email = $"change-pwd-invalid-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, "User", TestConstants.TestUserPassword, emailConfirmed: true);

            var loginResponse = await _authService.LoginAsync(new LoginRequest(email, TestConstants.TestUserPassword));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.Token);

            var request = new ChangePasswordDto
            {
                CurrentPassword = "WrongPassword123!",
                NewPassword = "NewSecurePassword123!"
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/authentication/change-password", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_ChangePassword_Unauthorized_Returns401()
        {
            var request = new ChangePasswordDto
            {
                CurrentPassword = "OldPassword",
                NewPassword = "NewPassword123!"
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/authentication/change-password", content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PUT_UpdateProfile_ValidData_Returns200()
        {
            string email = $"update-profile-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, "User", TestConstants.TestUserPassword, emailConfirmed: true);

            var loginResponse = await _authService.LoginAsync(new LoginRequest(email, TestConstants.TestUserPassword));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.Token);

            var request = new UpdateUserProfileDto
            {
                FirstName = "UpdatedFirst",
                LastName = "UpdatedLast"
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync("/api/authentication/update-profile", content);

            response.EnsureSuccessStatusCode();

            _dbContext.ChangeTracker.Clear();
            var user = await _userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.Equal("UpdatedFirst", user.FirstName);
            Assert.Equal("UpdatedLast", user.LastName);
        }

        [Fact]
        public async Task PUT_UpdateProfile_ChangeEmail_Returns200AndEmailChanged()
        {
            string oldEmail = $"old-{Guid.NewGuid()}@test.com";
            string newEmail = $"new-{Guid.NewGuid()}@test.com";
            await AddUserToDb(oldEmail, "User", TestConstants.TestUserPassword, emailConfirmed: true);

            var loginResponse = await _authService.LoginAsync(new LoginRequest(oldEmail, TestConstants.TestUserPassword));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.Token);

            var request = new UpdateUserProfileDto
            {
                Email = newEmail
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync("/api/authentication/update-profile", content);

            response.EnsureSuccessStatusCode();

            _dbContext.ChangeTracker.Clear();
            var user = await _userManager.FindByEmailAsync(newEmail);
            Assert.NotNull(user);
            Assert.Equal(newEmail, user.Email);
        }

        [Fact]
        public async Task PUT_UpdateProfile_EmailAlreadyExists_Returns400()
        {
            string email1 = $"user1-{Guid.NewGuid()}@test.com";
            string email2 = $"user2-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email1, "User", TestConstants.TestUserPassword, emailConfirmed: true);
            await AddUserToDb(email2, "User", TestConstants.TestUserPassword, emailConfirmed: true);

            var loginResponse = await _authService.LoginAsync(new LoginRequest(email1, TestConstants.TestUserPassword));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.Token);

            var request = new UpdateUserProfileDto
            {
                Email = email2 // Try to change to already existing email
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync("/api/authentication/update-profile", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PUT_UpdateProfile_Unauthorized_Returns401()
        {
            var request = new UpdateUserProfileDto
            {
                FirstName = "Test"
            };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync("/api/authentication/update-profile", content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task POST_ResendVerificationCode_UnverifiedUser_Returns200()
        {
            string email = $"resend-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, "User", TestConstants.TestUserPassword, emailConfirmed: false);

            var request = new ResendVerificationCodeDto { Email = email };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/authentication/resend-verification-code", content);

            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task POST_ResendVerificationCode_AlreadyVerified_Returns400()
        {
            string email = $"verified-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, "User", TestConstants.TestUserPassword, emailConfirmed: true);

            var request = new ResendVerificationCodeDto { Email = email };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/authentication/resend-verification-code", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_ResendVerificationCode_UserNotFound_Returns400()
        {
            var request = new ResendVerificationCodeDto { Email = "nonexistent@test.com" };
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/authentication/resend-verification-code", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
