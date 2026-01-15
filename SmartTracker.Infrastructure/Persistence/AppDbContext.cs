using Microsoft.EntityFrameworkCore;
using SmartTracker.Application.Common;
using SmartTracker.Domain.Entities;

namespace SmartTracker.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        private RequestFilterContext? _ctx;

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Product> Products => Set<Product>();

        public void ApplyFilter(RequestFilterContext ctx)
        {
            _ctx = ctx;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
      .HasQueryFilter(p =>
          _ctx == null || !_ctx.ApplySoftDeleteFilter || !p.IsDeleted
      );

        }
    }
}
