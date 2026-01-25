using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;

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

        public async Task<List<TestResultDto>> ExecuteAsync(List<ExecuteCodeRequest> req)
        {
            try
            {
                var response = await _client.PostAsJsonAsync("/code-executor/Execute", req);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<TestResultDto>>()
                       ?? CreateErrorResult("Empty response", req.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error while attempting to connect code executors.");

                return CreateErrorResult($"Execution error: {ex.Message}", req.Count);
            }
        }

        private static List<TestResultDto> CreateErrorResult(string message, int count)
        {
            return
            [
                ..Enumerable.Range(0, count)
                    .Select(_ => new TestResultDto
                    {
                        Errors = [ new(message) ],
                        Passed = false
                    })
            ];
        }
    }
}