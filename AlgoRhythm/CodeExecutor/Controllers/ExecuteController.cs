using AlgoRhythm.Shared.Dtos.Submissions;
using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using CodeExecutor.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeExecutor.Controllers
{
    [ApiController]
    [Route("code-executor/[controller]")]
    public class ExecuteController : ControllerBase
    {
        private readonly CSharpExecuteService _executeService;
        public ExecuteController(
            CSharpExecuteService executeService)
        {
            _executeService = executeService;
        }

        /// <summary>
        /// Endpoint for executing in parallel test cases for the same code 
        /// </summary>
        [HttpPost]
        public async Task<List<TestResultDto>> RunTests([FromBody] List<ExecuteCodeRequest> requests)
        {
            return await _executeService.RunTests(requests);
        }
    }
}