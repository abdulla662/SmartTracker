using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Subscription;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface ISubscriptionService
    {
        Task<ApiResponseT<InitiatePaymentResponseDto>> InitiatePaymentAsync(InitiatePaymentDto dto, CancellationToken ct);
        Task<ApiResponse> HandleWebhookAsync(string rawBody, string hmacHeader, CancellationToken ct);
        Task<ApiResponse> HandleWebhookGetAsync(string merchantOrderId, string hmac, CancellationToken ct);
    }
}
