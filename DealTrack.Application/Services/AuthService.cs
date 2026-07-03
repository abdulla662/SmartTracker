using DealTrack.Application.Common;
using DealTrack.Application.DTOs;
using DealTrack.Application.DTOs.Auth;
using DealTrack.Application.Interfaces;
using DealTrack.Application.Resources;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace DealTrack.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _uow;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ICurrentUserService _currentUser;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IUnitOfWork uow,
            IStringLocalizer<SharedResource> localizer,
            ICurrentUserService currentUser)
        {
            _userManager = userManager;
            _configuration = configuration;
            _uow = uow;
            _localizer = localizer;
            _currentUser = currentUser;
        }

        public async Task<ApiResponse> RegisterAsync(RegisterDto request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return ApiResponse.FailureResponse(_localizer["EmailAlreadyExists"]);

            if (!Enum.IsDefined(typeof(SubscriptionPlan), request.SubscriptionPlan))
                return ApiResponse.FailureResponse(_localizer["InvalidSubscriptionPlan"]);

            if (string.IsNullOrWhiteSpace(request.CompanyName))
                return ApiResponse.FailureResponse(_localizer["CompanyNameRequired"]);

            UserRole role = request.SubscriptionPlan switch
            {
                SubscriptionPlan.Free => UserRole.Sales,
                SubscriptionPlan.Pro => UserRole.TeamLead,
                SubscriptionPlan.Enterprise => UserRole.Admin,
                _ => throw new InvalidOperationException(_localizer["InvalidSubscriptionPlan"])
            };

            var tenant = new Tenant(request.CompanyName, request.SubscriptionPlan);
            await _uow.Write<Tenant>().AddAsync(tenant);

            var user = new ApplicationUser
            {
                FullName = request.FullName,
                Email = request.Email,
                UserName = request.Email,
                Role = role,
                SubscriptionPlan = request.SubscriptionPlan,
                TenantId = tenant.Id
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return ApiResponse.FailureResponse(result.Errors.First().Description);

            await _uow.SaveChangesAsync();

            return ApiResponse.SuccessResponse(message: _localizer["RegisterSuccess"]);
        }

        public async Task<ApiResponseT<AuthResponseDto>> LoginAsync(LoginDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return ApiResponseT<AuthResponseDto>.FailureResponse(_localizer["InvalidCredentials"]);

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
                return ApiResponseT<AuthResponseDto>.FailureResponse(_localizer["InvalidCredentials"]);

            var accessToken = GenerateJwtToken(user);

            var refreshToken = new RefreshToken(
                user.TenantId,
                user.Id,
                Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                DateTime.UtcNow.AddDays(7));

            await _uow.Write<RefreshToken>().AddAsync(refreshToken);
            await _uow.SaveChangesAsync();

            return ApiResponseT<AuthResponseDto>.SuccessResponse(new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token
            }, _localizer["LoginSuccess"]);
        }

        public async Task<ApiResponseT<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken ct)
        {
            var stored = await _uow.Read<RefreshToken>()
                .GetSingleAsync(r => r.Token == dto.RefreshToken, ct);

            if (stored is null || !stored.IsActive)
                return ApiResponseT<AuthResponseDto>.FailureResponse(
                    _localizer["InvalidRefreshToken"], HttpStatusCode.Unauthorized);

            var user = await _userManager.FindByIdAsync(stored.UserId);
            if (user is null)
                return ApiResponseT<AuthResponseDto>.FailureResponse(
                    _localizer["UserNotFound"], HttpStatusCode.NotFound);

            stored.Revoke();
            await _uow.Write<RefreshToken>().UpdateAsync(stored, ct);

            var newRefreshToken = new RefreshToken(
                user.TenantId, user.Id,
                Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                DateTime.UtcNow.AddDays(7));

            await _uow.Write<RefreshToken>().AddAsync(newRefreshToken, ct);
            await _uow.SaveChangesAsync();

            return ApiResponseT<AuthResponseDto>.SuccessResponse(new AuthResponseDto
            {
                AccessToken = GenerateJwtToken(user),
                RefreshToken = newRefreshToken.Token
            });
        }

        public async Task<ApiResponse> LogoutAsync(CancellationToken ct)
        {
            var userId = _currentUser.UserId;

            var tokens = await _uow.Read<RefreshToken>()
                .ListAsync(r => r.UserId == userId && !r.IsRevoked, ct);

            foreach (var token in tokens)
            {
                token.Revoke();
                await _uow.Write<RefreshToken>().UpdateAsync(token, ct);
            }

            await _uow.SaveChangesAsync();
            return ApiResponse.SuccessResponse(message: _localizer["LogoutSuccess"]);
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("SubscriptionPlan", user.SubscriptionPlan.ToString()),
                new Claim("TenantId", user.TenantId.ToString())
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_configuration["JwtSettings:ExpireMinutes"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
