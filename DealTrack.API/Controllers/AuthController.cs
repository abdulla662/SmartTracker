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

        [HttpPost("forgot-password")]
        public async Task<ApiResponse> ForgotPassword(ForgotPasswordDto dto, CancellationToken ct)
    => await _authService.ForgotPasswordAsync(dto, ct);

        [HttpPost("reset-password")]
        public async Task<ApiResponse> ResetPassword(ResetPasswordDto dto, CancellationToken ct)
            => await _authService.ResetPasswordAsync(dto, ct);
    }
}
