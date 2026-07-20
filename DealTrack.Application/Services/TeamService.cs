using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Team;
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
    public class TeamService : ITeamService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> localizer;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notifications;

        public TeamService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> _Localizer, UserManager<ApplicationUser> userManager, INotificationService notifications)
        {
            _uow = uow;
            _currentUser = currentUser;
            localizer = _Localizer;
            _userManager = userManager;
            _notifications = notifications;
        }

        public async Task<ApiResponseT<TeamsResponseDto>> GetAllTeamsAsync(CancellationToken ct)
        {
            var tenantId = _currentUser.TenantId;

            var teamLeads = await _uow.Read<ApplicationUser>()
                .ListAsync(u => u.TenantId == tenantId && u.Role == UserRole.TeamLead && u.IsApproved, ct);

            var allSales = await _uow.Read<ApplicationUser>()
                .ListAsync(u => u.TenantId == tenantId && u.Role == UserRole.Sales && u.IsApproved, ct);

            // لكل TeamLead جيب الـ Sales تبعيه
            var teamLeadDtos = teamLeads.Select(tl => new TeamLeadWithMembersDto
            {
                Id = tl.Id,
                FullName = tl.FullName,
                Email = tl.Email ?? string.Empty,
                SalesMembers = allSales
                    .Where(s => s.TeamLeadId == Guid.Parse(tl.Id))
                    .Select(s => new TeamMemberDto
                    {
                        Id = s.Id,
                        FullName = s.FullName,
                        Email = s.Email ?? string.Empty
                    }).ToList()
            }).ToList();

            var individualSales = allSales
                .Where(s => s.TeamLeadId == null)
                .Select(s => new TeamMemberDto
                {
                    Id = s.Id,
                    FullName = s.FullName,
                    Email = s.Email ?? string.Empty
                }).ToList();

            var hrUsers = await _uow.Read<ApplicationUser>()
                .ListAsync(u => u.TenantId == tenantId && u.Role == UserRole.HR && u.IsApproved, ct);
            var accountantUsers = await _uow.Read<ApplicationUser>()
                .ListAsync(u => u.TenantId == tenantId && u.Role == UserRole.Accountant && u.IsApproved, ct);
            var adminUsers = await _uow.Read<ApplicationUser>()
                .ListAsync(u => u.TenantId == tenantId && u.Role == UserRole.Admin && u.IsApproved, ct);

            var result = new TeamsResponseDto
            {
                TeamLeads = teamLeadDtos,
                IndividualSales = individualSales,
                HrMembers = hrUsers.Select(u => new TeamMemberDto { Id = u.Id, FullName = u.FullName, Email = u.Email ?? "" }).ToList(),
                Accountants = accountantUsers.Select(u => new TeamMemberDto { Id = u.Id, FullName = u.FullName, Email = u.Email ?? "" }).ToList(),
                Admins = adminUsers.Select(u => new TeamMemberDto { Id = u.Id, FullName = u.FullName, Email = u.Email ?? "" }).ToList(),
            };

            return ApiResponseT<TeamsResponseDto>.SuccessResponse(result);
        }
        public async Task<ApiResponseT<bool>> RemoveMemberAsync(string userId, CancellationToken ct)
        {
            var tenantId = _currentUser.TenantId;
            var callerRole = _currentUser.Role;

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || user.TenantId != tenantId || !user.IsApproved)
                return ApiResponseT<bool>.FailureResponse(localizer["Member not found."], HttpStatusCode.NotFound);

            // Only Admin can delete members
            if (callerRole != UserRole.Admin)
                return ApiResponseT<bool>.FailureResponse(localizer["Unauthorized"], HttpStatusCode.Forbidden);

            // Prevent deleting other Admins
            if (user.Role == UserRole.Admin)
                return ApiResponseT<bool>.FailureResponse(localizer["Unauthorized"], HttpStatusCode.Forbidden);

            // Reassign all this member's clients to the caller (Admin/TeamLead) before deleting
            var clients = await _uow.Read<Client>()
                .ListAsync(c => c.AssignedToUserId == userId && c.TenantId == tenantId, ct);

            foreach (var client in clients)
                client.Reassign(_currentUser.UserId);

            if (clients.Count > 0)
            {
                foreach (var client in clients)
                    await _uow.Write<Client>().UpdateAsync(client, ct);
                await _uow.SaveChangesAsync();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return ApiResponseT<bool>.FailureResponse(result.Errors.First().Description, HttpStatusCode.BadRequest);

            return ApiResponseT<bool>.SuccessResponse(true);
        }

        public async Task<ApiResponseT<List<TeamMemberDto>>> GetMyTeamAsync(CancellationToken ct)
        {
            var currentUserId = Guid.Parse(_currentUser.UserId);
            var tenantId = _currentUser.TenantId;

            var members = await _uow.Read<ApplicationUser>()
                .ListAsync(u => u.TenantId == tenantId && u.TeamLeadId == currentUserId && u.IsApproved, ct);

            var dtos = members.Select(u => new TeamMemberDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty
            }).ToList();

            return ApiResponseT<List<TeamMemberDto>>.SuccessResponse(dtos);
        }

        public async Task<ApiResponseT<bool>> AssignToTeamLeadAsync(string userId, string teamLeadId, CancellationToken ct)
        {
            var tenantId = _currentUser.TenantId;

            var member = await _userManager.FindByIdAsync(userId);
            if (member == null || member.TenantId != tenantId || !member.IsApproved || member.Role != UserRole.Sales)
                return ApiResponseT<bool>.FailureResponse(localizer["Member not found."], HttpStatusCode.NotFound);

            var teamLead = await _userManager.FindByIdAsync(teamLeadId);
            if (teamLead == null || teamLead.TenantId != tenantId || !teamLead.IsApproved || teamLead.Role != UserRole.TeamLead)
                return ApiResponseT<bool>.FailureResponse(localizer["TeamLead not found."], HttpStatusCode.NotFound);

            member.TeamLeadId = Guid.Parse(teamLeadId);
            var result = await _userManager.UpdateAsync(member);
            if (!result.Succeeded)
                return ApiResponseT<bool>.FailureResponse(result.Errors.First().Description, HttpStatusCode.BadRequest);

            await _notifications.CreateAsync(
                Guid.Parse(teamLeadId),
                tenantId,
                NotifKey.Build("notif.title.newMember"),
                NotifKey.Build("notif.msg.newTeamMember", member.FullName),
                NotificationType.NewMemberJoined,
                ct);

            return ApiResponseT<bool>.SuccessResponse(true);
        }

        public async Task<ApiResponseT<List<TeamMemberDto>>> GetOtherTeamLeadsAsync(CancellationToken ct)
        {
            var tenantId = _currentUser.TenantId;
            var currentUserId = _currentUser.UserId;

            var leads = await _uow.Read<ApplicationUser>()
                .ListAsync(u => u.TenantId == tenantId && u.Role == UserRole.TeamLead && u.IsApproved && u.Id != currentUserId, ct);

            return ApiResponseT<List<TeamMemberDto>>.SuccessResponse(
                leads.Select(u => new TeamMemberDto { Id = u.Id, FullName = u.FullName, Email = u.Email ?? "" }).ToList());
        }

        public async Task<ApiResponseT<bool>> UnassignFromTeamLeadAsync(string userId, CancellationToken ct)
        {
            var tenantId = _currentUser.TenantId;

            var member = await _userManager.FindByIdAsync(userId);
            if (member == null || member.TenantId != tenantId || !member.IsApproved || member.Role != UserRole.Sales)
                return ApiResponseT<bool>.FailureResponse(localizer["Member not found."], HttpStatusCode.NotFound);

            member.TeamLeadId = null;
            var result = await _userManager.UpdateAsync(member);
            if (!result.Succeeded)
                return ApiResponseT<bool>.FailureResponse(result.Errors.First().Description, HttpStatusCode.BadRequest);

            return ApiResponseT<bool>.SuccessResponse(true);
        }

        public async Task<ApiResponseT<MemberProfileDto>> GetMemberProfileAsync(string userId, CancellationToken ct)
        {
            var tenantId = _currentUser.TenantId;
            var role = _currentUser.Role;

            var member = await _uow.Read<ApplicationUser>().GetByIdAsync(userId, ct);
            if (member == null || member.TenantId != tenantId || !member.IsApproved)
                return ApiResponseT<MemberProfileDto>.FailureResponse(localizer["Member not found."], HttpStatusCode.NotFound);

            // Admin can view anyone; TeamLead can only view their own Sales
            if (role == UserRole.TeamLead)
            {
                var currentUserId = Guid.Parse(_currentUser.UserId);
                if (member.TeamLeadId != currentUserId)
                    return ApiResponseT<MemberProfileDto>.FailureResponse(localizer["Unauthorized"], HttpStatusCode.Forbidden);
            }
            else if (role != UserRole.Admin)
            {
                return ApiResponseT<MemberProfileDto>.FailureResponse(localizer["Unauthorized"], HttpStatusCode.Forbidden);
            }

            var clients = await _uow.Read<Client>()
                .ListAsync(c => c.AssignedToUserId == userId && c.TenantId == tenantId, ct);

            var clientIds = clients.Select(c => c.Id).ToHashSet();

            var followUps = await _uow.Read<FollowUp>()
                .ListAsync(f => f.TenantId == tenantId && clientIds.Contains(f.ClientId), ct);

            var payments = await _uow.Read<Payment>()
                .ListAsync(p => p.TenantId == tenantId && clientIds.Contains(p.ClientId), ct);

            var profile = new MemberProfileDto
            {
                UserId = member.Id,
                FullName = member.FullName,
                Email = member.Email ?? string.Empty,
                Role = member.Role.ToString(),
                TotalClients = clients.Count,
                PendingFollowUps = followUps.Count(f => f.Status == FollowUpStatus.Pending),
                DoneFollowUps = followUps.Count(f => f.Status == FollowUpStatus.Done),
                MissedFollowUps = followUps.Count(f => f.Status == FollowUpStatus.Missed),
                TotalPayments = payments.Sum(p => p.Amount),
                PaymentCount = payments.Count,
                LastActivityAt = payments.Count > 0 || followUps.Count > 0
                    ? new[] {
                        payments.Select(p => p.CreatedAt).DefaultIfEmpty(DateTime.MinValue).Max(),
                        followUps.Select(f => f.CreatedAt).DefaultIfEmpty(DateTime.MinValue).Max()
                      }.Max()
                    : null
            };

            return ApiResponseT<MemberProfileDto>.SuccessResponse(profile);
        }
    }
}
