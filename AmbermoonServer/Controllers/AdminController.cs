using AmbermoonServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmbermoonServer.Controllers
{
    [Authorize(Policy = Policies.AdminOnly)]
    public class AdminController(ILogger<AdminController> logger, AdminService adminService) : BaseController<AdminController>(logger)
    {
        [HttpPost("create/gameVersion")]
        public async Task<IActionResult> CreateGameVersion([FromBody] string gameVersion)
        {
            await adminService.CreateGameVersion(gameVersion);

            return Created();
        }

        [HttpPost("create/language")]
        public async Task<IActionResult> CreateLanguage([FromBody] string language)
        {
            await adminService.CreateLanguage(language);

            return Created();
        }

        [HttpPost("create/source")]
        public async Task<IActionResult> CreateSource([FromBody] string source)
        {
            await adminService.CreateSource(source);

            return Created();
        }

        [HttpPost("ban")]
        public async Task<IActionResult> BanUser([FromBody] string email)
        {
            await adminService.BanUser(email.ToLower());

            return Ok();
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteUser([FromBody] string email)
        {
            await adminService.DeleteUser(email.ToLower());

            return Ok();
        }
    }
}
