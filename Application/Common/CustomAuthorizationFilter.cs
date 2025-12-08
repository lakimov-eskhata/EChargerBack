using Microsoft.AspNetCore.Http;

namespace Application.Common;

public class CustomAuthorizationFilter: IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        var user = httpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
            return Results.Unauthorized();

        if (!user.HasClaim("role", "Admin"))
            return Results.Forbid();

        return await next(context);
    }
}