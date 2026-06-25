namespace DealTrack.Application.Interfaces
{
    public interface IWriteRepository<TEntity> where TEntity : class
    {
        Task AddAsync(TEntity entity, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

        Task UpdateAsync(TEntity entity, CancellationToken ct = default);
        Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

        Task DeleteAsync(TEntity entity, CancellationToken ct = default); // hard delete
    }

}
