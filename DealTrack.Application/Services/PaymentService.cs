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

        public async Task<ApiResponseT<PagedResult<PaymentResponseDto>>> GetAllPaymentsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
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

            var ordered = payments.OrderByDescending(p => p.PaymentDate).ToList();
            var totalCount = ordered.Count;
            var items = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => {
                    var dto = _mapper.Map<PaymentResponseDto>(p);
                    dto.ClientName = clients.TryGetValue(p.ClientId, out var name) ? name : string.Empty;
                    return dto;
                })
                .ToList();

            return ApiResponseT<PagedResult<PaymentResponseDto>>.SuccessResponse(
                new PagedResult<PaymentResponseDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
        }

        public async Task<ApiResponseT<PagedResult<PaymentResponseDto>>> GetPaymentsForClientAsync(Guid clientId, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(clientId, ct);
            if (client is null)
                return ApiResponseT<PagedResult<PaymentResponseDto>>.FailureResponse(_localizer["ClientNotFound"], HttpStatusCode.NotFound);

            var payments = await _uow.Read<Payment>().ListAsync(p => p.ClientId == clientId, ct);

            var ordered = payments.OrderByDescending(p => p.PaymentDate).ToList();
            var totalCount = ordered.Count;
            var items = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => { var dto = _mapper.Map<PaymentResponseDto>(p); dto.ClientName = client.Name; return dto; })
                .ToList();

            return ApiResponseT<PagedResult<PaymentResponseDto>>.SuccessResponse(
                new PagedResult<PaymentResponseDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
        }

        public async Task<ApiResponseT<PaymentResponseDto>> CreatePaymentAsync(CreatePaymentDto dto, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(dto.ClientId, ct);
            if (client is null)
                return ApiResponseT<PaymentResponseDto>.FailureResponse(_localizer["ClientNotFound"], HttpStatusCode.NotFound);

            var payment = new Payment(_currentUser.TenantId, dto.ClientId, dto.Amount);
            await _uow.Write<Payment>().AddAsync(payment, ct);

            await SyncFinancialSummaryAsync(dto.ClientId, ct, dealAmount: dto.TotalDealAmount);

            await _uow.SaveChangesAsync();
            await _activityLog.LogAsync("CreatePayment", payment.Id, "Payment", ct);

            var result = _mapper.Map<PaymentResponseDto>(payment);
            result.ClientName = client.Name;

            return ApiResponseT<PaymentResponseDto>.SuccessResponse(result, _localizer["PaymentRecorded"], HttpStatusCode.Created);
        }

        public async Task<ApiResponseT<List<ClientPaymentSummaryDto>>> GetClientSummariesAsync(CancellationToken ct = default)
        {
            var tenantId = _currentUser.TenantId;

            var summaries = await _uow.Read<ClientFinancialSummary>()
                .ListAsync(s => s.TenantId == tenantId, ct);

            var clientIds = summaries.Select(s => s.ClientId).ToList();
            var clients   = await _uow.Read<Client>()
                .ListAsync(c => clientIds.Contains(c.Id), ct);
            var clientMap = clients.ToDictionary(c => c.Id);

            var payments  = await _uow.Read<Payment>()
                .ListAsync(p => p.TenantId == tenantId, ct);
            var countMap  = payments.GroupBy(p => p.ClientId)
                .ToDictionary(g => g.Key, g => g.Count());
            var lastMap   = payments.GroupBy(p => p.ClientId)
                .ToDictionary(g => g.Key, g => g.Max(p => p.PaymentDate));

            var result = summaries
                .Where(s => clientMap.ContainsKey(s.ClientId))
                .Select(s => new ClientPaymentSummaryDto
                {
                    ClientId       = s.ClientId,
                    ClientName     = clientMap[s.ClientId].Name,
                    TotalAmount    = s.TotalAmount,
                    PaidAmount     = s.PaidAmount,
                    Remaining      = s.Remaining,
                    Status         = s.Status.ToString(),
                    PaymentCount   = countMap.GetValueOrDefault(s.ClientId, 0),
                    LastPaymentDate = lastMap.TryGetValue(s.ClientId, out var d) ? d : null,
                })
                .OrderByDescending(s => s.PaidAmount)
                .ToList();

            return ApiResponseT<List<ClientPaymentSummaryDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse> SetDealAmountAsync(Guid clientId, decimal totalDealAmount, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(clientId, ct);
            if (client is null)
                return ApiResponse.FailureResponse(_localizer["ClientNotFound"], HttpStatusCode.NotFound);

            var summary = await _uow.Read<ClientFinancialSummary>()
                .GetSingleAsync(s => s.ClientId == clientId, ct);

            if (summary is null)
            {
                summary = new ClientFinancialSummary(_currentUser.TenantId, clientId, totalDealAmount);
                await _uow.Write<ClientFinancialSummary>().AddAsync(summary, ct);
            }
            else
            {
                summary.SetTotalAmount(totalDealAmount);
                await _uow.Write<ClientFinancialSummary>().UpdateAsync(summary, ct);
            }

            await _uow.SaveChangesAsync();
            return ApiResponse.SuccessResponse();
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

        private async Task SyncFinancialSummaryAsync(Guid clientId, CancellationToken ct, Guid? excludePaymentId = null, decimal? dealAmount = null)
        {
            var payments = await _uow.Read<Payment>().ListAsync(p => p.ClientId == clientId, ct);

            var paidTotal = payments
                .Where(p => excludePaymentId == null || p.Id != excludePaymentId)
                .Sum(p => p.Amount);

            var summary = await _uow.Read<ClientFinancialSummary>()
                .GetSingleAsync(s => s.ClientId == clientId, ct);

            if (summary is null)
            {
                summary = new ClientFinancialSummary(_currentUser.TenantId, clientId, dealAmount ?? 0);
                summary.RecalculatePaidAmount(paidTotal);
                await _uow.Write<ClientFinancialSummary>().AddAsync(summary, ct);
            }
            else
            {
                if (dealAmount.HasValue) summary.SetTotalAmount(dealAmount.Value);
                summary.RecalculatePaidAmount(paidTotal);
                await _uow.Write<ClientFinancialSummary>().UpdateAsync(summary, ct);
            }
        }
    }
}
