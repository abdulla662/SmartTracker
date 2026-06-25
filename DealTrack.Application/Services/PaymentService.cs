using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Payments;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using Microsoft.Extensions.Localization;
using System.Net;

namespace DealTrack.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public PaymentService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
        }

        public async Task<ApiResponseT<List<PaymentResponseDto>>> GetPaymentsForClientAsync(
            Guid clientId, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(clientId, ct);
            if (client is null)
                return ApiResponseT<List<PaymentResponseDto>>.FailureResponse(
                    _localizer["ClientNotFound"], HttpStatusCode.NotFound);

            var payments = await _uow.Read<Payment>().ListAsync(p => p.ClientId == clientId, ct);

            var dtos = payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => MapToDto(p, client.Name))
                .ToList();

            return ApiResponseT<List<PaymentResponseDto>>.SuccessResponse(dtos);
        }

        public async Task<ApiResponseT<PaymentResponseDto>> CreatePaymentAsync(
            CreatePaymentDto dto, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(dto.ClientId, ct);
            if (client is null)
                return ApiResponseT<PaymentResponseDto>.FailureResponse(
                    _localizer["ClientNotFound"], HttpStatusCode.NotFound);

            var payment = new Payment(_currentUser.TenantId, dto.ClientId, dto.Amount);
            await _uow.Write<Payment>().AddAsync(payment, ct);
            await _uow.SaveChangesAsync();

            return ApiResponseT<PaymentResponseDto>.SuccessResponse(
                MapToDto(payment, client.Name), _localizer["PaymentRecorded"], HttpStatusCode.Created);
        }

        public async Task<ApiResponse> DeletePaymentAsync(Guid id, CancellationToken ct = default)
        {
            var payment = await _uow.Read<Payment>().GetByIdAsync(id, ct);
            if (payment is null)
                return ApiResponse.FailureResponse(_localizer["PaymentNotFound"], HttpStatusCode.NotFound);

            await _uow.SoftDelete<Payment>().SoftDeleteAsync(payment, ct);
            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["PaymentDeleted"]);
        }

        private static PaymentResponseDto MapToDto(Payment p, string clientName) => new()
        {
            Id = p.Id,
            ClientId = p.ClientId,
            ClientName = clientName,
            Amount = p.Amount,
            PaymentDate = p.PaymentDate,
            CreatedAt = p.CreatedAt
        };
    }
}
