using DealTrack.Application.Common;
using DealTrack.Application.DTOs;
using DealTrack.Application.DTOs.Auth;
using DealTrack.Application.DTOs.Auth.Forget_Password;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IAuthService
    {
        Task<ApiResponse> RegisterAsync(RegisterDto request);
        Task<ApiResponseT<AuthResponseDto>> LoginAsync(LoginDto request);
        Task<ApiResponseT<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken ct);
        Task<ApiResponse> LogoutAsync(CancellationToken ct);
        Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct);
        Task<ApiResponse> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct);
    }
}
