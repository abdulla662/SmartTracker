using AutoMapper;
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
        private readonly IMapper _mapper;

        public PaymentService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer, IMapper mapper)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
            _mapper = mapper;
        }

        public async Task<ApiResponseT<List<PaymentResponseDto>>> GetPaymentsForClientAsync(Guid clientId, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(clientId, ct);
            if (client is null)
                return ApiResponseT<List<PaymentResponseDto>>.FailureResponse(_localizer["ClientNotFound"], HttpStatusCode.NotFound);

            var payments = await _uow.Read<Payment>().ListAsync(p => p.ClientId == clientId, ct);

            var dtos = payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => { var dto = _mapper.Map<PaymentResponseDto>(p); dto.ClientName = client.Name; return dto; })
                .ToList();

            return ApiResponseT<List<PaymentResponseDto>>.SuccessResponse(dtos);
        }

        public async Task<ApiResponseT<PaymentResponseDto>> CreatePaymentAsync(CreatePaymentDto dto, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(dto.ClientId, ct);
            if (client is null)
                return ApiResponseT<PaymentResponseDto>.FailureResponse(_localizer["ClientNotFound"], HttpStatusCode.NotFound);

            var payment = new Payment(_currentUser.TenantId, dto.ClientId, dto.Amount);
            await _uow.Write<Payment>().AddAsync(payment, ct);
            await _uow.SaveChangesAsync();

            var result = _mapper.Map<PaymentResponseDto>(payment);
            result.ClientName = client.Name;

            return ApiResponseT<PaymentResponseDto>.SuccessResponse(result, _localizer["PaymentRecorded"], HttpStatusCode.Created);
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

        public async Task<ApiResponse> UpdatePaymentAsync(Guid id, UpdatePaymentDto dto, CancellationToken ct)
        {
            var payment = await _uow.Read<Payment>().GetByIdAsync(id, ct);
            if (payment is null)
                return ApiResponse.FailureResponse(_localizer["PaymentNotFound"], HttpStatusCode.NotFound);

            payment.UpdateAmount(dto.Amount);
            await _uow.Write<Payment>().UpdateAsync(payment, ct);
            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["PaymentUpdated"]);
        }
    }
}
