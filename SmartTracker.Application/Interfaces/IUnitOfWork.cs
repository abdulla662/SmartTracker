using SmartTracker.Domain.Common;

namespace SmartTracker.Application.Interfaces
{
    public interface IUnitOfWork
    {
        IReadRepository<T> Read<T>() where T : class;
        IWriteRepository<T> Write<T>() where T : class;
        ISoftDeleteRepository<T> SoftDelete<T>() where T : BaseEntity;
        Task<int> SaveChangesAsync();
    }

}
