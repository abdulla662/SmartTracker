using DealTrack.Domain.Common;

namespace DealTrack.Application.Interfaces
{
    public interface ISoftDeleteRepository<TEntity>
      where TEntity : BaseEntity
    {
        Task SoftDeleteAsync(TEntity entity, CancellationToken ct = default);
        Task SoftDeleteByIdAsync(Guid id, CancellationToken ct = default);
        Task SoftDeleteRange(IEnumerable<TEntity> entities, CancellationToken ct);
        Task RestoreAsync(TEntity entity, CancellationToken ct = default); //1....10000
        Task RestoreRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default); //1...10000
    }
}
