using CodeExecutor.Services;
using CodeExecutor.DTO.Requests;
using CodeExecutor.DTO;

using Microsoft.AspNetCore.Mvc;

namespace CodeExecutor.Controllers
{
    [ApiController]
    [Route("code-executor/[controller]")]
    public class ExecuteController : ControllerBase
    {
        private readonly CSharpExecutionService _executionService;
        public ExecuteController(
            CSharpExecutionService executionService)
        {
            _executionService = executionService;
        }

        [HttpPost]
        public async Task<ExecutionResult> Execute([FromBody] ExecuteCodeRequest request)
        {
            var result = await _executionService.ExecuteCodeOptimizedAsync(request.Code);
            return result;
        }
    }    
}
