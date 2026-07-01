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

        public DashboardService(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<ApiResponseT<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default)
        {
            var userId = Guid.Parse(_currentUser.UserId);
            var isAdmin = _currentUser.Role == UserRole.Admin;
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var allFollowUps = await _uow.Read<FollowUp>().ListAsync(
                f => isAdmin || f.CreatedByUserId == userId, ct);

            var clientIds = allFollowUps.Select(f => f.ClientId).Distinct().ToList();
            var clients = await _uow.Read<Client>().ListAsync(
                c => clientIds.Contains(c.Id), ct);
            var clientNames = clients.ToDictionary(c => c.Id, c => c.Name);

            FollowUpResponseDto ToDto(FollowUp f) => new()
            {
                Id = f.Id,
                ClientId = f.ClientId,
                ClientName = clientNames.GetValueOrDefault(f.ClientId, ""),
                FollowUpDate = f.FollowUpDate,
                Status = f.Status,
                Notes = f.Notes,
                CreatedByUserId = f.CreatedByUserId,
                CreatedAt = f.CreatedAt
            };

            var todayFollowUps = allFollowUps
                .Where(f => f.FollowUpDate >= today && f.FollowUpDate < tomorrow
                            && f.Status == FollowUpStatus.Pending)
                .OrderBy(f => f.FollowUpDate)
                .ToList();

            var overdueFollowUps = allFollowUps
                .Where(f => f.FollowUpDate < today && f.Status == FollowUpStatus.Pending)
                .OrderBy(f => f.FollowUpDate)
                .ToList();

            var userIdStr = _currentUser.UserId;
            var totalClients = isAdmin
                ? await _uow.Read<Client>().CountAsync(ct)
                : await _uow.Read<Client>().CountAsync(c => c.AssignedToUserId == userIdStr, ct);

            var summary = new DashboardSummaryDto
            {
                TodayFollowUpsCount = todayFollowUps.Count,
                OverdueFollowUpsCount = overdueFollowUps.Count,
                PendingFollowUpsCount = allFollowUps.Count(f => f.Status == FollowUpStatus.Pending),
                CompletedTodayCount = allFollowUps.Count(f =>
                    f.FollowUpDate >= today && f.FollowUpDate < tomorrow
                    && f.Status == FollowUpStatus.Done),
                TotalClientsCount = totalClients,
                TodayFollowUps = todayFollowUps.Select(ToDto).ToList(),
                OverdueFollowUps = overdueFollowUps.Select(ToDto).ToList()
            };

            return ApiResponseT<DashboardSummaryDto>.SuccessResponse(summary);
        }
    }
}
