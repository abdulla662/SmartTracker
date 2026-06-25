using DealTrack.Domain.Common;

namespace DealTrack.Application.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IReadRepository<T> Read<T>() where T : class;
        IWriteRepository<T> Write<T>() where T : class;
        ISoftDeleteRepository<T> SoftDelete<T>() where T : BaseEntity;
        Task<int> SaveChangesAsync();
    }

}
