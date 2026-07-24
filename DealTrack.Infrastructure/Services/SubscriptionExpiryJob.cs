using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;
using DealTrack.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DealTrack.Infrastructure.Services
{
    public class SubscriptionExpiryJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionExpiryJob> _logger;

        public SubscriptionExpiryJob(IServiceScopeFactory scopeFactory, ILogger<SubscriptionExpiryJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await DowngradeExpiredTenantsAsync(stoppingToken);

                // Run once per day
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task DowngradeExpiredTenantsAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var expired = await db.Set<Tenant>()
                    .Where(t => t.Plan != SubscriptionPlan.Free
                             && t.PlanExpiresAt != null
                             && t.PlanExpiresAt <= DateTime.UtcNow)
                    .ToListAsync(ct);

                foreach (var tenant in expired)
                {
                    tenant.UpdatePlan(SubscriptionPlan.Free, expiresAt: null);

                    var users = await userManager.Users
                        .Where(u => u.TenantId == tenant.Id)
                        .ToListAsync(ct);

                    foreach (var user in users)
                        user.SubscriptionPlan = SubscriptionPlan.Free;

                    _logger.LogInformation("Tenant {TenantId} downgraded to Free — plan expired.", tenant.Id);
                }

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubscriptionExpiryJob failed.");
            }
        }
    }
}
