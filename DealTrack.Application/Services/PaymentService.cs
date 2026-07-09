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
        private readonly IActivityLogService _activityLog;

        public PaymentService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer, IMapper mapper, IActivityLogService activityLog)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
            _mapper = mapper;
            _activityLog = activityLog;
        }

        public async Task<ApiResponseT<List<PaymentResponseDto>>> GetAllPaymentsAsync(CancellationToken ct = default)
        {
            var tenantId = _currentUser.TenantId;
            var payments = await _uow.Read<Payment>().ListAsync(p => p.TenantId == tenantId, ct);

            var clientIds = payments.Select(p => p.ClientId).Distinct().ToList();
            var clients = new Dictionary<Guid, string>();
            foreach (var cid in clientIds)
            {
                var c = await _uow.Read<Client>().GetByIdAsync(cid, ct);
                if (c is not null) clients[cid] = c.Name;
            }

            var dtos = payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => {
                    var dto = _mapper.Map<PaymentResponseDto>(p);
                    dto.ClientName = clients.TryGetValue(p.ClientId, out var name) ? name : string.Empty;
                    return dto;
                })
                .ToList();

            return ApiResponseT<List<PaymentResponseDto>>.SuccessResponse(dtos);
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

            await SyncFinancialSummaryAsync(dto.ClientId, ct);

            await _uow.SaveChangesAsync();
            await _activityLog.LogAsync("CreatePayment", payment.Id, "Payment", ct);

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

            await SyncFinancialSummaryAsync(payment.ClientId, ct, excludePaymentId: id);

            await _uow.SaveChangesAsync();
            await _activityLog.LogAsync("DeletePayment", payment.Id, "Payment", ct);

            return ApiResponse.SuccessResponse(message: _localizer["PaymentDeleted"]);
        }

        public async Task<ApiResponse> UpdatePaymentAsync(Guid id, UpdatePaymentDto dto, CancellationToken ct)
        {
            var payment = await _uow.Read<Payment>().GetByIdAsync(id, ct);
            if (payment is null)
                return ApiResponse.FailureResponse(_localizer["PaymentNotFound"], HttpStatusCode.NotFound);

            payment.UpdateAmount(dto.Amount);
            await _uow.Write<Payment>().UpdateAsync(payment, ct);

            await SyncFinancialSummaryAsync(payment.ClientId, ct);

            await _uow.SaveChangesAsync();
            await _activityLog.LogAsync("UpdatePayment", payment.Id, "Payment", ct);

            return ApiResponse.SuccessResponse(message: _localizer["PaymentUpdated"]);
        }

        private async Task SyncFinancialSummaryAsync(Guid clientId, CancellationToken ct, Guid? excludePaymentId = null)
        {
            var payments = await _uow.Read<Payment>().ListAsync(p => p.ClientId == clientId, ct);

            var total = payments
                .Where(p => excludePaymentId == null || p.Id != excludePaymentId)
                .Sum(p => p.Amount);

            var summary = await _uow.Read<ClientFinancialSummary>()
                .GetSingleAsync(s => s.ClientId == clientId, ct);

            if (summary is null)
            {
                summary = new ClientFinancialSummary(_currentUser.TenantId, clientId, 0);
                summary.RecalculatePaidAmount(total);
                await _uow.Write<ClientFinancialSummary>().AddAsync(summary, ct);
            }
            else
            {
                summary.RecalculatePaidAmount(total);
                await _uow.Write<ClientFinancialSummary>().UpdateAsync(summary, ct);
            }
        }
    }
}
