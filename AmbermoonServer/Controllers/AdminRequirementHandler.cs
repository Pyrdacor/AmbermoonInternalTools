using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AmbermoonServer.Controllers;

public class AdminRequirement() : IAuthorizationRequirement
{
}

public class AdminRequirementHandler : AuthorizationHandler<AdminRequirement>
{
    private const string AdminUserName = "ASPNETCORE_ADMIN_USER";
    internal readonly static string AdminUser = Environment.GetEnvironmentVariable(AdminUserName) ?? throw new KeyNotFoundException($"Missing {AdminUserName} environment variable");

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        if (context.User.Claims.Any(claim => claim.Subject?.IsAuthenticated == true && claim.Type == ClaimTypes.Name && claim.Value.Equals(AdminUser, StringComparison.CurrentCultureIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
