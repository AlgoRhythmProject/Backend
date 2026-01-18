using System.Net;
using System.Text.Json;

namespace IntegrationTests.IntegrationTestSetup;

/// <summary>
/// Mock HttpClientFactory for testing Google OAuth without real HTTP calls
/// </summary>
public class MockGoogleHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var handler = new MockGoogleHttpMessageHandler();
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://oauth2.googleapis.com")
        };
    }
}

/// <summary>
/// Mock HTTP handler that returns fake Google token info
/// </summary>
public class MockGoogleHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Check if this is a Google tokeninfo request
        if (request.RequestUri?.AbsolutePath.Contains("tokeninfo") == true)
        {
            var query = request.RequestUri.Query;
            var idToken = query.Split("id_token=").LastOrDefault()?.Split('&').FirstOrDefault();

            if (string.IsNullOrEmpty(idToken))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"invalid_token\"}")
                });
            }

            // Handle invalid token
            if (idToken == "invalid_token_12345")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"invalid_token\"}")
                });
            }

            // Handle empty token test
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"invalid_token\"}")
                });
            }

            // Extract email from mock token
            // Format: mock_google_token_{email} or mock_google_token_{prefix}
            string email;
            string firstName = "Test";
            string lastName = "User";

            if (idToken.Contains("@"))
            {
                // Token contains full email: mock_google_token_user@test.com
                email = idToken.Replace("mock_google_token_", "");
            }
            else
            {
                // Token contains prefix: mock_google_token_new_user
                var prefix = idToken.Replace("mock_google_token_", "").Replace("_user", "");
                email = $"{prefix}@test.com";
                
                // Set custom names based on prefix
                if (prefix == "new")
                {
                    firstName = "New";
                    lastName = "User";
                }
                else if (prefix == "streak")
                {
                    firstName = "Streak";
                    lastName = "User";
                }
            }

            // Return mock Google user info
            var mockResponse = new
            {
                aud = "test-google-client-id",
                sub = "mock_google_id_" + Guid.NewGuid().ToString("N")[..10],
                email = email,
                email_verified = "true",
                given_name = firstName,
                family_name = lastName,
                name = $"{firstName} {lastName}",
                picture = "https://example.com/photo.jpg"
            };

            var json = JsonSerializer.Serialize(mockResponse);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        // Default response for other requests
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"error\":\"not_found\"}")
        });
    }
}