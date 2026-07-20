using DealTrack.Application.Common;
using DealTrack.Application.DTOs;
using DealTrack.Application.DTOs.Auth;
using DealTrack.Application.DTOs.Auth.Forget_Password;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<ApiResponse> RegisterAsync(RegisterDto request)
            => await _authService.RegisterAsync(request);

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<ApiResponseT<AuthResponseDto>> LoginAsync(LoginDto request)
            => await _authService.LoginAsync(request);

        [HttpPost("refresh")]
        public async Task<ApiResponseT<AuthResponseDto>> RefreshToken(RefreshTokenDto dto, CancellationToken ct)
            => await _authService.RefreshTokenAsync(dto, ct);

        [HttpPost("logout")]
        [Authorize]
        public async Task<ApiResponse> Logout(CancellationToken ct)
            => await _authService.LogoutAsync(ct);

        [HttpGet("check-company")]
        [EnableRateLimiting("public-lookup")]
        public async Task<ApiResponseT<CompanyCheckResult>> CheckCompany([FromQuery] string name)
            => await _authService.CheckCompanyAsync(name);

        // Returns only whether a pending request exists — no names, roles, or company details
        [HttpGet("pending-request")]
        [EnableRateLimiting("public-lookup")]
        public async Task<ApiResponseT<PendingRequestInfoDto>> GetPendingRequest([FromQuery] string email)
            => await _authService.GetPendingRequestAsync(email);

        [HttpPost("send-withdrawal-code")]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<ApiResponse> SendWithdrawalCode(SendWithdrawalCodeDto dto)
            => await _authService.SendWithdrawalCodeAsync(dto);

        [HttpPost("cancel-pending-request")]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<ApiResponse> CancelPendingRequest(CancelPendingRequestDto dto)
            => await _authService.CancelPendingRequestAsync(dto);

        [HttpPost("forgot-password")]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<ApiResponse> ForgotPassword(ForgotPasswordDto dto, CancellationToken ct)
            => await _authService.ForgotPasswordAsync(dto, ct);

        [HttpPost("reset-password")]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<ApiResponse> ResetPassword(ResetPasswordDto dto, CancellationToken ct)
            => await _authService.ResetPasswordAsync(dto, ct);
    }
}
