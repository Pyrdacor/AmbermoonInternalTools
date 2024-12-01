using Microsoft.AspNetCore.Mvc;

namespace AmbermoonServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController<T> : ControllerBase
	where T : BaseController<T>
{
    private protected ILogger<T> Logger { get; }

    private protected string Email => Request.Headers[Headers.UserKey].ToString().Split(':').FirstOrDefault(string.Empty);

    private protected BaseController(ILogger<T> logger)
	{
		Logger = logger;
    }
}
