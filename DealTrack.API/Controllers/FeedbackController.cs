using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Feedback;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpGet("team")]
        public async Task<ApiResponseT<List<FeedbackMemberDto>>> GetTeamFeedback(
            [FromQuery] int? month, [FromQuery] int? year, CancellationToken ct)
            => await _feedbackService.GetTeamFeedbackAsync(month, year, ct);

        [HttpGet("my-progress")]
        public async Task<ApiResponseT<FeedbackMemberDto>> GetMyProgress(
            [FromQuery] int? month, [FromQuery] int? year, CancellationToken ct)
            => await _feedbackService.GetMyProgressAsync(month, year, ct);
    }
}
