using System.Linq.Expressions;
namespace SmartTracker.Application.Interfaces
{
    public interface IReadRepository<TEntity> where TEntity : class
    {
        // Basic reads
        Task<TEntity?> GetByIdAsync(object id, CancellationToken ct = default);
        Task<TEntity?> GetSingleAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken ct = default);

        // Lists
        Task<List<TEntity>> ListAsync(CancellationToken ct = default);
        Task<List<TEntity>> ListAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken ct = default);

        // Projection
        Task<List<TProjected>> ListAsync<TProjected>(
            Expression<Func<TEntity, TProjected>> selector,
            CancellationToken ct = default);

        Task<List<TProjected>> ListAsync<TProjected>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TProjected>> selector,
            CancellationToken ct = default);

        // Streaming (large data)
        IAsyncEnumerable<TProjected> StreamAsync<TProjected>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TProjected>> selector);

        // Existence & count
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
        Task<int> CountAsync(CancellationToken ct = default);
        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    }

}
