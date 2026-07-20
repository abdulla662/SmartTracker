using DealTrack.Application.Helpers;
using DealTrack.Application.Interfaces;
using DealTrack.Application.ServicesInterfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;

namespace DealTrack.Infrastructure.BackgroundJobs
{
    public class MarkMissedFollowUpsJob
    {
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notifications;

        public MarkMissedFollowUpsJob(IUnitOfWork uow, INotificationService notifications)
        {
            _uow = uow;
            _notifications = notifications;
        }

        public async Task ExecuteAsync()
        {
            var missedFollowUps = await _uow.Read<FollowUp>()
                .ListAsync(f => f.FollowUpDate < DateTime.UtcNow
                             && f.Status == FollowUpStatus.Pending);

            foreach (var followUp in missedFollowUps)
            {
                followUp.MarkMissed();
                await _uow.Write<FollowUp>().UpdateAsync(followUp);

                await _notifications.CreateAsync(
                    followUp.CreatedByUserId,
                    followUp.TenantId,
                    NotifKey.Build("notif.title.missedFollowUp"),
                    NotifKey.Build("notif.msg.missedFollowUp", followUp.FollowUpDate.ToString("yyyy-MM-dd HH:mm")),
                    NotificationType.FollowUpReminder);
            }

            await _uow.SaveChangesAsync();
        }
    }
}