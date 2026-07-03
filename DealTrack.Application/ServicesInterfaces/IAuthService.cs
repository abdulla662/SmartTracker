using DealTrack.Application.Common;
using DealTrack.Application.DTOs;
using DealTrack.Application.DTOs.Auth;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IAuthService
    {
        Task<ApiResponse> RegisterAsync(RegisterDto request);
        Task<ApiResponseT<AuthResponseDto>> LoginAsync(LoginDto request);
        Task<ApiResponseT<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken ct);
        Task<ApiResponse> LogoutAsync(CancellationToken ct);
    }
}
