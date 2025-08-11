
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Shared;
namespace TiklabChallenge.API.Middleware
{
    public class AssignStudentRoleFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var result = await next(context);

            var http = context.HttpContext;
            if (http.Request.Method == "POST" &&
                http.Request.Path.Value?.EndsWith("/register", StringComparison.OrdinalIgnoreCase) == true &&
                http.Response.StatusCode is >= 200 and < 300)
            {
                var req = context.Arguments.OfType<RegisterRequest>().FirstOrDefault();
                if (req is not null)
                {
                    var um = http.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                    var user = await um.FindByEmailAsync(req.Email);
                    if (user is not null && !await um.IsInRoleAsync(user, AppRoles.Student))
                    {
                        await um.AddToRoleAsync(user, AppRoles.Student);
                    }
                }
            }
            return result;
        }
    }
}
