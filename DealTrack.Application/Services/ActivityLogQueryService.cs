using DealTrack.Application.Common;
using DealTrack.Application.DTOs.ActivityLog;
using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;

namespace DealTrack.Application.Services
{
    public class ActivityLogQueryService : IActivityLogQueryService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public ActivityLogQueryService(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<ApiResponseT<PagedResult<ActivityLogResponseDto>>> GetLogsAsync(ActivityLogFilterDto filter, CancellationToken ct)
        {
            var userId = _currentUser.UserId;
            var userGuid = Guid.Parse(userId);
            var role = _currentUser.Role;

            List<Guid> visibleUserIds;

            if (role == UserRole.Admin)
            {
                var allUsers = await _uow.Read<ApplicationUser>()
                    .ListAsync(u => u.TenantId == _currentUser.TenantId, ct);
                visibleUserIds = allUsers.Select(u => Guid.Parse(u.Id)).ToList();
            }
            else if (role == UserRole.TeamLead)
            {
                var teamMembers = await _uow.Read<ApplicationUser>()
                    .ListAsync(u => u.TeamLeadId == userGuid, ct);
                visibleUserIds = teamMembers.Select(u => Guid.Parse(u.Id)).Append(userGuid).ToList();
            }
            else
            {
                visibleUserIds = new List<Guid> { userGuid };
            }

            var all = await _uow.Read<ActivityLog>().ListAsync(l =>
                visibleUserIds.Contains(l.UserId) &&
                (filter.EntityType == null || l.EntityType == filter.EntityType) &&
                (filter.Action == null || l.Action == filter.Action) &&
                (filter.From == null || l.CreatedAt >= filter.From) &&
                (filter.To == null || l.CreatedAt <= filter.To), ct);

            var userIds = all.Select(l => l.UserId.ToString()).Distinct().ToList();
            var users = await _uow.Read<ApplicationUser>()
                .ListAsync(u => userIds.Contains(u.Id), ct);
            var userNames = users.ToDictionary(u => u.Id, u => u.FullName);

            var totalCount = all.Count;
            var items = all
                .OrderByDescending(l => l.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(l => new ActivityLogResponseDto
                {
                    Id = l.Id,
                    Action = l.Action,
                    EntityType = l.EntityType ?? string.Empty,
                    EntityId = l.EntityId,
                    UserId = l.UserId.ToString(),
                    UserFullName = userNames.GetValueOrDefault(l.UserId.ToString(), ""),
                    CreatedAt = l.CreatedAt
                }).ToList();

            return ApiResponseT<PagedResult<ActivityLogResponseDto>>.SuccessResponse(
                new PagedResult<ActivityLogResponseDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize
                });
        }
    }
}