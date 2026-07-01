using DealTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DealTrack.Infrastructure.Persistence.Configurations
{
    public class TenantInviteConfiguration : BaseEntityConfiguration<TenantInvite>
    {
        public override void Configure(EntityTypeBuilder<TenantInvite> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.InviteCode).IsRequired().HasMaxLength(8);
            builder.Property(x => x.Email).IsRequired().HasMaxLength(256);
            builder.Property(x => x.ExpiresAt).IsRequired();
            builder.Property(x => x.IsUsed).IsRequired();

            builder.HasOne(x => x.Tenant)
                .WithMany(t => t.TenantInvites)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable("TenantInvites");
        }
    }
}
