using DealTrack.Application.Interfaces;
using DealTrack.Domain.Entities;
using DealTrack.Domain.Enums;

namespace DealTrack.Infrastructure.BackgroundJobs
{
    public class MarkMissedFollowUpsJob
    {
        private readonly IUnitOfWork _uow;

        public MarkMissedFollowUpsJob(IUnitOfWork uow)
        {
            _uow = uow;
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
            }

            await _uow.SaveChangesAsync();
        }
    }
}