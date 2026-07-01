using DealTrack.Application.Common;
using DealTrack.Application.DTOs.TenantInvite;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvitesController : ControllerBase
    {
        private readonly IInviteService _inviteService;

        public InvitesController(IInviteService inviteService)
        {
            _inviteService = inviteService;
        }

        [HttpPost]
        public async Task<ApiResponseT<GetInviteDto>> CreateInvite(
            [FromBody] CreateInviteDto dto,
            CancellationToken ct)
            => await _inviteService.CreateInviteAsync(dto, ct);

        [HttpPost("accept")]
        [AllowAnonymous]
        public async Task<ApiResponseT<AcceptInviteDto>> AcceptInvite(
            [FromBody] AcceptInviteDto dto,
            CancellationToken ct)
            => await _inviteService.AcceptInviteAsync(dto, ct);

        [HttpGet]
        public async Task<ApiResponseT<List<GetInviteDto>>> GetAllInvites(CancellationToken ct)
            => await _inviteService.GetAllInvites(ct);
    }
}
