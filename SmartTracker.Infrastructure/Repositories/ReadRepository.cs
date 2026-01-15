using Microsoft.EntityFrameworkCore;
using SmartTracker.Application.Interfaces;
using SmartTracker.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace SmartTracker.Infrastructure.Repositories
{
    public class ReadRepository<T> : IReadRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public ReadRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        // =======================
        // Get by Id
        // =======================

        public async Task<T?> GetByIdAsync(Guid id)
            => await _dbSet.FindAsync(id);

        public async Task<T?> GetByIdAsync(object id, CancellationToken ct = default)
            => await _dbSet.FindAsync(new[] { id }, ct);

        // =======================
        // Single
        // =======================

        public async Task<T?> GetSingleAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken ct = default)
            => await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(predicate, ct);

        // =======================
        // List
        // =======================

        public async Task<IReadOnlyList<T>> GetAllAsync()
            => await _dbSet
                .AsNoTracking()
                .ToListAsync();

        public async Task<List<T>> ListAsync(CancellationToken ct = default)
            => await _dbSet
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<List<T>> ListAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken ct = default)
            => await _dbSet
                .AsNoTracking()
                .Where(predicate)
                .ToListAsync(ct);

        public async Task<IReadOnlyList<T>> FindAsync(
            Expression<Func<T, bool>> predicate)
            => await _dbSet
                .AsNoTracking()
                .Where(predicate)
                .ToListAsync();

        // =======================
        // Projection
        // =======================

        public async Task<List<TProjected>> ListAsync<TProjected>(
            Expression<Func<T, TProjected>> selector,
            CancellationToken ct = default)
            => await _dbSet
                .AsNoTracking()
                .Select(selector)
                .ToListAsync(ct);

        public async Task<List<TProjected>> ListAsync<TProjected>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TProjected>> selector,
            CancellationToken ct = default)
            => await _dbSet
                .AsNoTracking()
                .Where(predicate)
                .Select(selector)
                .ToListAsync(ct);

        // =======================
        // Streaming (Large Data)
        // =======================

        public IAsyncEnumerable<TProjected> StreamAsync<TProjected>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TProjected>> selector)
            => _dbSet
                .AsNoTracking()
                .Where(predicate)
                .Select(selector)
                .AsAsyncEnumerable();

        // =======================
        // Utilities
        // =======================

        public async Task<bool> AnyAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken ct = default)
            => await _dbSet.AnyAsync(predicate, ct);

        public async Task<int> CountAsync(CancellationToken ct = default)
            => await _dbSet.CountAsync(ct);

        public async Task<int> CountAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken ct = default)
            => await _dbSet.CountAsync(predicate, ct);

        // =======================
        // Query (Advanced cases)
        // =======================

        public IQueryable<T> Query()
            => _dbSet.AsQueryable();
    }
}
