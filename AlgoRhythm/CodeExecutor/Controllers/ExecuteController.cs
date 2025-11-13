using Microsoft.AspNetCore.Mvc;

namespace CodeExecutor.Controllers
{
    [ApiController]
    [Route("code-executor/[controller]")]
    public class ExecuteController : ControllerBase
    {
        [HttpPost]
        public async Task Execute()
        {

        }
    }
}
