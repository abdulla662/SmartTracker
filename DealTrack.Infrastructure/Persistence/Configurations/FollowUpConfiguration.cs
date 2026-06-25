using DealTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DealTrack.Infrastructure.Persistence.Configurations
{
    public class FollowUpConfiguration : BaseEntityConfiguration<FollowUp>
    {
        public override void Configure(EntityTypeBuilder<FollowUp> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.FollowUpDate).IsRequired();
            builder.Property(x => x.Status).HasConversion<string>();
            builder.Property(x => x.CreatedByUserId).IsRequired();

            builder.HasOne(x => x.Client)
                .WithMany(c => c.FollowUps)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable("FollowUps");
        }
    }
}
