using AmbermoonServer.Controllers;
using AmbermoonServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AmbermoonServer.Middleware;

public class CustomAuthentificationHandler(
    UserService userService,
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authKey = Request.Headers[Headers.UserKey].ToString();

        if (string.IsNullOrEmpty(authKey))
        {
            return await Task.FromResult(AuthenticateResult.Fail("Missing auth header"));
        }

        var keyParts = authKey.Split(':');
        var email = keyParts.FirstOrDefault();
        var token = keyParts.LastOrDefault();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            return await Task.FromResult(AuthenticateResult.Fail("Invalid auth header"));
        }

        var allowedToRequest = await userService.IsAllowedToRequest(email, token);

        if (!allowedToRequest)
        {
            return await Task.FromResult(AuthenticateResult.Fail("Invalid email or token"));
        }

        // Create authenticated user
        var claims = new[] { new Claim(ClaimTypes.Name, email) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return await Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
