using DealTrack.Application.Common;
using DealTrack.Application.DTOs.HR;
using DealTrack.Application.Helpers;
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
    public class HRService : IHRService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly INotificationService _notifications;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly UserManager<ApplicationUser> _userManager;

        public HRService(IUnitOfWork uow, ICurrentUserService currentUser, INotificationService notifications, IStringLocalizer<SharedResource> localizer, UserManager<ApplicationUser> userManager)
        {
            _uow = uow;
            _currentUser = currentUser;
            _notifications = notifications;
            _localizer = localizer;
            _userManager = userManager;
        }

        public async Task<ApiResponse> CreateActionRequestAsync(CreateHRActionDto dto, CancellationToken ct = default)
        {
            if (_currentUser.Role != UserRole.HR)
                return ApiResponse.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);

            // For AcceptJoinRequest, targetUserId comes from the join request
            if (dto.ActionType != HRActionType.AcceptJoinRequest)
            {
                var targetUser = await _uow.Read<ApplicationUser>().GetSingleAsync(
                    u => u.Id == dto.TargetUserId && u.TenantId == _currentUser.TenantId, ct);
                if (targetUser == null)
                    return ApiResponse.FailureResponse(_localizer["UserNotFound"], HttpStatusCode.NotFound);

                // HR may not submit actions against Admin or other HR users
                if (targetUser.Role == UserRole.Admin || targetUser.Role == UserRole.HR)
                    return ApiResponse.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);
            }

            if (dto.ActionType == HRActionType.AssignToTeamLead && string.IsNullOrEmpty(dto.TargetTeamLeadId))
                return ApiResponse.FailureResponse(_localizer["TeamLeadRequired"]);

            if ((dto.ActionType == HRActionType.SalaryBonus || dto.ActionType == HRActionType.SalaryDeduction)
                && (dto.Amount == null || dto.Amount <= 0))
                return ApiResponse.FailureResponse(_localizer["SalaryMustBePositive"]);

            var request = new HRActionRequest(
                _currentUser.TenantId,
                _currentUser.UserId,
                dto.TargetUserId,
                dto.ActionType,
                dto.Description,
                dto.TargetTeamLeadId,
                dto.Amount,
                dto.JoinRequestId);

            await _uow.Write<HRActionRequest>().AddAsync(request, ct);
            await _uow.SaveChangesAsync();

            // Notify all admins in this tenant
            var admins = await _uow.Read<ApplicationUser>().ListAsync(
                u => u.TenantId == _currentUser.TenantId && u.Role == UserRole.Admin, ct);

            var hrUser = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == _currentUser.UserId, ct);
            var hrName = hrUser?.FullName ?? "";
            var targetPerson = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == dto.TargetUserId, ct);
            var targetName = targetPerson?.FullName ?? targetPerson?.UserName ?? dto.TargetUserId;
            var actionKey = dto.ActionType.ToString();

            foreach (var admin in admins)
            {
                await _notifications.CreateAsync(
                    Guid.Parse(admin.Id), _currentUser.TenantId,
                    NotifKey.Build("notif.title.hrActionPending"),
                    NotifKey.Build("notif.msg.hrActionPending", new[] { hrName, actionKey, targetName }, "/hr"),
                    NotificationType.SystemNotification, ct);
            }

            return ApiResponse.SuccessResponse(message: _localizer["HRActionSubmitted"]);
        }

        public async Task<ApiResponse> ReviewActionRequestAsync(Guid id, ReviewHRActionDto dto, CancellationToken ct = default)
        {
            if (_currentUser.Role != UserRole.Admin)
                return ApiResponse.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);

            var request = await _uow.Read<HRActionRequest>().GetByIdAsync(id, ct);
            if (request == null || request.TenantId != _currentUser.TenantId)
                return ApiResponse.FailureResponse(_localizer["NotFound"], HttpStatusCode.NotFound);

            if (request.Status != HRActionStatus.Pending)
                return ApiResponse.FailureResponse(_localizer["HRActionAlreadyReviewed"]);

            if (dto.Approved)
            {
                request.Approve(_currentUser.UserId, dto.AdminNote);

                // Execute the action — fetch via UserManager so changes are tracked and persisted
                var targetUser = await _userManager.FindByIdAsync(request.TargetUserId ?? "");

                if (targetUser != null)
                {
                    var now = DateTime.UtcNow;
                    bool userChanged = false;
                    switch (request.ActionType)
                    {
                        case HRActionType.RemoveMember:
                            targetUser.IsApproved = false;
                            userChanged = true;
                            break;
                        case HRActionType.AssignToTeamLead:
                            if (!string.IsNullOrEmpty(request.TargetTeamLeadId))
                            {
                                targetUser.TeamLeadId = Guid.Parse(request.TargetTeamLeadId);
                                userChanged = true;
                            }
                            break;
                        case HRActionType.UnassignFromTeamLead:
                            targetUser.TeamLeadId = null;
                            userChanged = true;
                            break;
                        case HRActionType.PromoteToTeamLead:
                            targetUser.Role = UserRole.TeamLead;
                            targetUser.TeamLeadId = null;
                            userChanged = true;
                            break;
                        case HRActionType.DemoteToSales:
                            targetUser.Role = UserRole.Sales;
                            userChanged = true;
                            break;
                        case HRActionType.AcceptJoinRequest:
                            targetUser.IsApproved = true;
                            userChanged = true;
                            break;
                        case HRActionType.SalaryBonus:
                        case HRActionType.SalaryDeduction:
                            if (request.Amount.HasValue)
                            {
                                var adjType = request.ActionType == HRActionType.SalaryBonus
                                    ? AdjustmentType.Bonus : AdjustmentType.Deduction;
                                var adj = new SalaryAdjustment(
                                    targetUser.Id, request.TenantId,
                                    now.Month, now.Year,
                                    request.Amount.Value, adjType,
                                    request.Description,
                                    _currentUser.UserId);
                                await _uow.Write<SalaryAdjustment>().AddAsync(adj, ct);

                                var typeKey = adjType == AdjustmentType.Bonus ? "notif.msg.salaryBonus" : "notif.msg.salaryDeduction";
                                var reviewerUser = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == _currentUser.UserId, ct);
                                var accountants = await _uow.Read<ApplicationUser>()
                                    .ListAsync(u => u.TenantId == request.TenantId && u.Role == UserRole.Accountant && u.IsApproved, ct);
                                foreach (var acc in accountants)
                                    await _notifications.CreateAsync(
                                        Guid.Parse(acc.Id), request.TenantId,
                                        NotifKey.Build("notif.title.salaryAdjusted"),
                                        NotifKey.Build(typeKey, new[] { reviewerUser?.FullName ?? "", request.Amount.Value.ToString("F0"), request.Description }, "/salary"),
                                        NotificationType.SystemNotification, ct);
                            }
                            break;
                    }
                    if (userChanged)
                        await _userManager.UpdateAsync(targetUser);
                }
            }
            else
            {
                request.Reject(_currentUser.UserId, dto.AdminNote);
            }

            await _uow.Write<HRActionRequest>().UpdateAsync(request, ct);
            await _uow.SaveChangesAsync();

            // Notify HR
            var adminUser = await _uow.Read<ApplicationUser>().GetSingleAsync(u => u.Id == _currentUser.UserId, ct);
            var adminName = adminUser?.FullName ?? "";
            var statusKey = dto.Approved ? "notif.msg.hrActionApproved" : "notif.msg.hrActionRejected";

            await _notifications.CreateAsync(
                Guid.Parse(request.RequestedByUserId), _currentUser.TenantId,
                NotifKey.Build("notif.title.hrActionReviewed"),
                NotifKey.Build(statusKey, new[] { adminName, request.ActionType.ToString(), dto.AdminNote ?? "" }, "/hr"),
                NotificationType.SystemNotification, ct);

            // Notify target user if approved
            if (dto.Approved && !string.IsNullOrEmpty(request.TargetUserId))
            {
                var isJoinRequest = request.ActionType == HRActionType.AcceptJoinRequest;
                await _notifications.CreateAsync(
                    Guid.Parse(request.TargetUserId), _currentUser.TenantId,
                    NotifKey.Build(isJoinRequest ? "notif.title.welcomeApproved" : "notif.title.hrActionExecuted"),
                    NotifKey.Build(isJoinRequest ? "notif.msg.welcomeApproved" : "notif.msg.hrActionExecuted",
                        isJoinRequest ? Array.Empty<string>() : new[] { request.ActionType.ToString() }, isJoinRequest ? "/dashboard" : "/"),
                    NotificationType.SystemNotification, ct);
            }

            return ApiResponse.SuccessResponse(message: dto.Approved ? _localizer["HRActionApproved"] : _localizer["HRActionRejected"]);
        }

        public async Task<ApiResponse> DeleteActionAsync(Guid id, CancellationToken ct = default)
        {
            if (_currentUser.Role != UserRole.HR)
                return ApiResponse.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);

            var request = await _uow.Read<HRActionRequest>().GetByIdAsync(id, ct);
            if (request == null || request.TenantId != _currentUser.TenantId)
                return ApiResponse.FailureResponse(_localizer["NotFound"], HttpStatusCode.NotFound);

            if (request.Status == HRActionStatus.Pending)
                return ApiResponse.FailureResponse(_localizer["CannotDeletePendingAction"], HttpStatusCode.BadRequest);

            if (request.RequestedByUserId != _currentUser.UserId)
                return ApiResponse.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);

            await _uow.Write<HRActionRequest>().DeleteAsync(request, ct);
            await _uow.SaveChangesAsync();
            return ApiResponse.SuccessResponse();
        }

        public async Task<ApiResponseT<List<HRActionResponseDto>>> GetActionRequestsAsync(CancellationToken ct = default)
        {
            var role = _currentUser.Role;
            var userId = _currentUser.UserId;
            var tenantId = _currentUser.TenantId;

            if (role != UserRole.Admin && role != UserRole.HR)
                return ApiResponseT<List<HRActionResponseDto>>.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);

            List<HRActionRequest> requests;
            if (role == UserRole.Admin)
            {
                requests = await _uow.Read<HRActionRequest>().ListAsync(r => r.TenantId == tenantId, ct);
            }
            else
            {
                requests = await _uow.Read<HRActionRequest>().ListAsync(r => r.TenantId == tenantId && r.RequestedByUserId == userId, ct);
            }

            var allUserIds = requests.SelectMany(r => new[] { r.RequestedByUserId, r.TargetUserId, r.ReviewedByAdminId ?? "", r.TargetTeamLeadId ?? "" })
                .Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            var users = await _uow.Read<ApplicationUser>().ListAsync(u => allUserIds.Contains(u.Id), ct);
            var userMap = users.ToDictionary(u => u.Id, u => u.FullName ?? u.UserName ?? "");

            var result = requests.Select(r => new HRActionResponseDto
            {
                Id = r.Id,
                RequestedByUserId = r.RequestedByUserId,
                RequestedByUserName = userMap.GetValueOrDefault(r.RequestedByUserId, ""),
                TargetUserId = r.TargetUserId,
                TargetUserName = userMap.GetValueOrDefault(r.TargetUserId, ""),
                TargetTeamLeadId = r.TargetTeamLeadId,
                TargetTeamLeadName = r.TargetTeamLeadId != null ? userMap.GetValueOrDefault(r.TargetTeamLeadId, "") : null,
                ActionType = r.ActionType,
                Description = r.Description,
                Status = r.Status,
                ReviewedByAdminName = r.ReviewedByAdminId != null ? userMap.GetValueOrDefault(r.ReviewedByAdminId, "") : null,
                AdminNote = r.AdminNote,
                ReviewedAt = r.ReviewedAt,
                CreatedAt = r.CreatedAt,
                Amount = r.Amount,
                JoinRequestId = r.JoinRequestId,
            }).OrderByDescending(r => r.CreatedAt).ToList();

            return ApiResponseT<List<HRActionResponseDto>>.SuccessResponse(result);
        }
    }
}
