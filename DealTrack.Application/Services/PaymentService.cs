using AutoMapper;
using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Payments;
using DealTrack.Application.Helpers;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
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
        private readonly INotificationService _notifications;

        public PaymentService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer, IMapper mapper, IActivityLogService activityLog, INotificationService notifications)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
            _mapper = mapper;
            _activityLog = activityLog;
            _notifications = notifications;
        }

        public async Task<ApiResponseT<PagedResult<PaymentResponseDto>>> GetAllPaymentsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var tenantId = _currentUser.TenantId;
            var userId   = _currentUser.UserId;

            List<Payment> payments;
            if (_currentUser.Role == UserRole.Sales)
            {
                var myClients  = await _uow.Read<Client>().ListAsync(c => c.AssignedToUserId == userId && c.TenantId == tenantId, ct);
                var myClientIds = myClients.Select(c => c.Id).ToHashSet();
                payments = await _uow.Read<Payment>().ListAsync(p => p.TenantId == tenantId && myClientIds.Contains(p.ClientId), ct);
            }
            else
            {
                payments = await _uow.Read<Payment>().ListAsync(p => p.TenantId == tenantId, ct);
            }

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

            await _notifications.CreateAsync(
                Guid.Parse(_currentUser.UserId),
                _currentUser.TenantId,
                NotifKey.Build("notif.title.paymentRecorded"),
                NotifKey.Build("notif.msg.paymentRecorded", dto.Amount.ToString("F2"), client.Name),
                NotificationType.PaymentReminder, ct);
            await _notifications.NotifyUpstreamAsync(_currentUser.UserId, _currentUser.TenantId, "recordedPayment", new[] { dto.Amount.ToString("F2"), client.Name }, $"/clients/{dto.ClientId}", ct);

            var result = _mapper.Map<PaymentResponseDto>(payment);
            result.ClientName = client.Name;

            return ApiResponseT<PaymentResponseDto>.SuccessResponse(result, _localizer["PaymentRecorded"], HttpStatusCode.Created);
        }

        public async Task<ApiResponseT<List<ClientPaymentSummaryDto>>> GetClientSummariesAsync(CancellationToken ct = default)
        {
            var tenantId = _currentUser.TenantId;

            var userId = _currentUser.UserId;

            IList<Client> visibleClients;
            if (_currentUser.Role == UserRole.Sales)
                visibleClients = await _uow.Read<Client>().ListAsync(c => c.AssignedToUserId == userId && c.TenantId == tenantId, ct);
            else
                visibleClients = await _uow.Read<Client>().ListAsync(c => c.TenantId == tenantId, ct);

            var visibleClientIds = visibleClients.Select(c => c.Id).ToHashSet();

            var summaries = await _uow.Read<ClientFinancialSummary>()
                .ListAsync(s => s.TenantId == tenantId && visibleClientIds.Contains(s.ClientId), ct);

            var clientIds = summaries.Select(s => s.ClientId).ToList();
            var clientMap = visibleClients.Where(c => clientIds.Contains(c.Id)).ToDictionary(c => c.Id);

            var payments  = await _uow.Read<Payment>()
                .ListAsync(p => p.TenantId == tenantId && visibleClientIds.Contains(p.ClientId), ct);
            var countMap  = payments.GroupBy(p => p.ClientId)
                .ToDictionary(g => g.Key, g => g.Count());
            var lastMap   = payments.GroupBy(p => p.ClientId)
                .ToDictionary(g => g.Key, g => g.Max(p => p.PaymentDate));

            // Recalculate paid fresh from payments (ClientFinancialSummary.PaidAmount can be stale)
            var paidMap = payments.GroupBy(p => p.ClientId)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

            // Auto-heal any stale summaries silently
            var stale = summaries.Where(s => paidMap.ContainsKey(s.ClientId) && s.PaidAmount != paidMap[s.ClientId]).ToList();
            foreach (var s in stale)
            {
                s.RecalculatePaidAmount(paidMap[s.ClientId]);
                await _uow.Write<ClientFinancialSummary>().UpdateAsync(s, ct);
            }
            if (stale.Any()) await _uow.SaveChangesAsync();

            var result = summaries
                .Where(s => clientMap.ContainsKey(s.ClientId))
                .Select(s =>
                {
                    var actualPaid = paidMap.GetValueOrDefault(s.ClientId, 0);
                    var remaining  = s.TotalAmount > 0 ? s.TotalAmount - actualPaid : 0;
                    var status     = s.TotalAmount <= 0  ? "Pending"
                                   : actualPaid <= 0     ? "Pending"
                                   : actualPaid >= s.TotalAmount ? "Paid"
                                   : "PartiallyPaid";
                    return new ClientPaymentSummaryDto
                    {
                        ClientId        = s.ClientId,
                        ClientName      = clientMap[s.ClientId].Name,
                        TotalAmount     = s.TotalAmount,
                        PaidAmount      = actualPaid,
                        Remaining       = remaining,
                        Status          = status,
                        PaymentCount    = countMap.GetValueOrDefault(s.ClientId, 0),
                        LastPaymentDate = lastMap.TryGetValue(s.ClientId, out var d) ? d : null,
                    };
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
            await _notifications.NotifyUpstreamAsync(_currentUser.UserId, _currentUser.TenantId, "deletedPayment", Array.Empty<string>(), null, ct);

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
            await _notifications.NotifyUpstreamAsync(_currentUser.UserId, _currentUser.TenantId, "updatedPayment", new[] { dto.Amount.ToString("F2") }, $"/clients/{payment.ClientId}", ct);

            return ApiResponse.SuccessResponse(message: _localizer["PaymentUpdated"]);
        }

        public async Task<ApiResponse> DeleteClientSummaryAsync(Guid clientId, CancellationToken ct = default)
        {
            var payments = await _uow.Read<Payment>().ListAsync(p => p.ClientId == clientId, ct);
            foreach (var p in payments)
                await _uow.SoftDelete<Payment>().SoftDeleteAsync(p, ct);

            var summary = await _uow.Read<ClientFinancialSummary>().GetSingleAsync(s => s.ClientId == clientId, ct);
            if (summary is not null)
                await _uow.Write<ClientFinancialSummary>().DeleteAsync(summary, ct);

            await _uow.SaveChangesAsync();
            return ApiResponse.SuccessResponse();
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
