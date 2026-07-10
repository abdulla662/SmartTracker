using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Team;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.Extensions.Localization;

namespace DealTrack.Application.Services
{
    public class TeamService : ITeamService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> localizer;
        public TeamService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> _Localizer)
        {
            _uow = uow;
            _currentUser = currentUser;
            localizer = _Localizer;
        }

        public async Task<ApiResponseT<TeamsResponseDto>> GetAllTeamsAsync(CancellationToken ct)
        {
            var tenantId = _currentUser.TenantId;

            var teamLeads = await _uow.Read<ApplicationUser>()
                .ListAsync(u => u.TenantId == tenantId && u.Role == UserRole.TeamLead, ct);

            var allSales = await _uow.Read<ApplicationUser>()
                .ListAsync(u => u.TenantId == tenantId && u.Role == UserRole.Sales, ct);

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

            var result = new TeamsResponseDto
            {
                TeamLeads = teamLeadDtos,
                IndividualSales = individualSales
            };

            return ApiResponseT<TeamsResponseDto>.SuccessResponse(result);
        }
        public async Task<ApiResponseT<bool>> RemoveMemberAsync(string userId, CancellationToken ct)
        {
            var tenantId = _currentUser.TenantId;
            var user = await _uow.Read<ApplicationUser>().GetByIdAsync(userId, ct);
            if (user is null || user.TenantId != tenantId)
                return ApiResponseT<bool>.FailureResponse(localizer["Member not found."]);
            user.TeamLeadId = null;
            await _uow.Write<ApplicationUser>().UpdateAsync(user, ct);
            await _uow.SaveChangesAsync();
            return ApiResponseT<bool>.SuccessResponse(true);
        }

        public async Task<ApiResponseT<List<TeamMemberDto>>> GetMyTeamAsync(CancellationToken ct)
        {
            var currentUserId = Guid.Parse(_currentUser.UserId);
            var tenantId = _currentUser.TenantId;

            var members = await _uow.Read<ApplicationUser>()
                .ListAsync(u => u.TenantId == tenantId && u.TeamLeadId == currentUserId, ct);

            var dtos = members.Select(u => new TeamMemberDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty
            }).ToList();

            return ApiResponseT<List<TeamMemberDto>>.SuccessResponse(dtos);
        }
    }
}
