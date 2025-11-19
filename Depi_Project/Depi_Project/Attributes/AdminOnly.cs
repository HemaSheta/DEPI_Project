using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class AdminOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        // Only this ONE email is admin
        if (!user.Identity.IsAuthenticated ||
            user.Identity.Name != "hemasheta061@gmail.com")
        {
            context.Result = new ForbidResult();
        }
    }
}
