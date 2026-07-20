using DealTrack.Application.Common;
using DealTrack.Application.DTOs.Tenant;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TenantController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public TenantController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpGet]
        public async Task<ApiResponseT<TenantInfoDto>> GetTenantInfo(CancellationToken ct)
            => await _tenantService.GetTenantInfoAsync(ct);

        [HttpPut]
        [Authorize(Roles = "Admin")]
        public async Task<ApiResponse> UpdateTenant([FromBody] UpdateTenantDto dto, CancellationToken ct)
            => await _tenantService.UpdateTenantAsync(dto, ct);
    }
}
