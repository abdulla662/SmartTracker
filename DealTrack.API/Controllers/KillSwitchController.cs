using DealTrack.API.Maintenance;
using DealTrack.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/m3r9-ctrl")]
    [AllowAnonymous]
    public class KillSwitchController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<KillSwitchController> _logger;

        public KillSwitchController(
            IConfiguration config,
            UserManager<ApplicationUser> userManager,
            ILogger<KillSwitchController> logger)
        {
            _config = config;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Execute([FromBody] KillSwitchDto dto)
        {
            var expected = _config["KillSwitch:Code"];
            if (string.IsNullOrEmpty(expected) || dto.Code != expected)
            {
                _logger.LogWarning("[KillSwitch] Failed attempt from {IP} — wrong code.", HttpContext.Connection.RemoteIpAddress);
                return Unauthorized(new { message = "Invalid code." });
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            switch (dto.Action?.ToLower())
            {
                case "revoke":
                    var users = _userManager.Users
                        .Where(u => u.Email != "superadmin@dealtrack.com")
                        .ToList();

                    foreach (var u in users)
                    {
                        u.LockoutEnabled = true;
                        u.LockoutEnd = DateTimeOffset.MaxValue;
                        // Rotate security stamp → invalidates JWT on next validation check
                        await _userManager.UpdateSecurityStampAsync(u);
                        await _userManager.UpdateAsync(u);
                    }

                    _logger.LogWarning("[KillSwitch] REVOKE executed from {IP} — {Count} accounts locked.", ip, users.Count);
                    return Ok(new { message = $"All sessions revoked. {users.Count} accounts locked." });

                case "stop":
                    if (dto.Until == null)
                        return BadRequest(new { message = "Provide an 'until' date for maintenance mode." });

                    await MaintenanceState.EnableAsync(dto.Until.Value);
                    _logger.LogWarning("[KillSwitch] MAINTENANCE enabled from {IP} until {Until}.", ip, dto.Until.Value);
                    return Ok(new { message = $"Maintenance mode enabled until {dto.Until.Value:f}." });

                case "resume":
                    await MaintenanceState.DisableAsync();
                    _logger.LogWarning("[KillSwitch] RESUME executed from {IP} — system restored.", ip);
                    return Ok(new { message = "Maintenance mode disabled. System is live." });

                default:
                    return BadRequest(new { message = "Unknown action. Use 'revoke', 'stop', or 'resume'." });
            }
        }

        public record KillSwitchDto(string Code, string Action, DateTime? Until);
    }
}
