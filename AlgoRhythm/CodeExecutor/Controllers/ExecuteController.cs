using AlgoRhythm.Shared.Models.CodeExecution.Requests;
using AlgoRhythm.Shared.Models.CodeExecution.Responses;
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

        [HttpPost]
        public async Task<ExecuteCodeResponse> Execute([FromBody] ExecuteCodeRequest request)
        {            
            return _executeService.Run(
                request.Timeout,
                request.Args,
                request.ExecutionClass,
                request.ExecutionMethod,
                request.Code
            );
        }
    }
}