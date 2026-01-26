using AlgoRhythm.Services.Blob.Interfaces;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using Azure;
using AlgoRhythm.Shared.Models.Users;
using Microsoft.AspNetCore.Identity;
using AlgoRhythm.Services.Users.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationTests.BlobTests
{
    public class FileControllerIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
    {
        private readonly AlgoRhythmTestFixture _fixture;
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _httpClient;
        private readonly Mock<IFileStorageService> _mockStorageService;
        private readonly IServiceScope _scope;
        private readonly UserManager<User> _userManager;
        private readonly IAuthService _authService;

        private readonly string _controllerRoute = "/api/file";

        public FileControllerIntegrationTests(AlgoRhythmTestFixture fixture)
        {
            _fixture = fixture;
            _mockStorageService = new Mock<IFileStorageService>();

            // Create factory with mocked IFileStorageService
            _factory = fixture.ServerFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IFileStorageService));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddSingleton(_mockStorageService.Object);
                });
            });

            _httpClient = _factory.CreateClient();
            
            // IMPORTANT: Use scope from the MOCKED factory, not the original fixture
            _scope = _factory.Services.CreateScope();
            _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            _authService = _scope.ServiceProvider.GetRequiredService<IAuthService>();
        }

        private async Task<string> SetupAuthenticatedAdminUser()
        {
            var userEmail = $"admin-{Guid.NewGuid()}@example.com";

            var user = new User
            {
                UserName = userEmail,
                Email = userEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, TestConstants.TestUserPassword);
            if (!createResult.Succeeded)
            {
                throw new Exception($"Failed to create admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            await _userManager.AddToRoleAsync(user, "Admin");

            var loginRequest = new AlgoRhythm.Shared.Dtos.Users.LoginRequest(userEmail, TestConstants.TestUserPassword);
            var authResponse = await _authService.LoginAsync(loginRequest);

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authResponse.Token);

            return authResponse.Token;
        }

        private MultipartFormDataContent CreateFileUploadContent(string fileName, string content, string contentType = "text/plain")
        {
            var multipartContent = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(content));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            multipartContent.Add(fileContent, "file", fileName);
            return multipartContent;
        }

        // --- POST Upload File ---

        [Fact]
        public async Task POST_UploadFile_ValidFile_Returns200WithUrl()
        {
            // Arrange
            await SetupAuthenticatedAdminUser();
            var fileName = $"test-{Guid.NewGuid()}.txt";
            var expectedUrl = $"https://test.blob.core.windows.net/files/{fileName}";

            _mockStorageService
                .Setup(s => s.UploadFileAsync(
                    It.Is<string>(f => f == fileName),
                    It.IsAny<Stream>(),
                    It.Is<string>(ct => ct == "text/plain")))
                .ReturnsAsync(expectedUrl);

            var content = CreateFileUploadContent(fileName, "Test file content");
            
            // Act
            var response = await _httpClient.PostAsync(_controllerRoute, content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            Assert.Contains("url", responseBody.ToLower());
            Assert.Contains("file saved", responseBody.ToLower());
        }

        [Fact]
        public async Task POST_UploadFile_EmptyFile_Returns400_BadRequest()
        {
            // Arrange
            await SetupAuthenticatedAdminUser();
            var multipartContent = new MultipartFormDataContent();
            var emptyFileContent = new ByteArrayContent(Array.Empty<byte>());
            emptyFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            multipartContent.Add(emptyFileContent, "file", "empty.txt");

            // Act
            var response = await _httpClient.PostAsync(_controllerRoute, multipartContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("empty file", responseBody.ToLower());
        }

        [Fact]
        public async Task POST_UploadFile_NoFile_Returns400_BadRequest()
        {
            // Arrange
            await SetupAuthenticatedAdminUser();
            var multipartContent = new MultipartFormDataContent();

            // Act
            var response = await _httpClient.PostAsync(_controllerRoute, multipartContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_UploadFile_DuplicateFile_Returns409_Conflict()
        {
            // Arrange
            await SetupAuthenticatedAdminUser();
            var fileName = $"duplicate-{Guid.NewGuid()}.txt";

            _mockStorageService
                .Setup(s => s.UploadFileAsync(fileName, It.IsAny<Stream>(), It.IsAny<string>()))
                .ThrowsAsync(new RequestFailedException(409, "Blob already exists"));

            var content = CreateFileUploadContent(fileName, "First upload");
            
            // Act
            var response = await _httpClient.PostAsync(_controllerRoute, content);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("already exists", responseBody.ToLower());
        }

        [Fact]
        public async Task POST_UploadFile_UnexpectedException_Returns500_InternalServerError()
        {
            // Arrange
            await SetupAuthenticatedAdminUser();
            var fileName = $"error-{Guid.NewGuid()}.txt";

            _mockStorageService
                .Setup(s => s.UploadFileAsync(fileName, It.IsAny<Stream>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var content = CreateFileUploadContent(fileName, "Content");
            
            // Act
            var response = await _httpClient.PostAsync(_controllerRoute, content);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("unexpected error", responseBody.ToLower());
        }

        [Fact]
        public async Task POST_UploadFile_DifferentContentTypes_Returns200()
        {
            // Arrange
            await SetupAuthenticatedAdminUser();
            var textFileName = $"text-{Guid.NewGuid()}.txt";
            var jsonFileName = $"json-{Guid.NewGuid()}.json";

            _mockStorageService
                .Setup(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync((string name, Stream stream, string type) => $"https://test.blob/{name}");

            var textContent = CreateFileUploadContent(textFileName, "Plain text", "text/plain");
            var jsonContent = CreateFileUploadContent(jsonFileName, "{\"key\":\"value\"}", "application/json");

            // Act
            var textResponse = await _httpClient.PostAsync(_controllerRoute, textContent);
            var jsonResponse = await _httpClient.PostAsync(_controllerRoute, jsonContent);

            // Assert
            textResponse.EnsureSuccessStatusCode();
            jsonResponse.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task POST_UploadFile_Unauthorized_Returns401()
        {
            // Arrange
            _httpClient.DefaultRequestHeaders.Authorization = null;
            var content = CreateFileUploadContent("test.txt", "content");

            // Act
            var response = await _httpClient.PostAsync(_controllerRoute, content);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // --- GET File ---

        [Fact]
        public async Task GET_GetFile_ExistingFile_Returns200WithContent()
        {
            // Arrange
            var fileName = $"retrieve-{Guid.NewGuid()}.txt";
            var fileContent = "Content to retrieve";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

            _mockStorageService
                .Setup(s => s.GetFileAsync(fileName))
                .ReturnsAsync((stream, "text/plain"));

            // Act
            var response = await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName={fileName}");

            // Assert
            response.EnsureSuccessStatusCode();
            var retrievedContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(fileContent, retrievedContent);
        }

        [Fact]
        public async Task GET_GetFile_NonExistingFile_Returns404_NotFound()
        {
            // Arrange
            var fileName = $"nonexistent-{Guid.NewGuid()}.txt";

            _mockStorageService
                .Setup(s => s.GetFileAsync(fileName))
                .ThrowsAsync(new FileNotFoundException("File not found"));

            // Act
            var response = await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName={fileName}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GET_GetFile_EmptyFileName_Returns400_BadRequest()
        {
            // Act
            var response = await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName=");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GET_GetFile_FileNameWithQuotes_RemovesQuotes()
        {
            // Arrange
            var fileName = $"quoted-{Guid.NewGuid()}.txt";
            var fileContent = "Content with quotes";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

            _mockStorageService
                .Setup(s => s.GetFileAsync(fileName))
                .ReturnsAsync((stream, "text/plain"));

            // Act
            var response = await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName=\"{fileName}\"");

            // Assert
            response.EnsureSuccessStatusCode();
            var retrievedContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(fileContent, retrievedContent);
        }

        [Fact]
        public async Task GET_GetFile_UnexpectedException_Returns500_InternalServerError()
        {
            // Arrange
            var fileName = $"error-{Guid.NewGuid()}.txt";

            _mockStorageService
                .Setup(s => s.GetFileAsync(fileName))
                .ThrowsAsync(new Exception("Storage error"));

            // Act
            var response = await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName={fileName}");

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task GET_VideoInfo_EmptyFileName_Returns400_BadRequest()
        {
            // Act
            var response = await _httpClient.GetAsync($"{_controllerRoute}/video_info?fileName=");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // --- DELETE File ---

        [Fact]
        public async Task DELETE_DeleteFile_ExistingFile_Returns200True()
        {
            // Arrange
            await SetupAuthenticatedAdminUser();
            var fileName = $"delete-{Guid.NewGuid()}.txt";

            _mockStorageService
                .Setup(s => s.DeleteFileAsync(fileName))
                .ReturnsAsync(true);

            // Act
            var response = await _httpClient.DeleteAsync($"{_controllerRoute}/{fileName}");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal("true", result.ToLower());
        }

        [Fact]
        public async Task DELETE_DeleteFile_NonExistingFile_Returns200False()
        {
            // Arrange
            await SetupAuthenticatedAdminUser();
            var fileName = $"nonexistent-{Guid.NewGuid()}.txt";

            _mockStorageService
                .Setup(s => s.DeleteFileAsync(fileName))
                .ReturnsAsync(false);

            // Act
            var response = await _httpClient.DeleteAsync($"{_controllerRoute}/{fileName}");

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal("false", result.ToLower());
        }

        [Fact]
        public async Task DELETE_DeleteFile_EmptyFileName_Returns405_MethodNotAllowed()
        {
            // Arrange
            await SetupAuthenticatedAdminUser();

            // Act
            var response = await _httpClient.DeleteAsync($"{_controllerRoute}/");

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        [Fact]
        public async Task DELETE_DeleteFile_Unauthorized_Returns401()
        {
            // Arrange
            _httpClient.DefaultRequestHeaders.Authorization = null;
            var fileName = "test.txt";

            // Act
            var response = await _httpClient.DeleteAsync($"{_controllerRoute}/{fileName}");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // --- Content Type Tests ---

        [Fact]
        public async Task GET_GetFile_PreservesContentType()
        {
            // Arrange
            var fileName = $"contenttype-{Guid.NewGuid()}.json";
            var jsonContent = "{\"test\": \"data\"}";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));

            _mockStorageService
                .Setup(s => s.GetFileAsync(fileName))
                .ReturnsAsync((stream, "application/json"));

            // Act
            var response = await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName={fileName}");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        // --- Integration Test: Mock Verification ---

        [Fact]
        public async Task UploadFile_CallsStorageServiceWithCorrectParameters()
        {
            // Arrange
            await SetupAuthenticatedAdminUser();
            var fileName = "test.txt";
            var content = "test content";

            _mockStorageService
                .Setup(s => s.UploadFileAsync(fileName, It.IsAny<Stream>(), "text/plain"))
                .ReturnsAsync("https://test.blob/test.txt");

            var uploadContent = CreateFileUploadContent(fileName, content);
            
            // Act
            await _httpClient.PostAsync(_controllerRoute, uploadContent);

            // Assert
            _mockStorageService.Verify(
                s => s.UploadFileAsync(fileName, It.IsAny<Stream>(), "text/plain"),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFile_CallsStorageServiceWithCorrectFileName()
        {
            // Arrange
            var fileName = "verify.txt";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content"));

            _mockStorageService
                .Setup(s => s.GetFileAsync(fileName))
                .ReturnsAsync((stream, "text/plain"));

            // Act
            await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName={fileName}");

            // Assert
            _mockStorageService.Verify(
                s => s.GetFileAsync(fileName),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteFile_CallsStorageServiceWithCorrectFileName()
        {
            // Arrange
            await SetupAuthenticatedAdminUser();
            var fileName = "delete.txt";

            _mockStorageService
                .Setup(s => s.DeleteFileAsync(fileName))
                .ReturnsAsync(true);

            // Act
            await _httpClient.DeleteAsync($"{_controllerRoute}/{fileName}");

            // Assert
            _mockStorageService.Verify(
                s => s.DeleteFileAsync(fileName),
                Times.Once
            );
        }
    }
}