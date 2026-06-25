using DealTrack.Application.Common;
using DealTrack.Application.DTOs;
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

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IUnitOfWork uow,
            IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _configuration = configuration;
            _uow = uow;
            _localizer = localizer;
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
                SubscriptionPlan.Free or SubscriptionPlan.Individual => UserRole.Sales,
                SubscriptionPlan.Team => UserRole.TeamLead,
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

        public async Task<ApiResponseT<string>> LoginAsync(LoginDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return ApiResponseT<string>.FailureResponse(_localizer["InvalidCredentials"]);

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
                return ApiResponseT<string>.FailureResponse(_localizer["InvalidCredentials"]);

            var token = GenerateJwtToken(user);
            return ApiResponseT<string>.SuccessResponse(token, _localizer["LoginSuccess"]);
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
