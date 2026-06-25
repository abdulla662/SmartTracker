using DealTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace DealTrack.Infrastructure.Persistence.Configurations
{
    public class ClientFinancialSummaryConfiguration : BaseEntityConfiguration<ClientFinancialSummary>
    {
        public override void Configure(EntityTypeBuilder<ClientFinancialSummary> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.TotalAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.PaidAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Ignore(x => x.Remaining); 

            builder.Property(x => x.Status).HasConversion<string>();

            builder.HasOne<Client>()
                .WithOne()
                .HasForeignKey<ClientFinancialSummary>(x => x.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable("ClientFinancialSummaries");
        }
    }
}
