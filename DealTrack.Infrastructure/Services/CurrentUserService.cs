using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Enums;
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

        public UserRole Role
        {
            get
            {
                var claim = User?.FindFirstValue(ClaimTypes.Role);
                return Enum.TryParse<UserRole>(claim, out var role) ? role : UserRole.Sales;
            }
        }

        public SubscriptionPlan SubscriptionPlan
        {
            get
            {
                var claim = User?.FindFirstValue("SubscriptionPlan");
                return Enum.TryParse<SubscriptionPlan>(claim, out var plan) ? plan : SubscriptionPlan.Free;
            }
        }

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
