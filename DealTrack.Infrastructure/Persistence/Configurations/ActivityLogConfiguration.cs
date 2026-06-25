using DealTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace DealTrack.Infrastructure.Persistence.Configurations
{
    public class ActivityLogConfiguration : BaseEntityConfiguration<ActivityLog>
    {
        public override void Configure(EntityTypeBuilder<ActivityLog> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Action).IsRequired().HasMaxLength(500);
            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.EntityId).IsRequired(false);

            builder.ToTable("ActivityLogs");
        }
    }
}
