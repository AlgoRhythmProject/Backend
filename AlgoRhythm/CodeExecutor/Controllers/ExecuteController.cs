using CodeExecutor.Services;
using CodeExecutor.DTO.Requests;
using CodeExecutor.DTO;
using CodeExecutor.Helpers;

using Microsoft.AspNetCore.Mvc;

namespace CodeExecutor.Controllers
{
    [ApiController]
    [Route("code-executor/[controller]")]
    public class ExecuteController : ControllerBase
    {
        private readonly CSharpExecutionService _executionService;
        private readonly CSharpCompileService _compileService;
        public ExecuteController(
            CSharpExecutionService executionService, CSharpCompileService compileService)
        {
            _executionService = executionService;
            _compileService = compileService;
        }

        [HttpPost]
        public async Task<ExecutionResult> Execute([FromBody] ExecuteCodeRequest request)
        {
            //var result = await _executionService.ExecuteCodeOptimizedAsync(request.Code);

            return _compileService.Run(
                request.ReturnType, 
                request.Code,
                request.Args
            );
        }
    }    
}
