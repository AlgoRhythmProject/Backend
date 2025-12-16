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
        private readonly ILogger _logger;

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

            try
            {   
                HttpResponseMessage response = await _client.PostAsJsonAsync("/code-executor/Execute", req);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<List<TestResultDto>>();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception thrown {0}", ex.Message);

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
