using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Targets;
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
    public class TargetService : ITargetService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly INotificationService _notifications;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public TargetService(IUnitOfWork uow, ICurrentUserService currentUser, INotificationService notifications, IStringLocalizer<SharedResource> localizer)
        {
            _uow = uow;
            _currentUser = currentUser;
            _notifications = notifications;
            _localizer = localizer;
        }

        public async Task<ApiResponse> SetTargetAsync(CreateTargetDto dto, CancellationToken ct = default)
        {
            var currentUserId = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;
            var role = _currentUser.Role;

            var targetUserId = string.IsNullOrEmpty(dto.AssignedToUserId) ? currentUserId : dto.AssignedToUserId;

            if (dto.Month < 1 || dto.Month > 12)
                return ApiResponse.FailureResponse(_localizer["InvalidMonth"]);
            if (dto.Year < 2020 || dto.Year > 2100)
                return ApiResponse.FailureResponse(_localizer["InvalidYear"]);
            if (dto.Value.HasValue && dto.Value.Value <= 0)
                return ApiResponse.FailureResponse(_localizer["TargetValueMustBePositive"]);
            if (dto.TargetType == TargetType.Custom && string.IsNullOrWhiteSpace(dto.CustomTypeName))
                return ApiResponse.FailureResponse(_localizer["CustomTypeNameRequired"]);

            if (targetUserId != currentUserId)
            {
                var targetUser = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == targetUserId && u.TenantId == tenantId, ct);
                if (targetUser == null)
                    return ApiResponse.FailureResponse(_localizer["UserNotFound"], HttpStatusCode.NotFound);

                if (role == UserRole.Sales)
                    return ApiResponse.FailureResponse(_localizer["TargetAccessDenied"], HttpStatusCode.Forbidden);

                if (role == UserRole.TeamLead)
                {
                    if (targetUser.TeamLeadId?.ToString() != currentUserId)
                        return ApiResponse.FailureResponse(_localizer["TargetAccessDenied"], HttpStatusCode.Forbidden);
                }
            }

            var existing = await _uow.Read<Target>().GetSingleAsync(
                t => t.AssignedToUserId == targetUserId && t.TargetType == dto.TargetType &&
                     t.Month == dto.Month && t.Year == dto.Year && t.TenantId == tenantId &&
                     (dto.TargetType != TargetType.Custom || t.CustomTypeName == dto.CustomTypeName), ct);

            if (existing != null)
            {
                existing.Update(dto.Value, dto.CustomTypeName, dto.CustomTypeUnit);
                await _uow.Write<Target>().UpdateAsync(existing, ct);
            }
            else
            {
                var target = new Target(tenantId, targetUserId, currentUserId, dto.TargetType, dto.Value, dto.Month, dto.Year, dto.CustomTypeName, dto.CustomTypeUnit);
                await _uow.Write<Target>().AddAsync(target, ct);
            }

            await _uow.SaveChangesAsync();

            if (targetUserId != currentUserId)
            {
                var assigner = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == currentUserId, ct);
                var assignerName = assigner?.FullName ?? assigner?.UserName ?? "";
                var typeLabel = dto.TargetType == TargetType.Custom ? (dto.CustomTypeName ?? "Custom") : dto.TargetType.ToString();
                var valueStr = dto.Value.HasValue ? dto.Value.Value.ToString("F0") : "-";
                var period = $"{dto.Month}/{dto.Year}";

                await _notifications.CreateAsync(
                    Guid.Parse(targetUserId), tenantId,
                    NotifKey.Build("notif.title.targetAssigned"),
                    NotifKey.Build("notif.msg.targetAssigned", new[] { assignerName, typeLabel, valueStr, period }, "/targets"),
                    NotificationType.SystemNotification, ct);
            }

            return ApiResponse.SuccessResponse(message: _localizer["TargetSaved"]);
        }

        public async Task<ApiResponseT<List<TargetResponseDto>>> GetMyTargetsAsync(int? month, int? year, CancellationToken ct = default)
        {
            var userId = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;

            var targets = await _uow.Read<Target>().ListAsync(
                t => t.AssignedToUserId == userId && t.TenantId == tenantId &&
                     (!month.HasValue || t.Month == month.Value) &&
                     (!year.HasValue || t.Year == year.Value), ct);

            var result = await BuildResponsesAsync(targets, tenantId, ct);
            return ApiResponseT<List<TargetResponseDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponseT<List<TargetResponseDto>>> GetTeamTargetsAsync(int? month, int? year, CancellationToken ct = default)
        {
            var userId = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;
            var role = _currentUser.Role;

            if (role == UserRole.Sales)
                return ApiResponseT<List<TargetResponseDto>>.FailureResponse(_localizer["TargetAccessDenied"], HttpStatusCode.Forbidden);

            List<Target> targets;

            if (role == UserRole.Admin)
            {
                targets = await _uow.Read<Target>().ListAsync(
                    t => t.TenantId == tenantId &&
                         (!month.HasValue || t.Month == month.Value) &&
                         (!year.HasValue || t.Year == year.Value), ct);
            }
            else
            {
                var members = await _uow.Read<ApplicationUser>().ListAsync(
                    u => u.TenantId == tenantId && u.TeamLeadId.HasValue && u.TeamLeadId.ToString() == userId, ct);
                var memberIds = members.Select(m => m.Id).Append(userId).ToHashSet();

                targets = await _uow.Read<Target>().ListAsync(
                    t => t.TenantId == tenantId && memberIds.Contains(t.AssignedToUserId) &&
                         (!month.HasValue || t.Month == month.Value) &&
                         (!year.HasValue || t.Year == year.Value), ct);
            }

            var result = await BuildResponsesAsync(targets, tenantId, ct);
            return ApiResponseT<List<TargetResponseDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponse> DeleteTargetAsync(Guid id, CancellationToken ct = default)
        {
            var target = await _uow.Read<Target>().GetByIdAsync(id, ct);
            if (target == null)
                return ApiResponse.FailureResponse(_localizer["TargetNotFound"], HttpStatusCode.NotFound);

            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            // Only the creator or an Admin can delete
            if (role != UserRole.Admin && target.CreatedByUserId != userId)
                return ApiResponse.FailureResponse(_localizer["TargetAccessDenied"], HttpStatusCode.Forbidden);

            await _uow.SoftDelete<Target>().SoftDeleteAsync(target, ct);
            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["TargetDeleted"]);
        }

        private async Task<List<TargetResponseDto>> BuildResponsesAsync(List<Target> targets, Guid tenantId, CancellationToken ct)
        {
            if (!targets.Any()) return new List<TargetResponseDto>();

            var userIds = targets.SelectMany(t => new[] { t.AssignedToUserId, t.CreatedByUserId }).Distinct().ToList();
            var users = await _uow.Read<ApplicationUser>().ListAsync(u => userIds.Contains(u.Id), ct);
            var userMap = users.ToDictionary(u => u.Id);

            var result = new List<TargetResponseDto>();
            foreach (var t in targets)
            {
                var progress = await CalculateProgressAsync(t, ct);
                var pct = (t.Value.HasValue && t.Value.Value > 0) ? Math.Round(progress / t.Value.Value * 100, 1) : 0m;

                userMap.TryGetValue(t.AssignedToUserId, out var assignedUser);
                userMap.TryGetValue(t.CreatedByUserId, out var createdByUser);

                result.Add(new TargetResponseDto
                {
                    Id = t.Id,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedToUserName = assignedUser?.FullName ?? assignedUser?.UserName ?? "",
                    AssignedToUserRole = assignedUser?.Role.ToString() ?? "",
                    CreatedByUserId = t.CreatedByUserId,
                    CreatedByUserName = createdByUser?.FullName ?? createdByUser?.UserName ?? "",
                    TargetType = t.TargetType,
                    CustomTypeName = t.CustomTypeName,
                    CustomTypeUnit = t.CustomTypeUnit,
                    Value = t.Value,
                    CurrentProgress = progress,
                    ProgressPercentage = Math.Min(pct, 100),
                    IsAchieved = t.Value.HasValue && progress >= t.Value.Value,
                    Month = t.Month,
                    Year = t.Year
                });
            }

            return result.OrderBy(r => r.Year).ThenBy(r => r.Month).ThenBy(r => r.AssignedToUserName).ToList();
        }

        private async Task<decimal> CalculateProgressAsync(Target target, CancellationToken ct)
        {
            var userId = target.AssignedToUserId;

            switch (target.TargetType)
            {
                case TargetType.Revenue:
                {
                    var clientIds = (await _uow.Read<Client>().ListAsync(
                        c => c.AssignedToUserId == userId && c.TenantId == target.TenantId, ct))
                        .Select(c => c.Id).ToHashSet();

                    var payments = await _uow.Read<Payment>().ListAsync(
                        p => clientIds.Contains(p.ClientId) &&
                             p.PaymentDate.Month == target.Month &&
                             p.PaymentDate.Year == target.Year, ct);

                    return payments.Sum(p => p.Amount);
                }

                case TargetType.Clients:
                {
                    var count = await _uow.Read<Client>().CountAsync(
                        c => c.AssignedToUserId == userId && c.TenantId == target.TenantId &&
                             c.CreatedAt.Month == target.Month && c.CreatedAt.Year == target.Year, ct);
                    return count;
                }

                case TargetType.FollowUps:
                {
                    var guid = Guid.Parse(userId);
                    var count = await _uow.Read<FollowUp>().CountAsync(
                        f => f.CreatedByUserId == guid && f.TenantId == target.TenantId &&
                             f.Status == FollowUpStatus.Done &&
                             f.UpdatedAt.Month == target.Month && f.UpdatedAt.Year == target.Year, ct);
                    return count;
                }

                default:
                    return 0;
            }
        }
    }
}
