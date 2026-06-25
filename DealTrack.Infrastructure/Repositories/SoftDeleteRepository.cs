using Microsoft.EntityFrameworkCore;
using DealTrack.Application.Interfaces;
using DealTrack.Domain.Common;
using DealTrack.Infrastructure.Persistence;

namespace DealTrack.Infrastructure.Repositories
{
    public class SoftDeleteRepository<T> : ISoftDeleteRepository<T>
        where T : BaseEntity
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public SoftDeleteRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        // =======================
        // Soft Delete
        // =======================

        public async Task SoftDeleteAsync(T entity, CancellationToken ct = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.SoftDelete();
            _dbSet.Update(entity);

            await Task.CompletedTask;
        }

        public async Task SoftDeleteByIdAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _dbSet.FindAsync(new object[] { id }, ct);
            if (entity == null)
                return;

            entity.SoftDelete();
            _dbSet.Update(entity);
        }

        public async Task SoftDeleteRange(IEnumerable<T> entities, CancellationToken ct = default)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            foreach (var entity in entities)
            {
                entity.SoftDelete();
            }

            _dbSet.UpdateRange(entities);
            await Task.CompletedTask;
        }

        // =======================
        // Restore
        // =======================

        public async Task RestoreAsync(T entity, CancellationToken ct = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.Restore();
            _dbSet.Update(entity);

            await Task.CompletedTask;
        }

        public async Task RestoreRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            foreach (var entity in entities)
            {
                entity.Restore();
            }

            _dbSet.UpdateRange(entities);
            await Task.CompletedTask;
        }
    }
}
