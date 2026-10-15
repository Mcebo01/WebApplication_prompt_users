using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace RaceDay_API.Security
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public string? Roles { get; set; }   // e.g. "Organiser" or "Participant"

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;
            var session = httpContext.Session;

            // 1. Check if user is logged in
            var userId = session.GetInt32("UserId");
            var role = session.GetString("Role");

            if (userId == null || string.IsNullOrEmpty(role))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    message = "You must be logged in to access this resource."
                });
                return;
            }

            // 2. If specific roles are required, check them
            if (!string.IsNullOrEmpty(Roles))
            {
                var allowedRoles = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (!allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    context.Result = new ObjectResult(new
                    {
                        message = $"Access denied. Required role(s): {Roles}. Your role: {role}"
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }
            }

            // User is authenticated (and has the correct role if specified)
        }
    }
}