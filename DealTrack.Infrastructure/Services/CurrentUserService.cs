using DealTrack.Application.ServicesInterfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DealTrack.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public string UserId =>
            User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        public string UserName =>
            User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        public string Role =>
            User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        public string SubscriptionPlan =>
            User?.FindFirstValue("SubscriptionPlan") ?? string.Empty;

        public Guid TenantId
        {
            get
            {
                var claim = User?.FindFirstValue("TenantId");
                return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
            }
        }

        public bool IsAuthenticated =>
            User?.Identity?.IsAuthenticated ?? false;
    }
}
