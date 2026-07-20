using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Tenant;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace DealTrack.Infrastructure.Services
{
    public class TenantService : ITenantService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public TenantService(IUnitOfWork uow, ICurrentUserService currentUser, IStringLocalizer<SharedResource> localizer)
        {
            _uow = uow;
            _currentUser = currentUser;
            _localizer = localizer;
        }

        public async Task<ApiResponseT<TenantInfoDto>> GetTenantInfoAsync(CancellationToken ct = default)
        {
            var tenant = await _uow.Read<Tenant>().GetSingleAsync(t => t.Id == _currentUser.TenantId, ct);
            if (tenant == null)
                return ApiResponseT<TenantInfoDto>.FailureResponse(_localizer["UserNotFound"]);

            return ApiResponseT<TenantInfoDto>.SuccessResponse(new TenantInfoDto
            {
                Name      = tenant.Name,
                Industry  = tenant.Industry,
                Address   = tenant.Address,
                Website   = tenant.Website,
                Currency  = tenant.Currency,
                Timezone  = tenant.Timezone,
                IsPersonal = tenant.IsPersonal,
                Plan      = tenant.Plan.ToString(),
            });
        }

        public async Task<ApiResponse> UpdateTenantAsync(UpdateTenantDto dto, CancellationToken ct = default)
        {
            var tenant = await _uow.Read<Tenant>().GetSingleAsync(t => t.Id == _currentUser.TenantId, ct);
            if (tenant == null)
                return ApiResponse.FailureResponse(_localizer["UserNotFound"]);

            if (tenant.IsPersonal)
                return ApiResponse.FailureResponse(_localizer["CannotUpdatePersonalTenant"]);

            tenant.UpdateInfo(dto.Name, dto.Industry, dto.Address, dto.Website, dto.Currency, dto.Timezone);
            await _uow.Write<Tenant>().UpdateAsync(tenant, ct);
            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["TenantUpdated"]);
        }
    }
}
