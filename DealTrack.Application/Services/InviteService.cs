using AutoMapper;
using DealTrack.Application.Common;
using DealTrack.Application.DTOs.TenantInvite;
using DealTrack.Application.Helpers;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Constants;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System.Net;

namespace DealTrack.Application.Services
{
    public class InviteService : IInviteService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _uow;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ICurrentUserService _currentUser;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly INotificationService _notifications;

        public InviteService(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork uow,
            IStringLocalizer<SharedResource> localizer,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            IMapper mapper,
            INotificationService notifications)
        {
            _userManager = userManager;
            _uow = uow;
            _localizer = localizer;
            _currentUser = currentUserService;
            _emailService = emailService;
            _mapper = mapper;
            _notifications = notifications;
        }
 

        public async Task<ApiResponseT<AcceptInviteDto>> AcceptInviteAsync(AcceptInviteDto dto, CancellationToken ct = default)
        {
            var invite = await _uow.Read<TenantInvite>()
                .GetSingleAsync(i => i.InviteCode == dto.InviteCode, ct);

            if (invite == null)
                return ApiResponseT<AcceptInviteDto>.FailureResponse(_localizer["InvalidInviteCode"], HttpStatusCode.NotFound);

            if (invite.ExpiresAt < DateTime.UtcNow)
                return ApiResponseT<AcceptInviteDto>.FailureResponse(_localizer["InviteExpired"], HttpStatusCode.BadRequest);

            if (invite.IsUsed)
                return ApiResponseT<AcceptInviteDto>.FailureResponse(_localizer["InviteAlreadyUsed"], HttpStatusCode.BadRequest);

            var user = new ApplicationUser
            {
                FullName = dto.FullName,
                Email = invite.Email,
                UserName = invite.Email,
                TenantId = invite.TenantId,
                Role = invite.InvitedRole != UserRole.Sales ? invite.InvitedRole : UserRole.Sales,
                SubscriptionPlan = SubscriptionPlan.Free,
                TeamLeadId = invite.TeamLeadId  // null = individual, Guid = under a TeamLead
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return ApiResponseT<AcceptInviteDto>.FailureResponse(
                    result.Errors.First().Description, HttpStatusCode.BadRequest);

            invite.MarkUsed();
            await _uow.Write<TenantInvite>().UpdateAsync(invite, ct);
            await _uow.SaveChangesAsync();

            if (invite.TeamLeadId.HasValue)
            {
                await _notifications.CreateAsync(
                    invite.TeamLeadId.Value, invite.TenantId,
                    NotifKey.Build("notif.title.newMemberJoined"),
                    NotifKey.Build("notif.msg.newMemberJoined", dto.FullName),
                    NotificationType.NewMemberJoined, ct);
            }

            return ApiResponseT<AcceptInviteDto>.SuccessResponse(dto, _localizer["InviteAccepted"]);
        }


        public async Task<ApiResponseT<GetInviteDto>> CreateInviteAsync(CreateInviteDto dto, CancellationToken ct = default)
        {
            if (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.TeamLead)
                return ApiResponseT<GetInviteDto>.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Unauthorized);

            // Only Admin can invite Accountant or HR
            if ((dto.InvitedRole == UserRole.Accountant || dto.InvitedRole == UserRole.HR) && _currentUser.Role != UserRole.Admin)
                return ApiResponseT<GetInviteDto>.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Forbidden);

            var existingUser = await _uow.Read<ApplicationUser>().ListAsync(u => u.Email == dto.Email && u.TenantId == _currentUser.TenantId, ct);
            if (existingUser.Any())
                return ApiResponseT<GetInviteDto>.FailureResponse(_localizer["UserAlreadyInCompany"]);

            var existingInvite = await _uow.Read<TenantInvite>().ListAsync(i => i.Email == dto.Email && i.TenantId == _currentUser.TenantId && !i.IsUsed, ct);
            if (existingInvite.Any())
                return ApiResponseT<GetInviteDto>.FailureResponse(_localizer["InviteAlreadySent"], HttpStatusCode.Conflict);

            // if TeamLead is sending invite, automatically assign to himself
            var teamLeadId = _currentUser.Role == UserRole.TeamLead
                ? Guid.Parse(_currentUser.UserId)
                : dto.TeamLeadId;

            var tenant = await _uow.Read<Tenant>().GetByIdAsync(_currentUser.TenantId, ct);

