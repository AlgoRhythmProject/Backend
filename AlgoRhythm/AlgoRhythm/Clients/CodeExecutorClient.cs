using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using AlgoRhythm.Shared.Models.CodeExecution.Responses;

namespace AlgoRhythm.Clients
{
    public class CodeExecutorClient 
    {
        private readonly HttpClient _client;

        public CodeExecutorClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<ExecuteCodeResponse> ExecuteAsync(ExecuteCodeRequest req)
        {
            HttpResponseMessage response = await _client.PostAsJsonAsync("/code-executor/Execute", req);

            return (await response.Content.ReadFromJsonAsync<ExecuteCodeResponse?>())
                ?? new ExecuteCodeResponse()
                {
                    Errors = [ new("Couldn't connect")],
                    Success = false,
                };
        }
    }
}
