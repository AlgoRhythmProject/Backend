using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AlgoRhythm.Attributes
{
    public class DevelopmentOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var env = context.HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>();

            if (!env.IsDevelopment())
            {
                context.Result = new NotFoundResult();
            }
        }
    }
}
