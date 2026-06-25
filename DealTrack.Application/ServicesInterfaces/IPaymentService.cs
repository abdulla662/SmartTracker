using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Payments;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IPaymentService
    {
        Task<ApiResponseT<List<PaymentResponseDto>>> GetPaymentsForClientAsync(Guid clientId, CancellationToken ct = default);
        Task<ApiResponseT<PaymentResponseDto>> CreatePaymentAsync(CreatePaymentDto dto, CancellationToken ct = default);
        Task<ApiResponse> DeletePaymentAsync(Guid id, CancellationToken ct = default);
    }
}
