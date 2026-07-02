using AutoMapper;
using DealTrack.Application.Common;
using DealTrack.Application.DTOs.TenantInvite;
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

        public InviteService(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork uow,
            IStringLocalizer<SharedResource> localizer,
            ICurrentUserService currentUserService,
            IEmailService emailService,
            IMapper mapper)
        {
            _userManager = userManager;
            _uow = uow;
            _localizer = localizer;
            _currentUser = currentUserService;
            _emailService = emailService;
            _mapper = mapper;
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
                Role = UserRole.Sales,
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

            return ApiResponseT<AcceptInviteDto>.SuccessResponse(dto, _localizer["InviteAccepted"]);
        }


        public async Task<ApiResponseT<GetInviteDto>> CreateInviteAsync(CreateInviteDto dto, CancellationToken ct = default)
        {
            if (_currentUser.Role != UserRole.Admin && _currentUser.Role != UserRole.TeamLead)
                return ApiResponseT<GetInviteDto>.FailureResponse(_localizer["Unauthorized"], HttpStatusCode.Unauthorized);

            var existingUser = await _uow.Read<ApplicationUser>().ListAsync(u => u.Email == dto.Email && u.TenantId == _currentUser.TenantId, ct);
            if (existingUser.Any())
                return ApiResponseT<GetInviteDto>.FailureResponse(_localizer["UserAlreadyInCompany"]);

            var existingInvite = await _uow.Read<TenantInvite>().ListAsync(i => i.Email == dto.Email && i.TenantId == _currentUser.TenantId && !i.IsUsed, ct);
            if (existingInvite.Any())
                return ApiResponseT<GetInviteDto>.FailureResponse(_localizer["InviteAlreadySent"]);

            // Plan limits check
            if (_currentUser.Role == UserRole.Admin)
            {
                if (dto.TeamLeadId == null)
                {
                    // inviting a direct Sales
                    var maxDirectSales = PlanLimits.GetMaxDirectSales(_currentUser.SubscriptionPlan);
                    if (maxDirectSales != int.MaxValue)
                    {
                        var directSalesCount = await _uow.Read<ApplicationUser>()
                            .CountAsync(u => u.TenantId == _currentUser.TenantId
                                && u.Role == UserRole.Sales
                                && u.TeamLeadId == null, ct);

                        if (directSalesCount >= maxDirectSales)
                            return ApiResponseT<GetInviteDto>.FailureResponse(
                                _localizer["PlanLimitReached"], HttpStatusCode.Forbidden);
                    }
                }
                else
                {
                    // inviting a TeamLead
                    var maxTeamLeads = PlanLimits.GetMaxTeamLeads(_currentUser.SubscriptionPlan);
                    if (maxTeamLeads != int.MaxValue)
                    {
                        var teamLeadsCount = await _uow.Read<ApplicationUser>()
                            .CountAsync(u => u.TenantId == _currentUser.TenantId
                                && u.Role == UserRole.TeamLead, ct);

                        if (teamLeadsCount >= maxTeamLeads)
                            return ApiResponseT<GetInviteDto>.FailureResponse(
                                _localizer["PlanLimitReached"], HttpStatusCode.Forbidden);
                    }
                }
            }

            if (_currentUser.Role == UserRole.TeamLead)
            {
                var maxSales = PlanLimits.GetMaxSalesUnderTeamLead(_currentUser.SubscriptionPlan);
                if (maxSales != int.MaxValue)
                {
                    var salesCount = await _uow.Read<ApplicationUser>()
                        .CountAsync(u => u.TeamLeadId == Guid.Parse(_currentUser.UserId), ct);

                    if (salesCount >= maxSales)
                        return ApiResponseT<GetInviteDto>.FailureResponse(
                            _localizer["PlanLimitReached"], HttpStatusCode.Forbidden);
                }
            }

            // if TeamLead is sending invite, automatically assign to himself
            var teamLeadId = _currentUser.Role == UserRole.TeamLead
                ? Guid.Parse(_currentUser.UserId)
                : dto.TeamLeadId;

            var tenant = await _uow.Read<Tenant>().GetByIdAsync(_currentUser.TenantId, ct);

            var invite = new TenantInvite(_currentUser.TenantId, dto.Email, teamLeadId);
            await _uow.Write<TenantInvite>().AddAsync(invite, ct);
            await _uow.SaveChangesAsync();

            await _emailService.SendInviteEmailAsync(dto.Email, tenant!.Name, invite.InviteCode);

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
    }
}
