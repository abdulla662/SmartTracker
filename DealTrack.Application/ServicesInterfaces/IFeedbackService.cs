using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Feedback;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IFeedbackService
    {
        Task<ApiResponseT<List<FeedbackMemberDto>>> GetTeamFeedbackAsync(int? month, int? year, CancellationToken ct = default);
        Task<ApiResponseT<FeedbackMemberDto>> GetMyProgressAsync(int? month, int? year, CancellationToken ct = default);
    }
}
