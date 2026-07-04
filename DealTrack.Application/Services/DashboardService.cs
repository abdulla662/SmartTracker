using AutoMapper;
using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Dashboard;
using DealTrack.Application.DTOs.FollowUps;
using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;

namespace DealTrack.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public DashboardService(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
        {
            _uow = uow;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<ApiResponseT<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default)
        {
            var userId = _currentUser.UserId;
            var userGuid = Guid.Parse(userId);
            var role = _currentUser.Role;
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            List<Guid> visibleUserGuids;
            List<string> visibleUserIds;

            if (role == UserRole.Admin)
            {
                visibleUserGuids = new List<Guid>();
                visibleUserIds = new List<string>();
            }
            else if (role == UserRole.TeamLead)
            {
                var teamMembers = await _uow.Read<ApplicationUser>()
                    .ListAsync(u => u.TeamLeadId == userGuid, ct);
                visibleUserGuids = teamMembers.Select(u => Guid.Parse(u.Id)).Append(userGuid).ToList();
                visibleUserIds = teamMembers.Select(u => u.Id).Append(userId).ToList();
            }
            else
            {
                visibleUserGuids = new List<Guid> { userGuid };
                visibleUserIds = new List<string> { userId };
            }

            // Follow-ups
            var allFollowUps = role == UserRole.Admin
                ? await _uow.Read<FollowUp>().ListAsync(ct)
                : await _uow.Read<FollowUp>().ListAsync(
                    f => visibleUserGuids.Contains(f.CreatedByUserId), ct);

            // Client names for follow-up DTOs
            var clientIds = allFollowUps.Select(f => f.ClientId).Distinct().ToList();
            var clients = clientIds.Any()
                ? await _uow.Read<Client>().ListAsync(c => clientIds.Contains(c.Id), ct)
                : new List<Client>();
            var clientNames = clients.ToDictionary(c => c.Id, c => c.Name);

            var todayFollowUps = allFollowUps
                .Where(f => f.FollowUpDate >= today && f.FollowUpDate < tomorrow
                            && f.Status == FollowUpStatus.Pending)
                .OrderBy(f => f.FollowUpDate)
                .ToList();

            var overdueFollowUps = allFollowUps
                .Where(f => f.FollowUpDate < today && f.Status == FollowUpStatus.Pending)
                .OrderBy(f => f.FollowUpDate)
                .ToList();

            FollowUpResponseDto MapWithClientName(FollowUp f)
            {
                var dto = _mapper.Map<FollowUpResponseDto>(f);
                dto.ClientName = clientNames.GetValueOrDefault(f.ClientId, "");
                return dto;
            }

            // Clients count
            var totalClients = role == UserRole.Admin
                ? await _uow.Read<Client>().CountAsync(ct)
                : await _uow.Read<Client>().CountAsync(
                    c => visibleUserIds.Contains(c.AssignedToUserId), ct);

            // Payments
            var payments = role == UserRole.Admin
                ? await _uow.Read<Payment>().ListAsync(ct)
                : clientIds.Any()
                    ? await _uow.Read<Payment>().ListAsync(p => clientIds.Contains(p.ClientId), ct)
                    : new List<Payment>();

            var summary = new DashboardSummaryDto
            {
                TodayFollowUpsCount = todayFollowUps.Count,
                OverdueFollowUpsCount = overdueFollowUps.Count,
                PendingFollowUpsCount = allFollowUps.Count(f => f.Status == FollowUpStatus.Pending),
                CompletedTodayCount = allFollowUps.Count(f =>
                    f.FollowUpDate >= today && f.FollowUpDate < tomorrow
                    && f.Status == FollowUpStatus.Done),
                TotalClientsCount = totalClients,
                TotalPaidAmount = payments.Sum(p => p.Amount),
                TotalPaymentsCount = payments.Count,
                TodayFollowUps = todayFollowUps.Select(MapWithClientName).ToList(),
                OverdueFollowUps = overdueFollowUps.Select(MapWithClientName).ToList()
            };

            return ApiResponseT<DashboardSummaryDto>.SuccessResponse(summary);
        }
    }
}
