using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SajhaSikshya.Filters;

/// <summary>
/// Short-circuits an action with a 400 response when DataAnnotations validation fails,
/// for API-style/AJAX endpoints where returning a View isn't appropriate. Traditional
/// form-posting actions should keep their own explicit `if (!ModelState.IsValid)` check
/// so they can re-render the form; this filter is for JSON endpoints only.
/// </summary>
public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(context.ModelState);
        }
    }
}
