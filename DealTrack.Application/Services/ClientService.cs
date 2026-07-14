using AutoMapper;
using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Clients;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Constants;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.Extensions.Localization;
using System.Net;

namespace DealTrack.Application.Services
{
    public class ClientService : IClientService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IMapper _mapper;
        private readonly IActivityLogService _activityLog;
        private readonly INotificationService _notifications;

        public ClientService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer, IMapper mapper, IActivityLogService activityLog, INotificationService notifications)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
            _mapper = mapper;
            _activityLog = activityLog;
            _notifications = notifications;
        }

        public async Task<ApiResponseT<PagedResult<ClientResponseDto>>> GetClientsAsync(ClientFilterDto filter, CancellationToken ct = default)
        {
            var userId = _currentUser.UserId;
            var isAdmin = _currentUser.Role == UserRole.Admin;

            var all = await _uow.Read<Client>().ListAsync(c =>
                (isAdmin || c.AssignedToUserId == userId) &&
                (string.IsNullOrEmpty(filter.Search) ||
                 c.Name.Contains(filter.Search) ||
                 c.Phone.Contains(filter.Search)), ct);

            var totalCount = all.Count;
            var items = all
                .OrderByDescending(c => c.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(c => _mapper.Map<ClientResponseDto>(c))
                .ToList();

            return ApiResponseT<PagedResult<ClientResponseDto>>.SuccessResponse(new PagedResult<ClientResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponseT<ClientResponseDto>> GetClientByIdAsync(Guid id, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(id, ct);
            if (client is null)
                return ApiResponseT<ClientResponseDto>.FailureResponse(_localizer["ClientNotFound"], HttpStatusCode.NotFound);

            if (!CanAccess(client))
                return ApiResponseT<ClientResponseDto>.FailureResponse(_localizer["AccessDenied"], HttpStatusCode.Forbidden);

            return ApiResponseT<ClientResponseDto>.SuccessResponse(_mapper.Map<ClientResponseDto>(client));
        }

        public async Task<ApiResponseT<ClientResponseDto>> CreateClientAsync(CreateClientDto dto, CancellationToken ct = default)
        {
            var userId = _currentUser.UserId;

            var maxClients = PlanLimits.GetMaxClients(_currentUser.SubscriptionPlan);
            if (maxClients != int.MaxValue)
            {
                var clientCount = await _uow.Read<Client>()
                    .CountAsync(c => c.AssignedToUserId == userId, ct);

                if (clientCount >= maxClients)
                    return ApiResponseT<ClientResponseDto>.FailureResponse(
                        _localizer["PlanLimitReached"], HttpStatusCode.Forbidden);
            }

            var phoneExists = await _uow.Read<Client>().AnyAsync(c => c.Phone == dto.Phone && c.AssignedToUserId == userId, ct);
            if (phoneExists)
                return ApiResponseT<ClientResponseDto>.FailureResponse(_localizer["ClientPhoneExists"]);

            var client = new Client(_currentUser.TenantId, dto.Name, dto.Phone, dto.Notes, userId);
            await _uow.Write<Client>().AddAsync(client, ct);
            await _uow.SaveChangesAsync();
            await _activityLog.LogAsync("CreateClient", client.Id, "Client", ct);
            await _notifications.NotifyUpstreamAsync(_currentUser.UserId, _currentUser.TenantId, $"added a new client ({dto.Name})", ct);

            return ApiResponseT<ClientResponseDto>.SuccessResponse(
                _mapper.Map<ClientResponseDto>(client), _localizer["ClientCreated"], HttpStatusCode.Created);
        }

        public async Task<ApiResponse> UpdateClientAsync(Guid id, UpdateClientDto dto, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(id, ct);
            if (client is null)
                return ApiResponse.FailureResponse(_localizer["ClientNotFound"], HttpStatusCode.NotFound);

            if (!CanAccess(client))
                return ApiResponse.FailureResponse(_localizer["AccessDenied"], HttpStatusCode.Forbidden);

            client.Update(dto.Name, dto.Phone, dto.Notes);
            await _uow.Write<Client>().UpdateAsync(client, ct);
            await _uow.SaveChangesAsync();
            await _activityLog.LogAsync("UpdateClient", client.Id, "Client", ct);
            await _notifications.NotifyUpstreamAsync(_currentUser.UserId, _currentUser.TenantId, $"updated client ({client.Name})", ct);

            return ApiResponse.SuccessResponse(message: _localizer["ClientUpdated"]);
        }

        public async Task<ApiResponse> DeleteClientAsync(Guid id, CancellationToken ct = default)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(id, ct);
            if (client is null)
                return ApiResponse.FailureResponse(_localizer["ClientNotFound"], HttpStatusCode.NotFound);

            if (!CanAccess(client))
                return ApiResponse.FailureResponse(_localizer["AccessDenied"], HttpStatusCode.Forbidden);

            // Cascade soft-delete follow-ups and payments
            var followUps = await _uow.Read<FollowUp>().ListAsync(f => f.ClientId == id, ct);
            foreach (var f in followUps)
                await _uow.SoftDelete<FollowUp>().SoftDeleteAsync(f, ct);

            var payments = await _uow.Read<Payment>().ListAsync(p => p.ClientId == id, ct);
            foreach (var p in payments)
                await _uow.SoftDelete<Payment>().SoftDeleteAsync(p, ct);

            await _uow.SoftDelete<Client>().SoftDeleteAsync(client, ct);
            await _uow.SaveChangesAsync();
            await _activityLog.LogAsync("DeleteClient", client.Id, "Client", ct);
            await _notifications.NotifyUpstreamAsync(_currentUser.UserId, _currentUser.TenantId, $"deleted client ({client.Name})", ct);

            return ApiResponse.SuccessResponse(message: _localizer["ClientDeleted"]);
        }

        public async Task<ApiResponse> ReassignClientAsync(Guid clientId, ReassignClientDto dto, CancellationToken ct)
        {
            var client = await _uow.Read<Client>().GetByIdAsync(clientId, ct);
            if (client is null)
                return ApiResponse.FailureResponse(_localizer["ClientNotFound"], HttpStatusCode.NotFound);

            var newSales = await _uow.Read<ApplicationUser>()
                .GetSingleAsync(u => u.Id == dto.NewSalesUserId, ct);
            if (newSales is null)
                return ApiResponse.FailureResponse(_localizer["UserNotFound"], HttpStatusCode.NotFound);

            if (_currentUser.Role == UserRole.Admin)
            {
                if (dto.NewTenantId.HasValue && dto.NewTenantId != _currentUser.TenantId)
                {
                    var isAdminInNewTenant = await _uow.Read<ApplicationUser>()
                        .AnyAsync(u => u.Id == _currentUser.UserId
                            && u.TenantId == dto.NewTenantId.Value, ct);

                    if (!isAdminInNewTenant)
                        return ApiResponse.FailureResponse(_localizer["AccessDenied"], HttpStatusCode.Forbidden);
                }
            }
            else if (_currentUser.Role == UserRole.TeamLead)
            {
                if (dto.NewTenantId.HasValue)
                    return ApiResponse.FailureResponse(_localizer["AccessDenied"], HttpStatusCode.Forbidden);

                if (newSales.TenantId != _currentUser.TenantId)
                    return ApiResponse.FailureResponse(_localizer["AccessDenied"], HttpStatusCode.Forbidden);
            }
            else
            {
                return ApiResponse.FailureResponse(_localizer["AccessDenied"], HttpStatusCode.Forbidden);
            }

            // Get old Sales before reassign to notify their TeamLead
            var oldSales = await _uow.Read<ApplicationUser>()
                .GetSingleAsync(u => u.Id == client.AssignedToUserId, ct);

            client.Reassign(dto.NewSalesUserId, dto.NewTenantId);
            await _uow.Write<Client>().UpdateAsync(client, ct);
            await _uow.SaveChangesAsync();
            await _activityLog.LogAsync("ReassignClient", client.Id, "Client", ct);

            // Notify the new Sales person
            await _notifications.CreateAsync(
                Guid.Parse(dto.NewSalesUserId),
                newSales.TenantId,
                "Client Assigned to You",
                $"Client '{client.Name}' has been assigned to you.",
                NotificationType.ClientReassigned, ct);

            // Notify the new TeamLead — Ahmed joined your team
            if (newSales.TeamLeadId.HasValue)
            {
                await _notifications.CreateAsync(
                    newSales.TeamLeadId.Value,
                    newSales.TenantId,
                    "New Member Joined Your Team",
                    $"'{newSales.FullName}' has been added to your team by the admin.",
                    NotificationType.NewMemberJoined, ct);
            }

            // Notify the old TeamLead — Ahmed left your team
            if (oldSales?.TeamLeadId.HasValue == true && oldSales.TeamLeadId != newSales.TeamLeadId)
            {
                await _notifications.CreateAsync(
                    oldSales.TeamLeadId.Value,
                    oldSales.TenantId,
                    "Team Member Left Your Team",
                    $"'{oldSales.FullName}' has been transferred to another team by the admin.",
                    NotificationType.ClientReassigned, ct);
            }

            return ApiResponse.SuccessResponse(message: _localizer["ClientReassigned"]);
        }

        private bool CanAccess(Client client) =>
            _currentUser.Role == UserRole.Admin || client.AssignedToUserId == _currentUser.UserId;
    }
}
