using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;

namespace AlgoRhythm.Clients
{
    public class CodeExecutorClient 
    {
        private readonly HttpClient _client;

        public CodeExecutorClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<List<TestResultDto>> ExecuteAsync(List<ExecuteCodeRequest> req)
        {
            HttpResponseMessage response = await _client.PostAsJsonAsync("/code-executor/Execute", req);

            return (await response.Content.ReadFromJsonAsync<List<TestResultDto>>()) ??
                [
                    new()
                    {
                        Errors = [ new("Couldn't connect") ],
                        Passed = false
                    }
                ];
        }
    }
}
