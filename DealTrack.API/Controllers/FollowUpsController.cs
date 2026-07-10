using DealTrack.Application.Common;
using DealTrack.Application.DTOs.FollowUps;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FollowUpsController : ControllerBase
    {
        private readonly IFollowUpService _followUpService;

        public FollowUpsController(IFollowUpService followUpService)
        {
            _followUpService = followUpService;
        }

        [HttpGet]
        public async Task<ApiResponseT<PagedResult<FollowUpResponseDto>>> GetAll(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            return await _followUpService.GetAllFollowUpsAsync(page, pageSize, ct);
        }

        [HttpGet("client/{clientId:guid}")]
        public async Task<ApiResponseT<PagedResult<FollowUpResponseDto>>> GetForClient(
            Guid clientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            return await _followUpService.GetFollowUpsForClientAsync(clientId, page, pageSize, ct);
        }

        [HttpPost]
        public async Task<ApiResponseT<FollowUpResponseDto>> Create(
            [FromBody] CreateFollowUpDto dto, CancellationToken ct)
        {
            return await _followUpService.CreateFollowUpAsync(dto, ct);
        }

        [HttpPatch("{id:guid}/done")]
        public async Task<ApiResponse> MarkDone(Guid id, CancellationToken ct)
        {
            return await _followUpService.MarkDoneAsync(id, ct);
        }

        [HttpPatch("{id:guid}/missed")]
        public async Task<ApiResponse> MarkMissed(Guid id, CancellationToken ct)
        {
            return await _followUpService.MarkMissedAsync(id, ct);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ApiResponse> Delete(Guid id, CancellationToken ct)
        {
            return await _followUpService.DeleteFollowUpAsync(id, ct);
        }

        [HttpPut("{id:guid}")]
        public async Task<ApiResponse> UpdateFollowUp(Guid id, [FromBody] UpdateFollowUpDto dto, CancellationToken ct)
        {
            return await _followUpService.UpdateFollowUpAsync(id, dto, ct);
        }

    }
}
