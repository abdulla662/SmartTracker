using DealTrack.Application.DTOs.Targets;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TargetController : ControllerBase
    {
        private readonly ITargetService _targets;

        public TargetController(ITargetService targets)
        {
            _targets = targets;
        }

        /// <summary>Set or update a target (upsert by user+type+month+year).</summary>
        [HttpPost]
        public async Task<IActionResult> Set([FromBody] CreateTargetDto dto, CancellationToken ct)
        {
            var result = await _targets.SetTargetAsync(dto, ct);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Get current user's own targets.</summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMy([FromQuery] int? month, [FromQuery] int? year, CancellationToken ct)
        {
            var result = await _targets.GetMyTargetsAsync(month, year, ct);
            return Ok(result);
        }

        /// <summary>Get all team targets (Admin: whole tenant; TeamLead: their members + self).</summary>
        [HttpGet("team")]
        public async Task<IActionResult> GetTeam([FromQuery] int? month, [FromQuery] int? year, CancellationToken ct)
        {
            var result = await _targets.GetTeamTargetsAsync(month, year, ct);
            return result.Success ? Ok(result) : StatusCode(403, result);
        }

        /// <summary>Delete a target.</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var result = await _targets.DeleteTargetAsync(id, ct);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
