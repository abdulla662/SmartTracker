using Microsoft.EntityFrameworkCore;
using SmartTracker.Application.Interfaces;
using SmartTracker.Infrastructure.Persistence;

namespace SmartTracker.Infrastructure.Repositories
{
    public class WriteRepository<T> : IWriteRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public WriteRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        // =======================
        // Add
        // =======================

        public async Task AddAsync(T entity)
            => await _dbSet.AddAsync(entity);

        public async Task AddAsync(T entity, CancellationToken ct = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity, ct);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            await _dbSet.AddRangeAsync(entities, ct);
        }

        // =======================
        // Update
        // =======================

        public void Update(T entity)
            => _dbSet.Update(entity);

        public async Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            _dbSet.UpdateRange(entities);
            await Task.CompletedTask;
        }

        // =======================
        // Delete Note Ya Hosny This is hard Delete Not Use It Too Much
        // =======================

        public void Delete(T entity)
            => _dbSet.Remove(entity);

        public async Task DeleteAsync(T entity, CancellationToken ct = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Remove(entity);
            await Task.CompletedTask;
        }
    }
}
