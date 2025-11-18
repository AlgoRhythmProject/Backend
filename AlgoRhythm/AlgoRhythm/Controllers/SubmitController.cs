using AlgoRhythm.Clients;
using AlgoRhythm.DockerPool;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using AlgoRhythm.Shared.Models.CodeExecution.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AlgoRhythm.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubmitController : ControllerBase
    {
        //private readonly ContainerPool _pool;
        private readonly CodeExecutorClient _client;
        public SubmitController(CodeExecutorClient client)
        {
            _client = client;
        }

        [HttpPost]
        public async Task<ExecuteCodeResponse> Post([FromBody] ExecuteCodeRequest request)
        {
            return await _client.ExecuteAsync(request);
        }

    }
}
