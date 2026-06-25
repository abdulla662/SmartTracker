using DealTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace DealTrack.Infrastructure.Persistence.Configurations
{
    public class NotificationConfiguration : BaseEntityConfiguration<Notification>
    {
        public override void Configure(EntityTypeBuilder<Notification> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Message).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.Type).HasConversion<string>();
            builder.Property(x => x.IsRead).HasDefaultValue(false);
            builder.Property(x => x.UserId).IsRequired();

            builder.ToTable("Notifications");
        }
    }
}
