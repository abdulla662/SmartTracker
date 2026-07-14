using AutoMapper;
using DealTrack.Application.Common;
using DealTrack.Application.DTOs.FollowUps;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.Extensions.Localization;
using System.Net;

namespace DealTrack.Application.Services
{
    public class FollowUpService : IFollowUpService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IMapper _mapper;
        private readonly IActivityLogService _activityLog;
        private readonly INotificationService _notifications;

        public FollowUpService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer, IMapper mapper, IActivityLogService activityLog, INotificationService notifications)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
            _mapper = mapper;
            _activityLog = activityLog;
            _notifications = notifications;
        }

        public async Task<ApiResponseT<PagedResult<FollowUpResponseDto>>> GetAllFollowUpsAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var userId = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;
            var role = _currentUser.Role;

            var clients = await _uow.Read<Client>().ListAsync(c =>
                role == DealTrack.Domain.Enums.UserRole.Admin ||
                (role == DealTrack.Domain.Enums.UserRole.TeamLead && c.TenantId == tenantId) ||
                (role == DealTrack.Domain.Enums.UserRole.Sales && c.AssignedToUserId == userId), ct);

            var clientMap = clients.ToDictionary(c => c.Id, c => c.Name);
            var clientIds = clients.Select(c => c.Id).ToHashSet();

            var followUps = await _uow.Read<FollowUp>().ListAsync(f => clientIds.Contains(f.ClientId), ct);

            var ordered = followUps.OrderBy(f => f.FollowUpDate).ToList();
            var totalCount = ordered.Count;
            var items = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => { var dto = _mapper.Map<FollowUpResponseDto>(f); dto.ClientName = clientMap.GetValueOrDefault(f.ClientId, ""); return dto; })
                .ToList();

            return ApiResponseT<PagedResult<FollowUpResponseDto>>.SuccessResponse(
                new PagedResult<FollowUpResponseDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
        }

        public async Task<ApiResponseT<PagedResult<FollowUpResponseDto>>> GetFollowUpsForClientAsync(Guid clientId, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(clientId, ct);
            if (client is null)
                return ApiResponseT<PagedResult<FollowUpResponseDto>>.FailureResponse(_localizer["ClientNotFound"], HttpStatusCode.NotFound);

            var followUps = await _uow.Read<FollowUp>().ListAsync(f => f.ClientId == clientId, ct);

            var ordered = followUps.OrderBy(f => f.FollowUpDate).ToList();
            var totalCount = ordered.Count;
            var items = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => { var dto = _mapper.Map<FollowUpResponseDto>(f); dto.ClientName = client.Name; return dto; })
                .ToList();

            return ApiResponseT<PagedResult<FollowUpResponseDto>>.SuccessResponse(
                new PagedResult<FollowUpResponseDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize });
        }

        public async Task<ApiResponseT<FollowUpResponseDto>> CreateFollowUpAsync(CreateFollowUpDto dto, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(dto.ClientId, ct);
            if (client is null)
                return ApiResponseT<FollowUpResponseDto>.FailureResponse(_localizer["ClientNotFound"], HttpStatusCode.NotFound);

            var followUp = new FollowUp(_currentUser.TenantId, dto.ClientId, dto.FollowUpDate, Guid.Parse(_currentUser.UserId), dto.Notes);
            await _uow.Write<FollowUp>().AddAsync(followUp, ct);
            await _uow.SaveChangesAsync();
            await _activityLog.LogAsync("CreateFollowUp", followUp.Id, "FollowUp", ct);
            await _notifications.NotifyUpstreamAsync(_currentUser.UserId, _currentUser.TenantId, $"created a follow-up for {client.Name} on {dto.FollowUpDate:yyyy-MM-dd}", ct);

            var result = _mapper.Map<FollowUpResponseDto>(followUp);
            result.ClientName = client.Name;

            return ApiResponseT<FollowUpResponseDto>.SuccessResponse(result, _localizer["FollowUpCreated"], HttpStatusCode.Created);
        }

        public async Task<ApiResponse> MarkDoneAsync(Guid id, CancellationToken ct = default)
        {
            var followUp = await _uow.Read<FollowUp>().GetByIdAsync(id, ct);
            if (followUp is null)
                return ApiResponse.FailureResponse(_localizer["FollowUpNotFound"], HttpStatusCode.NotFound);

            followUp.MarkDone();
            await _uow.Write<FollowUp>().UpdateAsync(followUp, ct);
            await _uow.SaveChangesAsync();
            await _activityLog.LogAsync("MarkFollowUpDone", followUp.Id, "FollowUp", ct);
            await _notifications.NotifyUpstreamAsync(_currentUser.UserId, _currentUser.TenantId, "marked a follow-up as done", ct);

            return ApiResponse.SuccessResponse(message: _localizer["FollowUpMarkedDone"]);
        }

        public async Task<ApiResponse> MarkMissedAsync(Guid id, CancellationToken ct = default)
        {
            var followUp = await _uow.Read<FollowUp>().GetByIdAsync(id, ct);
            if (followUp is null)
                return ApiResponse.FailureResponse(_localizer["FollowUpNotFound"], HttpStatusCode.NotFound);

            followUp.MarkMissed();
            await _uow.Write<FollowUp>().UpdateAsync(followUp, ct);
            await _uow.SaveChangesAsync();
            await _activityLog.LogAsync("MarkFollowUpMissed", followUp.Id, "FollowUp", ct);

            await _notifications.CreateAsync(
                followUp.CreatedByUserId,
                followUp.TenantId,
                "Missed Follow-up",
                $"You missed a follow-up scheduled for {followUp.FollowUpDate:yyyy-MM-dd HH:mm}.",
                NotificationType.FollowUpReminder, ct);
            await _notifications.NotifyUpstreamAsync(_currentUser.UserId, _currentUser.TenantId, $"missed a follow-up scheduled for {followUp.FollowUpDate:yyyy-MM-dd}", ct);

            return ApiResponse.SuccessResponse(message: _localizer["FollowUpMarkedMissed"]);
        }

        public async Task<ApiResponse> DeleteFollowUpAsync(Guid id, CancellationToken ct = default)
        {
            var followUp = await _uow.Read<FollowUp>().GetByIdAsync(id, ct);
            if (followUp is null)
                return ApiResponse.FailureResponse(_localizer["FollowUpNotFound"], HttpStatusCode.NotFound);

            await _uow.SoftDelete<FollowUp>().SoftDeleteAsync(followUp, ct);
            await _uow.SaveChangesAsync();
            await _activityLog.LogAsync("DeleteFollowUp", followUp.Id, "FollowUp", ct);
            await _notifications.NotifyUpstreamAsync(_currentUser.UserId, _currentUser.TenantId, "deleted a follow-up", ct);

            return ApiResponse.SuccessResponse(message: _localizer["FollowUpDeleted"]);
        }

        public async Task<ApiResponse> UpdateFollowUpAsync(Guid id, UpdateFollowUpDto dto, CancellationToken ct)
        {
            var followUp = await _uow.Read<FollowUp>().GetByIdAsync(id, ct);
            if (followUp is null)
                return ApiResponse.FailureResponse(_localizer["FollowUpNotFound"], HttpStatusCode.NotFound);

            followUp.UpdateNotes(dto.Notes);
            followUp.UpdateDate(dto.FollowUpDate);
            if (dto.Status.HasValue)
                followUp.ChangeStatus(dto.Status.Value);
            await _uow.Write<FollowUp>().UpdateAsync(followUp, ct);
            await _uow.SaveChangesAsync();
            await _activityLog.LogAsync("UpdateFollowUp", followUp.Id, "FollowUp", ct);
            await _notifications.NotifyUpstreamAsync(_currentUser.UserId, _currentUser.TenantId, "updated a follow-up", ct);

            return ApiResponse.SuccessResponse(message: _localizer["FollowUpUpdated"]);
        }
    }
}