            var invite = new TenantInvite(_currentUser.TenantId, dto.Email, teamLeadId, dto.InvitedRole, _currentUser.UserId);
            await _uow.Write<TenantInvite>().AddAsync(invite, ct);
            await _uow.SaveChangesAsync();

            try { await _emailService.SendInviteEmailAsync(dto.Email, tenant!.Name, invite.InviteCode); }
            catch { /* email delivery failed (e.g. quota) — invite still valid via code */ }

            return ApiResponseT<GetInviteDto>.SuccessResponse(
                _mapper.Map<GetInviteDto>(invite),
                _localizer["InviteCreated"],
                HttpStatusCode.Created);
        }

        public async Task<ApiResponseT<List<GetInviteDto>>> GetAllInvites(CancellationToken ct = default)
        {
            if (_currentUser.Role == UserRole.Admin)
            {
                var invites = await _uow.Read<TenantInvite>().ListAsync(i => i.TenantId == _currentUser.TenantId, ct);
                return ApiResponseT<List<GetInviteDto>>.SuccessResponse(_mapper.Map<List<GetInviteDto>>(invites), _localizer["InvitesRetrieved"]);
            }

            if (_currentUser.Role == UserRole.TeamLead)
            {
                var myId = Guid.Parse(_currentUser.UserId);
                var invites = await _uow.Read<TenantInvite>().ListAsync(i => i.TenantId == _currentUser.TenantId && i.TeamLeadId == myId, ct);
                return ApiResponseT<List<GetInviteDto>>.SuccessResponse(_mapper.Map<List<GetInviteDto>>(invites), _localizer["InvitesRetrieved"]);
            }

            return ApiResponseT<List<GetInviteDto>>.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Unauthorized);
        }

        public async Task<ApiResponseT<InviteInfoDto>> GetInviteInfoAsync(string code, CancellationToken ct = default)
        {
            var invite = await _uow.Read<TenantInvite>()
                .GetSingleAsync(i => i.InviteCode == code && !i.IsUsed, ct);

            if (invite == null)
                return ApiResponseT<InviteInfoDto>.FailureResponse(_localizer["InviteNotFound"], HttpStatusCode.NotFound);

            if (invite.ExpiresAt < DateTime.UtcNow)
                return ApiResponseT<InviteInfoDto>.FailureResponse(_localizer["InviteExpired"], HttpStatusCode.BadRequest);

            var inviterName = string.Empty;
            if (invite.InvitedByUserId != null)
            {
                var inviter = await _userManager.FindByIdAsync(invite.InvitedByUserId);
                inviterName = inviter?.FullName ?? string.Empty;
            }

            return ApiResponseT<InviteInfoDto>.SuccessResponse(new InviteInfoDto
            {
                Email = invite.Email,
                InviterName = inviterName,
                InvitedRole = invite.InvitedRole.ToString(),
            });
        }

        public async Task<ApiResponseT<GetInviteDto>> RevokeAndResendAsync(string email, CancellationToken ct = default)
        {
            if (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.TeamLead)
                return ApiResponseT<GetInviteDto>.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Unauthorized);

            var existing = await _uow.Read<TenantInvite>()
                .ListAsync(i => i.Email == email && i.TenantId == _currentUser.TenantId && !i.IsUsed, ct);

            foreach (var old in existing)
            {
                old.MarkUsed(); // soft-revoke by marking used
                await _uow.Write<TenantInvite>().UpdateAsync(old, ct);
            }
            await _uow.SaveChangesAsync();

            // Re-use the existing role from the old invite if available
            var oldRole = existing.FirstOrDefault()?.InvitedRole ?? UserRole.Sales;
            var oldTeamLeadId = existing.FirstOrDefault()?.TeamLeadId;

            var teamLeadId = _currentUser.Role == UserRole.TeamLead
                ? Guid.Parse(_currentUser.UserId)
                : oldTeamLeadId;

            var tenant = await _uow.Read<Tenant>().GetByIdAsync(_currentUser.TenantId, ct);
            var invite = new TenantInvite(_currentUser.TenantId, email, teamLeadId, oldRole, _currentUser.UserId);
            await _uow.Write<TenantInvite>().AddAsync(invite, ct);
            await _uow.SaveChangesAsync();

            try { await _emailService.SendInviteEmailAsync(email, tenant!.Name, invite.InviteCode); }
            catch { /* email delivery failed — invite still valid via code */ }

            return ApiResponseT<GetInviteDto>.SuccessResponse(
                _mapper.Map<GetInviteDto>(invite),
                _localizer["InviteRevoked"],
                HttpStatusCode.Created);
        }
    }
}
