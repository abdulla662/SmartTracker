using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace DealTrack.Infrastructure.Authorization
{
    public class PlanRequirement : IAuthorizationRequirement
    {
        public string[] AllowedPlans { get; }
        public PlanRequirement(params string[] allowedPlans) => AllowedPlans = allowedPlans;
    }

    public class PlanRequirementHandler : AuthorizationHandler<PlanRequirement>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public PlanRequirementHandler(UserManager<ApplicationUser> userManager)
            => _userManager = userManager;

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PlanRequirement requirement)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return;

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return;

            if (requirement.AllowedPlans.Contains(user.SubscriptionPlan.ToString(), StringComparer.OrdinalIgnoreCase))
                context.Succeed(requirement);
        }
    }
}
