using AmbermoonServer.DTO;
using AmbermoonServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmbermoonServer.Controllers;

public class SavegameController
(
	ILogger<SavegameController> logger,
	SavegameService savegameService
) : BaseController<SavegameController>(logger)
{
    [HttpGet()]
    [Authorize]
    public async Task<ActionResult<IEnumerable<SavegameDto>>> GetAll()
	{
        return await savegameService.GetSavegames(Email);
    }

    [HttpPost()]
    [Authorize]
    public async Task<IActionResult> Store([FromBody] SavegamesDto savegames)
    {
        if (savegames.Savegames.Length >= 1000)
        {
            // suspicious amount of savegames
            return BadRequest();
        }

        await savegameService.StoreSavegames(Email, savegames.Savegames);

        return Created();
    }
}
