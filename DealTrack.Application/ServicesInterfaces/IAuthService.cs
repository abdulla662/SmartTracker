using DealTrack.Application.Common;
using DealTrack.Application.DTOs;


namespace DealTrack.Application.ServicesInterfaces
{
    public interface IAuthService
    {
        Task<ApiResponse> RegisterAsync(RegisterDto request);

        Task<ApiResponseT<string>> LoginAsync(LoginDto request);
    }
}
