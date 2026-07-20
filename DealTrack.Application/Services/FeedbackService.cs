using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Feedback;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System.Net;

namespace DealTrack.Application.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ITargetService _targetService;

        public FeedbackService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer, ITargetService targetService)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
            _targetService = targetService;
        }

        public async Task<ApiResponseT<List<FeedbackMemberDto>>> GetTeamFeedbackAsync(int? month, int? year, CancellationToken ct = default)
        {
            var role = _currentUser.Role;
            var userId = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;

            if (role != UserRole.Admin && role != UserRole.TeamLead)
                return ApiResponseT<List<FeedbackMemberDto>>.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);

            List<ApplicationUser> members;
            if (role == UserRole.Admin)
            {
                members = await _uow.Read<ApplicationUser>().ListAsync(
                    u => u.TenantId == tenantId && (u.Role == UserRole.TeamLead || u.Role == UserRole.Sales), ct);
            }
            else
            {
                members = await _uow.Read<ApplicationUser>().ListAsync(
                    u => u.TenantId == tenantId && u.TeamLeadId.HasValue && u.TeamLeadId.ToString() == userId, ct);
            }

            var result = new List<FeedbackMemberDto>();
            foreach (var member in members)
            {
                var targets = await _uow.Read<Target>().ListAsync(
                    t => t.AssignedToUserId == member.Id && t.TenantId == tenantId &&
                         (!month.HasValue || t.Month == month.Value) &&
                         (!year.HasValue || t.Year == year.Value), ct);

                var targetDtos = new List<FeedbackTargetDto>();
                foreach (var t in targets)
                {
                    var progress = await CalculateProgressAsync(t, ct);
                    var pct = (t.Value.HasValue && t.Value.Value > 0) ? Math.Round(progress / t.Value.Value * 100, 1) : 0m;
                    targetDtos.Add(new FeedbackTargetDto
                    {
                        Id = t.Id,
                        TargetType = t.TargetType,
                        CustomTypeName = t.CustomTypeName,
                        CustomTypeUnit = t.CustomTypeUnit,
                        GoalValue = t.Value,
                        CurrentProgress = progress,
                        ProgressPercentage = Math.Min(pct, 100),
                        IsAchieved = t.Value.HasValue && progress >= t.Value.Value,
                        Month = t.Month,
                        Year = t.Year
                    });
                }

                result.Add(new FeedbackMemberDto
                {
                    UserId = member.Id,
                    UserName = member.FullName ?? member.UserName ?? "",
                    UserRole = member.Role.ToString(),
                    Targets = targetDtos.OrderBy(t => t.Year).ThenBy(t => t.Month).ToList()
                });
            }

            return ApiResponseT<List<FeedbackMemberDto>>.SuccessResponse(result);
        }

        public async Task<ApiResponseT<FeedbackMemberDto>> GetMyProgressAsync(int? month, int? year, CancellationToken ct = default)
        {
            var userId = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;
            var user = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == userId, ct);

            var targets = await _uow.Read<Target>().ListAsync(
                t => t.AssignedToUserId == userId && t.TenantId == tenantId &&
                     (!month.HasValue || t.Month == month.Value) &&
                     (!year.HasValue || t.Year == year.Value), ct);

            var targetDtos = new List<FeedbackTargetDto>();
            foreach (var t in targets)
            {
                var progress = await CalculateProgressAsync(t, ct);
                var pct = (t.Value.HasValue && t.Value.Value > 0) ? Math.Round(progress / t.Value.Value * 100, 1) : 0m;
                targetDtos.Add(new FeedbackTargetDto
                {
                    Id = t.Id,
                    TargetType = t.TargetType,
                    CustomTypeName = t.CustomTypeName,
                    CustomTypeUnit = t.CustomTypeUnit,
                    GoalValue = t.Value,
                    CurrentProgress = progress,
                    ProgressPercentage = Math.Min(pct, 100),
                    IsAchieved = t.Value.HasValue && progress >= t.Value.Value,
                    Month = t.Month,
                    Year = t.Year
                });
            }

            return ApiResponseT<FeedbackMemberDto>.SuccessResponse(new FeedbackMemberDto
            {
                UserId = userId,
                UserName = user?.FullName ?? user?.UserName ?? "",
                UserRole = user?.Role.ToString() ?? "",
                Targets = targetDtos.OrderBy(t => t.Year).ThenBy(t => t.Month).ToList()
            });
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
                    return await _uow.Read<Client>().CountAsync(
                        c => c.AssignedToUserId == userId && c.TenantId == target.TenantId &&
                             c.CreatedAt.Month == target.Month && c.CreatedAt.Year == target.Year, ct);
                }
                case TargetType.FollowUps:
                {
                    var guid = Guid.Parse(userId);
                    return await _uow.Read<FollowUp>().CountAsync(
                        f => f.CreatedByUserId == guid && f.TenantId == target.TenantId &&
                             f.Status == FollowUpStatus.Done &&
                             f.UpdatedAt.Month == target.Month && f.UpdatedAt.Year == target.Year, ct);
                }
                default:
                    return 0;
            }
        }
    }
}
