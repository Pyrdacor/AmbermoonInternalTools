using AmbermoonServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AmbermoonServer.Controllers;

public class UserController(ILogger<UserController> logger, UserService userService) : BaseController<UserController>(logger)
{
    [HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] string email)
	{
		await userService.RegisterUser(email.ToLower());

		// Note: If the user exists already, we will still just return OK.
		// This way you can't check for registered users.

		return Ok();
	}

    [HttpGet("verify")]
    public async Task<IActionResult> Verify([FromQuery] string email, [FromQuery] string token)
    {
		if (!Guid.TryParse(token, out var guid))
			guid = Guid.Empty;

        return Content(await userService.VerifyUser(email, guid), "text/html");
    }

    [HttpPost("request-code")]
    public async Task<IActionResult> RequestCode([FromQuery] string email)
    {
        await userService.RequestCode(email.ToLower());

        return Ok();
    }

    [HttpGet("code")]
    public async Task<IActionResult> CodeRequest([FromQuery] string email, [FromQuery] string token)
    {
        if (!Guid.TryParse(token, out var guid))
            guid = Guid.Empty;

        return Content(await userService.CodeRequest(email, guid), "text/html");
    }
}
