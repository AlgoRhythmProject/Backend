using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlgoRhythm.Clients
{
    public class CodeExecutorClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<CodeExecutorClient> _logger;

        public CodeExecutorClient(HttpClient client, ILogger<CodeExecutorClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<List<TestResultDto>?> ExecuteAsync(List<ExecuteCodeRequest> req)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };

            // ======== LOG REQUEST ========
            try
            {
                string json = JsonSerializer.Serialize(req, jsonOptions);
                _logger.LogInformation("Sending request to JUDGE:\n" + json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize ExecuteCodeRequest list for logging");
            }
            // ==============================

            try
            {
                HttpResponseMessage response =
                    await _client.PostAsJsonAsync("http://executor:8080/code-executor/Execute", req);

                _logger.LogInformation("Judge responded with HTTP {StatusCode}", response.StatusCode);

                response.EnsureSuccessStatusCode();

                var result =
                    await response.Content.ReadFromJsonAsync<List<TestResultDto>>();

                // ======== LOG RESPONSE ========
                try
                {
                    string jsonResult = JsonSerializer.Serialize(result, jsonOptions);
                    _logger.LogInformation("Judge response JSON:\n{Json}", jsonResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to serialize TestResultDto list for logging");
                }
                // ===============================

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when communicating with judge service");

                return
                [
                    new TestResultDto
                    {
                        Errors = [ new("Couldn't connect with external service") ]
                    }
                ];
            }
        }
    }
}
