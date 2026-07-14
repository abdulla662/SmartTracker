using DealTrack.Application.Common;
using DealTrack.Application.DTOs.JoinRequest;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class JoinRequestsController : ControllerBase
    {
        private readonly IJoinRequestService _joinRequestService;

        public JoinRequestsController(IJoinRequestService joinRequestService)
        {
            _joinRequestService = joinRequestService;
        }

        [HttpGet]
        public async Task<ApiResponseT<List<JoinRequestDto>>> GetPending(CancellationToken ct)
            => await _joinRequestService.GetPendingAsync(ct);

        [HttpPost("{userId}/accept")]
        public async Task<ApiResponse> Accept(string userId, CancellationToken ct)
            => await _joinRequestService.AcceptAsync(userId, ct);

        [HttpPost("{userId}/reject")]
        public async Task<ApiResponse> Reject(string userId, CancellationToken ct)
            => await _joinRequestService.RejectAsync(userId, ct);
    }
}
