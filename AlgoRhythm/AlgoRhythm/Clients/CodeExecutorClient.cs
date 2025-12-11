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

        public CodeExecutorClient(HttpClient client)
        {
            _client = client;
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
                HttpResponseMessage response = await _client.PostAsJsonAsync("http://executor:8080/code-executor/Execute", req);
                response.EnsureSuccessStatusCode();

                var result =
                    await response.Content.ReadFromJsonAsync<List<TestResultDto>>();

                try
                {
                    string jsonResult = JsonSerializer.Serialize(result, jsonOptions);
                }
                catch (Exception ex)
                {
                }
                return result;
            }
            catch (Exception ex)
            {

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
