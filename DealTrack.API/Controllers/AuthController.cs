using DealTrack.Application.Common;
using DealTrack.Application.DTOs;
using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DealTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase, IAuthService
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ApiResponse> RegisterAsync(
            RegisterDto request)
        {
            return await _authService.RegisterAsync(request);
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<ApiResponseT<string>> LoginAsync(
            LoginDto request)
        {
            return await _authService.LoginAsync(request);
        }
    }
}