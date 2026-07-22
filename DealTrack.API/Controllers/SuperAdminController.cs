using DealTrack.Application.Common;
using DealTrack.Application.Interfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/super-admin")]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public SuperAdminController(IUnitOfWork uow, UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _uow = uow;
            _userManager = userManager;
            _config = config;
        }

        // ── List all tenants ────────────────────────────────────────────────
        [HttpGet("tenants")]
        public async Task<ApiResponseT<List<TenantDto>>> GetTenants(CancellationToken ct)
        {
            var tenants = await _uow.Read<Tenant>().ListAsync(
                t => t.Name != "__DealTrack_System__", ct);
            var allUsers = _userManager.Users.ToList();

            var result = tenants.Select(t => MapTenant(t, allUsers))
                                .OrderByDescending(t => t.CreatedAt).ToList();

            return ApiResponseT<List<TenantDto>>.SuccessResponse(result);
        }

        // ── Single tenant ───────────────────────────────────────────────────
        [HttpGet("tenants/{id:guid}")]
        public async Task<ApiResponseT<TenantDto>> GetTenantById(Guid id, CancellationToken ct)
        {
            var tenant = await _uow.Read<Tenant>().GetSingleAsync(t => t.Id == id, ct);
            if (tenant == null)
                return ApiResponseT<TenantDto>.FailureResponse("Tenant not found", System.Net.HttpStatusCode.NotFound);

            var users = _userManager.Users.Where(u => u.TenantId == id).ToList();
            return ApiResponseT<TenantDto>.SuccessResponse(MapTenant(tenant, users));
        }

        // ── Stats ───────────────────────────────────────────────────────────
        [HttpGet("stats")]
        public async Task<ApiResponseT<StatsDto>> GetStats(CancellationToken ct)
        {
            var tenants = await _uow.Read<Tenant>().ListAsync(
                t => t.Name != "__DealTrack_System__", ct);
            var allUsers = _userManager.Users.ToList();

            return ApiResponseT<StatsDto>.SuccessResponse(new StatsDto
            {
                TotalTenants  = tenants.Count,
                ActiveTenants = tenants.Count(t => !t.IsBlocked),
                TotalUsers    = allUsers.Count(u => u.IsApproved && u.Role != UserRole.SuperAdmin),
                TotalRevenue  = 0,
            });
        }

        // ── Impersonate (enter as tenant Admin) ─────────────────────────────
        [HttpPost("impersonate/{tenantId:guid}")]
        public async Task<ApiResponseT<ImpersonateResponseDto>> Impersonate(Guid tenantId, CancellationToken ct)
        {
            var admin = _userManager.Users
                .FirstOrDefault(u => u.TenantId == tenantId && u.Role == UserRole.Admin && u.IsApproved);

            if (admin == null)
                return ApiResponseT<ImpersonateResponseDto>.FailureResponse(
                    "No active Admin found for this tenant.", System.Net.HttpStatusCode.NotFound);

            return ApiResponseT<ImpersonateResponseDto>.SuccessResponse(
                new ImpersonateResponseDto { AccessToken = GenerateJwt(admin) });
        }

        // ── Impersonate any specific user ────────────────────────────────────
        [HttpPost("impersonate-user/{userId}")]
        public async Task<ApiResponseT<ImpersonateResponseDto>> ImpersonateUser(string userId, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Role == UserRole.SuperAdmin)
                return ApiResponseT<ImpersonateResponseDto>.FailureResponse(
                    "User not found.", System.Net.HttpStatusCode.NotFound);

            return ApiResponseT<ImpersonateResponseDto>.SuccessResponse(
                new ImpersonateResponseDto { AccessToken = GenerateJwt(user) });
        }

        // ── Block tenant (cascade: lock all users) ───────────────────────────
        [HttpPost("tenants/{id:guid}/block")]
        public async Task<ApiResponse> BlockTenant(Guid id, [FromBody] BlockTenantDto dto, CancellationToken ct)
        {
            var tenant = await _uow.Read<Tenant>().GetSingleAsync(t => t.Id == id, ct);
            if (tenant == null)
                return ApiResponse.FailureResponse("Tenant not found", System.Net.HttpStatusCode.NotFound);

            tenant.Block(dto.BlockForever ? null : dto.BlockedUntil, dto.Reason);
            await _uow.Write<Tenant>().UpdateAsync(tenant, ct);

            // Cascade: lock all users in the org
            var lockUntil = dto.BlockForever ? DateTimeOffset.MaxValue
                : (dto.BlockedUntil.HasValue ? new DateTimeOffset(dto.BlockedUntil.Value) : DateTimeOffset.MaxValue);
            var users = _userManager.Users.Where(u => u.TenantId == id).ToList();
            foreach (var u in users)
            {
                u.LockoutEnabled = true;
                u.LockoutEnd = lockUntil;
                await _userManager.UpdateAsync(u);
            }

            await _uow.SaveChangesAsync();
            return ApiResponse.SuccessResponse(message: "Organization blocked successfully.");
        }

        // ── Unblock tenant (cascade: unlock all users) ───────────────────────
        [HttpPost("tenants/{id:guid}/unblock")]
        public async Task<ApiResponse> UnblockTenant(Guid id, CancellationToken ct)
        {
            var tenant = await _uow.Read<Tenant>().GetSingleAsync(t => t.Id == id, ct);
            if (tenant == null)
                return ApiResponse.FailureResponse("Tenant not found", System.Net.HttpStatusCode.NotFound);

            tenant.Unblock();
            await _uow.Write<Tenant>().UpdateAsync(tenant, ct);

            // Cascade: unlock all users in the org
            var users = _userManager.Users.Where(u => u.TenantId == id).ToList();
            foreach (var u in users)
            {
                u.LockoutEnabled = false;
                u.LockoutEnd = null;
                await _userManager.UpdateAsync(u);
            }

            await _uow.SaveChangesAsync();
            return ApiResponse.SuccessResponse(message: "Organization unblocked successfully.");
        }

        // ── Delete tenant + all users (cascade) ──────────────────────────────
        [HttpDelete("tenants/{id:guid}")]
        public async Task<ApiResponse> DeleteTenant(Guid id, CancellationToken ct)
        {
            var tenant = await _uow.Read<Tenant>().GetSingleAsync(t => t.Id == id, ct);
            if (tenant == null)
                return ApiResponse.FailureResponse("Tenant not found", System.Net.HttpStatusCode.NotFound);

            var users = _userManager.Users.Where(u => u.TenantId == id).ToList();
            foreach (var user in users)
                await _userManager.DeleteAsync(user);

            await _uow.Write<Tenant>().DeleteAsync(tenant, ct);
            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: "Organization and all users deleted successfully.");
        }

        // ── List users of a tenant ───────────────────────────────────────────
        [HttpGet("tenants/{id:guid}/users")]
        public async Task<ApiResponseT<List<TenantUserDto>>> GetTenantUsers(Guid id, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            var users = _userManager.Users
                .Where(u => u.TenantId == id && u.Role != UserRole.SuperAdmin)
                .ToList()
                .Select(u => new TenantUserDto
                {
                    Id        = u.Id,
                    FullName  = u.FullName,
                    Email     = u.Email ?? "",
                    Role      = u.Role.ToString(),
                    IsBlocked = u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd > now,
                })
                .ToList();

            return ApiResponseT<List<TenantUserDto>>.SuccessResponse(users);
        }

        // ── Block a specific user ────────────────────────────────────────────
        [HttpPost("users/{userId}/block")]
        public async Task<ApiResponse> BlockUser(string userId, [FromBody] BlockUserDto dto, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Role == UserRole.SuperAdmin)
                return ApiResponse.FailureResponse("User not found.", System.Net.HttpStatusCode.NotFound);

            var lockUntil = dto.BlockForever ? DateTimeOffset.MaxValue
                : (dto.BlockedUntil.HasValue ? new DateTimeOffset(dto.BlockedUntil.Value) : DateTimeOffset.MaxValue);

            user.LockoutEnabled = true;
            user.LockoutEnd = lockUntil;
            await _userManager.UpdateAsync(user);

            // If blocking the Admin, cascade to the entire org
            if (user.Role == UserRole.Admin)
            {
                var tenant = await _uow.Read<Tenant>().GetSingleAsync(t => t.Id == user.TenantId, ct);
                if (tenant != null)
                {
                    tenant.Block(dto.BlockForever ? null : dto.BlockedUntil, dto.Reason);
                    await _uow.Write<Tenant>().UpdateAsync(tenant, ct);

                    var orgUsers = _userManager.Users.Where(u => u.TenantId == user.TenantId && u.Id != userId).ToList();
                    foreach (var u in orgUsers)
                    {
                        u.LockoutEnabled = true;
                        u.LockoutEnd = lockUntil;
                        await _userManager.UpdateAsync(u);
                    }
                    await _uow.SaveChangesAsync();
                }
            }

            return ApiResponse.SuccessResponse(message: "User blocked successfully.");
        }

        // ── Unblock a specific user ──────────────────────────────────────────
        [HttpPost("users/{userId}/unblock")]
        public async Task<ApiResponse> UnblockUser(string userId, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Role == UserRole.SuperAdmin)
                return ApiResponse.FailureResponse("User not found.", System.Net.HttpStatusCode.NotFound);

            user.LockoutEnabled = false;
            user.LockoutEnd = null;
            await _userManager.UpdateAsync(user);

            return ApiResponse.SuccessResponse(message: "User unblocked successfully.");
        }

        // ── Delete a specific user (cascade if Admin) ────────────────────────
        [HttpDelete("users/{userId}")]
        public async Task<ApiResponse> DeleteUser(string userId, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.Role == UserRole.SuperAdmin)
                return ApiResponse.FailureResponse("User not found.", System.Net.HttpStatusCode.NotFound);

            // If deleting the Admin, cascade to the entire org
            if (user.Role == UserRole.Admin)
            {
                var tenantId = user.TenantId;
                var orgUsers = _userManager.Users.Where(u => u.TenantId == tenantId).ToList();
                foreach (var u in orgUsers)
                    await _userManager.DeleteAsync(u);

                var tenant = await _uow.Read<Tenant>().GetSingleAsync(t => t.Id == tenantId, ct);
                if (tenant != null)
                {
                    await _uow.Write<Tenant>().DeleteAsync(tenant, ct);
                    await _uow.SaveChangesAsync();
                }
            }
            else
            {
                await _userManager.DeleteAsync(user);
            }

            return ApiResponse.SuccessResponse(message: "User deleted successfully.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static TenantDto MapTenant(Tenant t, IEnumerable<ApplicationUser> users)
        {
            var list = users.Where(u => u.TenantId == t.Id).ToList();
            string status;
            if (t.IsBlocked)
                status = t.BlockedUntil.HasValue ? "Blocked" : "Suspended";
            else
                status = "Active";

            return new TenantDto
            {
                Id           = t.Id,
                Name         = t.Name,
                Email        = list.FirstOrDefault(u => u.Role == UserRole.Admin)?.Email ?? "",
                Plan         = t.Plan.ToString(),
                Status       = status,
                IsBlocked    = t.IsBlocked,
                BlockedUntil = t.BlockedUntil,
                BlockReason  = t.BlockReason,
                UserCount    = list.Count(u => u.IsApproved),
                CreatedAt    = t.CreatedAt,
            };
        }

        private string GenerateJwt(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("SubscriptionPlan", user.SubscriptionPlan.ToString()),
                new Claim("TenantId", user.TenantId.ToString()),
            };
            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer:            _config["JwtSettings:Issuer"],
                audience:          _config["JwtSettings:Audience"],
                claims:            claims,
                expires:           DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["JwtSettings:ExpireMinutes"])),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ── DTOs ─────────────────────────────────────────────────────────────
        public record TenantDto
        {
            public Guid      Id           { get; init; }
            public string    Name         { get; init; } = "";
            public string    Email        { get; init; } = "";
            public string    Plan         { get; init; } = "";
            public string    Status       { get; init; } = "";
            public bool      IsBlocked    { get; init; }
            public DateTime? BlockedUntil { get; init; }
            public string?   BlockReason  { get; init; }
            public int       UserCount    { get; init; }
            public DateTime  CreatedAt    { get; init; }
        }

        public record ImpersonateResponseDto  { public string AccessToken { get; init; } = ""; }

        public record BlockTenantDto
        {
            public bool      BlockForever { get; init; }
            public DateTime? BlockedUntil { get; init; }
            public string?   Reason       { get; init; }
        }

        public record BlockUserDto
        {
            public bool      BlockForever { get; init; }
            public DateTime? BlockedUntil { get; init; }
            public string?   Reason       { get; init; }
        }

        public record TenantUserDto
        {
            public string Id        { get; init; } = "";
            public string FullName  { get; init; } = "";
            public string Email     { get; init; } = "";
            public string Role      { get; init; } = "";
            public bool   IsBlocked { get; init; }
        }

        public record StatsDto
        {
            public int     TotalTenants  { get; init; }
            public int     ActiveTenants { get; init; }
            public int     TotalUsers    { get; init; }
            public decimal TotalRevenue  { get; init; }
        }
    }
}
