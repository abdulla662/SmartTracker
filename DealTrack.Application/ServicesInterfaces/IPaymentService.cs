using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Payments;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IPaymentService
    {
        Task<ApiResponseT<PagedResult<PaymentResponseDto>>> GetPaymentsForClientAsync(Guid clientId, int page = 1, int pageSize = 20, CancellationToken ct = default);
        Task<ApiResponseT<PagedResult<PaymentResponseDto>>> GetAllPaymentsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
        Task<ApiResponseT<PaymentResponseDto>> CreatePaymentAsync(CreatePaymentDto dto, CancellationToken ct = default);
        Task<ApiResponse> DeletePaymentAsync(Guid id, CancellationToken ct = default);
        Task<ApiResponse> UpdatePaymentAsync(Guid id, UpdatePaymentDto dto, CancellationToken ct);
        Task<ApiResponseT<List<ClientPaymentSummaryDto>>> GetClientSummariesAsync(CancellationToken ct = default);
        Task<ApiResponse> SetDealAmountAsync(Guid clientId, decimal totalDealAmount, CancellationToken ct = default);
    }
}
