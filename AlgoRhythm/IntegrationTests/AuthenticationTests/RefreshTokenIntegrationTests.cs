using AlgoRhythm.Data;
using AlgoRhythm.Shared.Dtos.Users;
using AlgoRhythm.Shared.Models.Users;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace IntegrationTests.AuthenticationTests
{
    public class RefreshTokenIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
    {
        private readonly AlgoRhythmTestFixture _fixture;
        private readonly HttpClient _httpClient;

        public RefreshTokenIntegrationTests(AlgoRhythmTestFixture fixture)
        {
            _fixture = fixture;
            _httpClient = fixture.ServerFactory.CreateClient();
        }

        private async Task<User> AddUserToDb(string email, string password)
        {
            using var scope = _fixture.ServerFactory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            var user = new User
            {
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "User",
                EmailConfirmed = true
            };

            await userManager.CreateAsync(user, password);
            await userManager.AddToRoleAsync(user, "User");

            return user;
        }

        private string? ExtractCookieValue(HttpResponseMessage response, string cookieName)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
                return null;

            var cookie = cookies.FirstOrDefault(c => c.StartsWith($"{cookieName}="));
            if (cookie == null)
                return null;

            // Extract value between "CookieName=" and first ";"
            var startIndex = cookie.IndexOf('=') + 1;
            var endIndex = cookie.IndexOf(';', startIndex);
            if (endIndex == -1)
                endIndex = cookie.Length;

            return cookie.Substring(startIndex, endIndex - startIndex);
        }

        [Fact]
        public async Task POST_RefreshToken_WithValidToken_ShouldReturnNewTokens()
        {
            // Arrange - Login to get initial tokens
            string email = $"refresh-valid-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, TestConstants.TestUserPassword);

            var loginRequest = new LoginRequest(email, TestConstants.TestUserPassword);
            var loginContent = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");
            var loginResponse = await _httpClient.PostAsync("/api/authentication/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();

            var refreshTokenValue = ExtractCookieValue(loginResponse, "RefreshToken");
            var jwtTokenValue = ExtractCookieValue(loginResponse, "JWT");
            Assert.NotNull(refreshTokenValue);
            Assert.NotNull(jwtTokenValue);

            // Wait a moment to ensure new tokens will have different timestamps
            await Task.Delay(1000);

            // Act - Call refresh-token endpoint
            var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/refresh-token");
            refreshRequest.Headers.Add("Cookie", $"RefreshToken={refreshTokenValue}");

            var refreshResponse = await _httpClient.SendAsync(refreshRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

            var responseBody = await refreshResponse.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RefreshTokenResponseDto>(responseBody);

            Assert.NotNull(result);
            Assert.NotEmpty(result.AccessToken);
            Assert.NotEmpty(result.RefreshToken);
            Assert.True(result.AccessTokenExpiresUtc > DateTime.UtcNow);
            Assert.True(result.RefreshTokenExpiresUtc > DateTime.UtcNow);

            // Verify new access token is different from old one
            Assert.NotEqual(jwtTokenValue, result.AccessToken);

            // Verify new cookies were set
            var newJwtCookie = ExtractCookieValue(refreshResponse, "JWT");
            var newRefreshCookie = ExtractCookieValue(refreshResponse, "RefreshToken");
            Assert.NotNull(newJwtCookie);
            Assert.NotNull(newRefreshCookie);
            Assert.NotEqual(refreshTokenValue, newRefreshCookie);
        }

        [Fact]
        public async Task POST_RefreshToken_WithoutCookie_ShouldReturnBadRequest()
        {
            // Act
            var response = await _httpClient.PostAsync("/api/authentication/refresh-token", null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync();
            var error = JsonConvert.DeserializeObject<ErrorResponse>(responseBody);

            Assert.NotNull(error);
            Assert.Equal("MISSING_TOKEN", error.Code);
            Assert.Contains("Refresh token is required", error.Message);
        }

        [Fact]
        public async Task POST_RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
        {
            // Arrange - Use fake token
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/refresh-token");
            request.Headers.Add("Cookie", "RefreshToken=invalid_fake_token_12345");

            // Act
            var response = await _httpClient.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            var responseBody = await response.Content.ReadAsStringAsync();
            var error = JsonConvert.DeserializeObject<ErrorResponse>(responseBody);

            Assert.NotNull(error);
            Assert.Equal("INVALID_REFRESH_TOKEN", error.Code);
        }

        [Fact]
        public async Task POST_RefreshToken_WithExpiredToken_ShouldReturnUnauthorized()
        {
            // Arrange - Create user and expired token directly in database
            string email = $"refresh-expired-{Guid.NewGuid()}@test.com";
            var user = await AddUserToDb(email, TestConstants.TestUserPassword);

            using (var scope = _fixture.ServerFactory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var expiredToken = new RefreshToken
                {
                    UserId = user.Id,
                    Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired yesterday
                    CreatedByIp = "127.0.0.1",
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                };

                dbContext.RefreshTokens.Add(expiredToken);
                await dbContext.SaveChangesAsync();
            }

            using (var scope = _fixture.ServerFactory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var token = await dbContext.RefreshTokens.FirstAsync(rt => rt.UserId == user.Id);

                var request = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/refresh-token");
                request.Headers.Add("Cookie", $"RefreshToken={token.Token}");

                // Act
                var response = await _httpClient.SendAsync(request);

                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

                var responseBody = await response.Content.ReadAsStringAsync();
                var error = JsonConvert.DeserializeObject<ErrorResponse>(responseBody);

                Assert.NotNull(error);
                Assert.Equal("INVALID_REFRESH_TOKEN", error.Code);
            }
        }

        [Fact]
        public async Task POST_RefreshToken_WithRevokedToken_ShouldReturnUnauthorized()
        {
            // Arrange - Create user and revoked token
            string email = $"refresh-revoked-{Guid.NewGuid()}@test.com";
            var user = await AddUserToDb(email, TestConstants.TestUserPassword);

            using (var scope = _fixture.ServerFactory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var revokedToken = new RefreshToken
                {
                    UserId = user.Id,
                    Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedByIp = "127.0.0.1",
                    CreatedAt = DateTime.UtcNow,
                    RevokedAt = DateTime.UtcNow.AddMinutes(-5), // Revoked 5 minutes ago
                    RevokedByIp = "127.0.0.1"
                };

                dbContext.RefreshTokens.Add(revokedToken);
                await dbContext.SaveChangesAsync();
            }

            using (var scope = _fixture.ServerFactory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var token = await dbContext.RefreshTokens.FirstAsync(rt => rt.UserId == user.Id);

                var request = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/refresh-token");
                request.Headers.Add("Cookie", $"RefreshToken={token.Token}");

                // Act
                var response = await _httpClient.SendAsync(request);

                // Assert
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }

        [Fact]
        public async Task POST_RefreshToken_ShouldRevokeOldToken_TokenRotation()
        {
            // Arrange - Login to get initial token
            string email = $"refresh-rotation-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, TestConstants.TestUserPassword);

            var loginRequest = new LoginRequest(email, TestConstants.TestUserPassword);
            var loginContent = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");
            var loginResponse = await _httpClient.PostAsync("/api/authentication/login", loginContent);

            var oldRefreshToken = ExtractCookieValue(loginResponse, "RefreshToken");
            Assert.NotNull(oldRefreshToken);

            // Act - Use refresh token
            var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/refresh-token");
            refreshRequest.Headers.Add("Cookie", $"RefreshToken={oldRefreshToken}");
            var refreshResponse = await _httpClient.SendAsync(refreshRequest);

            refreshResponse.EnsureSuccessStatusCode();

            // Assert - Check that old token was revoked in database using NEW SCOPE
            using var scope = _fixture.ServerFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var revokedToken = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == oldRefreshToken);

            Assert.NotNull(revokedToken);
            Assert.NotNull(revokedToken.RevokedAt);
            Assert.NotEmpty(revokedToken.ReplacedByToken);
            Assert.NotNull(revokedToken.RevokedByIp);
        }

        [Fact]
        public async Task POST_RefreshToken_OldTokenCannotBeReused()
        {
            // Arrange - Login and refresh once
            string email = $"refresh-reuse-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, TestConstants.TestUserPassword);

            var loginRequest = new LoginRequest(email, TestConstants.TestUserPassword);
            var loginContent = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");
            var loginResponse = await _httpClient.PostAsync("/api/authentication/login", loginContent);

            var oldRefreshToken = ExtractCookieValue(loginResponse, "RefreshToken");

            // First refresh - should succeed
            var firstRefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/refresh-token");
            firstRefreshRequest.Headers.Add("Cookie", $"RefreshToken={oldRefreshToken}");
            var firstRefreshResponse = await _httpClient.SendAsync(firstRefreshRequest);
            firstRefreshResponse.EnsureSuccessStatusCode();

            // Act - Try to reuse the old token
            var secondRefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/refresh-token");
            secondRefreshRequest.Headers.Add("Cookie", $"RefreshToken={oldRefreshToken}");
            var secondRefreshResponse = await _httpClient.SendAsync(secondRefreshRequest);

            // Assert - Should fail
            Assert.Equal(HttpStatusCode.Unauthorized, secondRefreshResponse.StatusCode);
        }

        [Fact]
        public async Task POST_RefreshToken_MultipleActiveTokensForSameUser_AllWorkIndependently()
        {
            // Arrange - Login from two different "devices" (two separate login calls)
            string email = $"refresh-multi-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, TestConstants.TestUserPassword);

            var loginRequest = new LoginRequest(email, TestConstants.TestUserPassword);
            var loginContent = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");

            // Device 1 login
            var device1LoginResponse = await _httpClient.PostAsync("/api/authentication/login", loginContent);
            var device1Token = ExtractCookieValue(device1LoginResponse, "RefreshToken");

            // Device 2 login
            var device2LoginResponse = await _httpClient.PostAsync("/api/authentication/login", loginContent);
            var device2Token = ExtractCookieValue(device2LoginResponse, "RefreshToken");

            Assert.NotNull(device1Token);
            Assert.NotNull(device2Token);
            Assert.NotEqual(device1Token, device2Token);

            // Act - Refresh both tokens
            var device1RefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/refresh-token");
            device1RefreshRequest.Headers.Add("Cookie", $"RefreshToken={device1Token}");
            var device1RefreshResponse = await _httpClient.SendAsync(device1RefreshRequest);

            var device2RefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/refresh-token");
            device2RefreshRequest.Headers.Add("Cookie", $"RefreshToken={device2Token}");
            var device2RefreshResponse = await _httpClient.SendAsync(device2RefreshRequest);

            // Assert - Both should succeed
            Assert.Equal(HttpStatusCode.OK, device1RefreshResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, device2RefreshResponse.StatusCode);

            var device1NewToken = ExtractCookieValue(device1RefreshResponse, "RefreshToken");
            var device2NewToken = ExtractCookieValue(device2RefreshResponse, "RefreshToken");

            Assert.NotNull(device1NewToken);
            Assert.NotNull(device2NewToken);
            Assert.NotEqual(device1NewToken, device2NewToken);
        }

        [Fact]
        public async Task POST_RefreshToken_NewAccessToken_CanBeUsedForAuthenticatedEndpoint()
        {
            // Arrange - Login
            string email = $"refresh-auth-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, TestConstants.TestUserPassword);

            var loginRequest = new LoginRequest(email, TestConstants.TestUserPassword);
            var loginContent = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");
            var loginResponse = await _httpClient.PostAsync("/api/authentication/login", loginContent);

            var refreshTokenValue = ExtractCookieValue(loginResponse, "RefreshToken");

            // Act - Refresh token
            var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/refresh-token");
            refreshRequest.Headers.Add("Cookie", $"RefreshToken={refreshTokenValue}");
            var refreshResponse = await _httpClient.SendAsync(refreshRequest);

            var responseBody = await refreshResponse.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RefreshTokenResponseDto>(responseBody);

            // Assert - Use new access token to access protected endpoint
            var protectedRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/logout");
            protectedRequest.Headers.Add("Cookie", $"JWT={result!.AccessToken}");

            var protectedResponse = await _httpClient.SendAsync(protectedRequest);

            Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
        }

        [Fact]
        public async Task POST_RevokeToken_WithValidToken_ShouldRevoke()
        {
            // Arrange - Login
            string email = $"revoke-valid-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, TestConstants.TestUserPassword);

            var loginRequest = new LoginRequest(email, TestConstants.TestUserPassword);
            var loginContent = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");
            var loginResponse = await _httpClient.PostAsync("/api/authentication/login", loginContent);

            var refreshTokenValue = ExtractCookieValue(loginResponse, "RefreshToken");
            var jwtTokenValue = ExtractCookieValue(loginResponse, "JWT");

            // Act - Revoke token
            var revokeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/revoke-token");
            revokeRequest.Headers.Add("Cookie", $"JWT={jwtTokenValue}; RefreshToken={refreshTokenValue}");

            var revokeResponse = await _httpClient.SendAsync(revokeRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

            // Verify token was revoked in database using NEW SCOPE
            using var scope = _fixture.ServerFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var revokedToken = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenValue);

            Assert.NotNull(revokedToken);
            Assert.NotNull(revokedToken.RevokedAt);

            // Try to use revoked token - should fail
            var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/refresh-token");
            refreshRequest.Headers.Add("Cookie", $"RefreshToken={refreshTokenValue}");
            var refreshResponse = await _httpClient.SendAsync(refreshRequest);

            Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        }

        [Fact]
        public async Task POST_RevokeToken_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Act
            var response = await _httpClient.PostAsync("/api/authentication/revoke-token", null);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task POST_RevokeToken_WithoutRefreshTokenCookie_ShouldReturnBadRequest()
        {
            // Arrange - Login to get JWT only
            string email = $"revoke-missing-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, TestConstants.TestUserPassword);

            var loginRequest = new LoginRequest(email, TestConstants.TestUserPassword);
            var loginContent = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");
            var loginResponse = await _httpClient.PostAsync("/api/authentication/login", loginContent);

            var jwtTokenValue = ExtractCookieValue(loginResponse, "JWT");

            // Act - Try to revoke without refresh token cookie
            var revokeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/revoke-token");
            revokeRequest.Headers.Add("Cookie", $"JWT={jwtTokenValue}");

            var revokeResponse = await _httpClient.SendAsync(revokeRequest);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, revokeResponse.StatusCode);
        }

        [Fact]
        public async Task POST_ChangePassword_ShouldRevokeAllRefreshTokens()
        {
            // Arrange - Login from two devices
            string email = $"change-pwd-revoke-{Guid.NewGuid()}@test.com";
            await AddUserToDb(email, TestConstants.TestUserPassword);

            var loginRequest = new LoginRequest(email, TestConstants.TestUserPassword);
            var loginContent = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");

            var device1Response = await _httpClient.PostAsync("/api/authentication/login", loginContent);
            var device1RefreshToken = ExtractCookieValue(device1Response, "RefreshToken");
            var device1JWT = ExtractCookieValue(device1Response, "JWT");

            var device2Response = await _httpClient.PostAsync("/api/authentication/login", loginContent);
            var device2RefreshToken = ExtractCookieValue(device2Response, "RefreshToken");

            // Act - Change password
            var changePasswordRequest = new ChangePasswordDto
            {
                CurrentPassword = TestConstants.TestUserPassword,
                NewPassword = "NewPassword123!"
            };
            var changePasswordContent = new StringContent(
                JsonConvert.SerializeObject(changePasswordRequest),
                Encoding.UTF8,
                "application/json"
            );

            var changePasswordHttpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/change-password")
            {
                Content = changePasswordContent
            };
            changePasswordHttpRequest.Headers.Add("Cookie", $"JWT={device1JWT}");

            var changePasswordResponse = await _httpClient.SendAsync(changePasswordHttpRequest);
            changePasswordResponse.EnsureSuccessStatusCode();

            // Assert - Both refresh tokens should be revoked using NEW SCOPE
            using var scope = _fixture.ServerFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var device1Token = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == device1RefreshToken);
            var device2Token = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == device2RefreshToken);

            Assert.NotNull(device1Token);
            Assert.NotNull(device2Token);
            Assert.NotNull(device1Token.RevokedAt);
            Assert.NotNull(device2Token.RevokedAt);
        }
    }

    // Helper record for error responses (if not already defined)
    public record ErrorResponse(string Code, string Message);
}