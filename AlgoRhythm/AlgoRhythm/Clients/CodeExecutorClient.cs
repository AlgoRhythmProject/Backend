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

        public async Task<List<TestResultDto>?> ExecuteAsync(List<ExecuteCodeRequest> req)
        {
            try
            {
                HttpResponseMessage response = await _client.PostAsJsonAsync("/code-executor/Execute", req);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<TestResultDto>>();
                    
            }
            catch (Exception) 
            {
                return
                [
                    new()
                    {
                        Errors = [new("Couldn't connect with external service")],
                    }
                ];
                
            }
        }
    }
}
