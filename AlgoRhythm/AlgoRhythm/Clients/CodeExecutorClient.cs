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
                // Dzięki AddPolicyHandler w Program.cs, to wywołanie 
                // automatycznie ponowi próbę, jeśli kontener zginie.
                var response = await _client.PostAsJsonAsync("/code-executor/Execute", req);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<TestResultDto>>()
                       ?? CreateErrorResult("Empty response");
            }
            catch (Exception ex)
            {
                // Jeśli po 3 próbach nadal jest błąd, znaczy że wszystkie repliki leżą 
                // lub kod studenta wiesza każdą z nich po kolei.
                _logger.LogError(ex, "Błąd krytyczny komunikacji z Executorami po seriach powtórzeń.");

                return CreateErrorResult($"Błąd wykonania: {ex.Message}");
            }
        }

        private List<TestResultDto> CreateErrorResult(string message)
        {
            return [ new TestResultDto {
                Errors = [ new(message) ],
                Passed = false
            } ];
        }
    }
}