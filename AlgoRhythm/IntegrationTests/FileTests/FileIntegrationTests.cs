using AlgoRhythm.Services.Blob.Interfaces;
using IntegrationTests.IntegrationTestSetup;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using Azure;

namespace IntegrationTests.BlobTests
{
    public class FileControllerIntegrationTests : IClassFixture<AlgoRhythmTestFixture>
    {
        private readonly AlgoRhythmTestFixture _fixture;
        private readonly HttpClient _httpClient;
        private readonly Mock<IFileStorageService> _mockStorageService;

        private readonly string _controllerRoute = "/api/file";

        public FileControllerIntegrationTests(AlgoRhythmTestFixture fixture)
        {
            _fixture = fixture;
            _mockStorageService = new Mock<IFileStorageService>();

            _httpClient = fixture.ServerFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IFileStorageService));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddSingleton(_mockStorageService.Object);
                });
            }).CreateClient();
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
            var fileName = $"test-{Guid.NewGuid()}.txt";
            var expectedUrl = $"https://test.blob.core.windows.net/files/{fileName}";

            _mockStorageService
                .Setup(s => s.UploadFileAsync(
                    It.Is<string>(f => f == fileName),
                    It.IsAny<Stream>(),
                    It.Is<string>(ct => ct == "text/plain")))
                .ReturnsAsync(expectedUrl);

            var content = CreateFileUploadContent(fileName, "Test file content");
            var response = await _httpClient.PostAsync(_controllerRoute, content);

            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            Assert.Contains("url", responseBody.ToLower());
            Assert.Contains("file saved", responseBody.ToLower());
        }

        [Fact]
        public async Task POST_UploadFile_EmptyFile_Returns400_BadRequest()
        {
            var multipartContent = new MultipartFormDataContent();
            var emptyFileContent = new ByteArrayContent(Array.Empty<byte>());
            emptyFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            multipartContent.Add(emptyFileContent, "file", "empty.txt");

            var response = await _httpClient.PostAsync(_controllerRoute, multipartContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("Empty file", responseBody);
        }

        [Fact]
        public async Task POST_UploadFile_NoFile_Returns400_BadRequest()
        {
            var multipartContent = new MultipartFormDataContent();

            var response = await _httpClient.PostAsync(_controllerRoute, multipartContent);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task POST_UploadFile_DuplicateFile_Returns409_Conflict()
        {
            var fileName = $"duplicate-{Guid.NewGuid()}.txt";

            _mockStorageService
                .Setup(s => s.UploadFileAsync(fileName, It.IsAny<Stream>(), It.IsAny<string>()))
                .ThrowsAsync(new RequestFailedException(409, "Blob already exists"));

            var content = CreateFileUploadContent(fileName, "First upload");
            var response = await _httpClient.PostAsync(_controllerRoute, content);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("already exists", responseBody);
        }

        [Fact]
        public async Task POST_UploadFile_UnexpectedException_Returns500_InternalServerError()
        {
            var fileName = $"error-{Guid.NewGuid()}.txt";

            _mockStorageService
                .Setup(s => s.UploadFileAsync(fileName, It.IsAny<Stream>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var content = CreateFileUploadContent(fileName, "Content");
            var response = await _httpClient.PostAsync(_controllerRoute, content);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("Unexpected error", responseBody);
        }

        [Fact]
        public async Task POST_UploadFile_DifferentContentTypes_Returns200()
        {
            var textFileName = $"text-{Guid.NewGuid()}.txt";
            var jsonFileName = $"json-{Guid.NewGuid()}.json";

            _mockStorageService
                .Setup(s => s.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync((string name, Stream stream, string type) => $"https://test.blob/{name}");

            var textContent = CreateFileUploadContent(textFileName, "Plain text", "text/plain");
            var jsonContent = CreateFileUploadContent(jsonFileName, "{\"key\":\"value\"}", "application/json");

            var textResponse = await _httpClient.PostAsync(_controllerRoute, textContent);
            var jsonResponse = await _httpClient.PostAsync(_controllerRoute, jsonContent);

            textResponse.EnsureSuccessStatusCode();
            jsonResponse.EnsureSuccessStatusCode();
        }

        // --- GET File ---

        [Fact]
        public async Task GET_GetFile_ExistingFile_Returns200WithContent()
        {
            var fileName = $"retrieve-{Guid.NewGuid()}.txt";
            var fileContent = "Content to retrieve";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

            _mockStorageService
                .Setup(s => s.GetFileAsync(fileName))
                .ReturnsAsync((stream, "text/plain"));

            var response = await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName={fileName}");

            response.EnsureSuccessStatusCode();
            var retrievedContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(fileContent, retrievedContent);
        }

        [Fact]
        public async Task GET_GetFile_NonExistingFile_Returns404_NotFound()
        {
            var fileName = $"nonexistent-{Guid.NewGuid()}.txt";

            _mockStorageService
                .Setup(s => s.GetFileAsync(fileName))
                .ThrowsAsync(new FileNotFoundException("File not found"));

            var response = await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName={fileName}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GET_GetFile_EmptyFileName_Returns400_BadRequest()
        {
            var response = await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName=");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }


        [Fact]
        public async Task GET_GetFile_FileNameWithQuotes_RemovesQuotes()
        {
            var fileName = $"quoted-{Guid.NewGuid()}.txt";
            var fileContent = "Content with quotes";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

            _mockStorageService
                .Setup(s => s.GetFileAsync(fileName))
                .ReturnsAsync((stream, "text/plain"));

            var response = await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName=\"{fileName}\"");

            response.EnsureSuccessStatusCode();
            var retrievedContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(fileContent, retrievedContent);
        }

        [Fact]
        public async Task GET_GetFile_UnexpectedException_Returns500_InternalServerError()
        {
            var fileName = $"error-{Guid.NewGuid()}.txt";

            _mockStorageService
                .Setup(s => s.GetFileAsync(fileName))
                .ThrowsAsync(new Exception("Storage error"));

            var response = await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName={fileName}");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }



        [Fact]
        public async Task GET_VideoInfo_EmptyFileName_Returns400_BadRequest()
        {
            var response = await _httpClient.GetAsync($"{_controllerRoute}/video_info?fileName=");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }



        // --- DELETE File ---

        [Fact]
        public async Task DELETE_DeleteFile_ExistingFile_Returns200True()
        {
            var fileName = $"delete-{Guid.NewGuid()}.txt";

            _mockStorageService
                .Setup(s => s.DeleteFileAsync(fileName))
                .ReturnsAsync(true);

            var response = await _httpClient.DeleteAsync($"{_controllerRoute}/{fileName}");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal("true", result.ToLower());
        }

        [Fact]
        public async Task DELETE_DeleteFile_NonExistingFile_Returns200False()
        {
            var fileName = $"nonexistent-{Guid.NewGuid()}.txt";

            _mockStorageService
                .Setup(s => s.DeleteFileAsync(fileName))
                .ReturnsAsync(false);

            var response = await _httpClient.DeleteAsync($"{_controllerRoute}/{fileName}");

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            Assert.Equal("false", result.ToLower());
        }

        [Fact]
        public async Task DELETE_DeleteFile_EmptyFileName_Returns400_BadRequest()
        {
            var response = await _httpClient.DeleteAsync($"{_controllerRoute}/");

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        // --- Content Type Tests ---

        [Fact]
        public async Task GET_GetFile_PreservesContentType()
        {
            var fileName = $"contenttype-{Guid.NewGuid()}.json";
            var jsonContent = "{\"test\": \"data\"}";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));

            _mockStorageService
                .Setup(s => s.GetFileAsync(fileName))
                .ReturnsAsync((stream, "application/json"));

            var response = await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName={fileName}");

            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        // --- Integration Test: Mock Verification ---

        [Fact]
        public async Task UploadFile_CallsStorageServiceWithCorrectParameters()
        {
            var fileName = "test.txt";
            var content = "test content";

            _mockStorageService
                .Setup(s => s.UploadFileAsync(fileName, It.IsAny<Stream>(), "text/plain"))
                .ReturnsAsync("https://test.blob/test.txt");

            var uploadContent = CreateFileUploadContent(fileName, content);
            await _httpClient.PostAsync(_controllerRoute, uploadContent);

            _mockStorageService.Verify(
                s => s.UploadFileAsync(fileName, It.IsAny<Stream>(), "text/plain"),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFile_CallsStorageServiceWithCorrectFileName()
        {
            var fileName = "verify.txt";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content"));

            _mockStorageService
                .Setup(s => s.GetFileAsync(fileName))
                .ReturnsAsync((stream, "text/plain"));

            await _httpClient.GetAsync($"{_controllerRoute}/get_file?fileName={fileName}");

            _mockStorageService.Verify(
                s => s.GetFileAsync(fileName),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteFile_CallsStorageServiceWithCorrectFileName()
        {
            var fileName = "delete.txt";

            _mockStorageService
                .Setup(s => s.DeleteFileAsync(fileName))
                .ReturnsAsync(true);

            await _httpClient.DeleteAsync($"{_controllerRoute}/{fileName}");

            _mockStorageService.Verify(
                s => s.DeleteFileAsync(fileName),
                Times.Once
            );
        }
    }
}