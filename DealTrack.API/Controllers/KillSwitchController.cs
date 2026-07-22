using DealTrack.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/ks")]
    [AllowAnonymous]
    public class KillSwitchController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHostApplicationLifetime _lifetime;

        public KillSwitchController(
            IConfiguration config,
            UserManager<ApplicationUser> userManager,
            IHostApplicationLifetime lifetime)
        {
            _config = config;
            _userManager = userManager;
            _lifetime = lifetime;
        }

        [HttpPost]
        public async Task<IActionResult> Execute([FromBody] KillSwitchDto dto)
        {
            var expected = _config["KillSwitch:Code"];
            if (string.IsNullOrEmpty(expected) || dto.Code != expected)
                return Unauthorized(new { message = "Invalid code." });

            switch (dto.Action?.ToLower())
            {
                case "revoke":
                    // Lock all non-SuperAdmin users to invalidate all active sessions
                    var users = _userManager.Users
                        .Where(u => u.Email != "superadmin@dealtrack.com")
                        .ToList();
                    foreach (var u in users)
                    {
                        u.LockoutEnabled = true;
                        u.LockoutEnd = DateTimeOffset.MaxValue;
                        await _userManager.UpdateAsync(u);
                    }
                    return Ok(new { message = $"All sessions revoked. {users.Count} accounts locked." });

                case "stop":
                    // Gracefully stop the application (Docker will restart unless --restart=no)
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(500);
                        _lifetime.StopApplication();
                    });
                    return Ok(new { message = "Application shutdown initiated." });

                default:
                    return BadRequest(new { message = "Unknown action. Use 'revoke' or 'stop'." });
            }
        }

        public record KillSwitchDto(string Code, string Action);
    }
}
