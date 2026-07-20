using DealTrack.Application.Common;
using DealTrack.Application.DTOs.HR;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HRController : ControllerBase
    {
        private readonly IHRService _hrService;

        public HRController(IHRService hrService)
        {
            _hrService = hrService;
        }

        [HttpPost("actions")]
        public async Task<ApiResponse> CreateAction(CreateHRActionDto dto, CancellationToken ct)
            => await _hrService.CreateActionRequestAsync(dto, ct);

        [HttpPut("actions/{id:guid}/review")]
        public async Task<ApiResponse> ReviewAction(Guid id, ReviewHRActionDto dto, CancellationToken ct)
            => await _hrService.ReviewActionRequestAsync(id, dto, ct);

        [HttpGet("actions")]
        public async Task<ApiResponseT<List<HRActionResponseDto>>> GetActions(CancellationToken ct)
            => await _hrService.GetActionRequestsAsync(ct);

        [HttpDelete("actions/{id:guid}")]
        [Authorize(Roles = "HR")]
        public async Task<ApiResponse> DeleteAction(Guid id, CancellationToken ct)
            => await _hrService.DeleteActionAsync(id, ct);
    }
}
