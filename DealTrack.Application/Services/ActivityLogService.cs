using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;

namespace DealTrack.Application.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public ActivityLogService(IUnitOfWork uow, ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task LogAsync(string action, Guid? entityId = null, string? entityType = null, CancellationToken ct = default)
        {
            var log = new ActivityLog(
                _currentUser.TenantId,
                Guid.Parse(_currentUser.UserId),
                action,
                entityId,
                entityType);

            await _uow.Write<ActivityLog>().AddAsync(log, ct);
            await _uow.SaveChangesAsync();
        }
    }
}