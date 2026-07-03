using DealTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DealTrack.Infrastructure.Persistence.Configurations
{
    public class TransferRequestConfiguration : BaseEntityConfiguration<TransferRequest>
    {
        public override void Configure(EntityTypeBuilder<TransferRequest> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.SalesUserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.FromTeamLeadId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ToTeamLeadId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Status).HasConversion<string>();

            builder.ToTable("TransferRequests");
        }
    }
}