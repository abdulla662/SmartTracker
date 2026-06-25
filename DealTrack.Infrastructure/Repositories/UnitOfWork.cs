using DealTrack.Application.Interfaces;
using DealTrack.Domain.Common;
using DealTrack.Infrastructure.Persistence;

namespace DealTrack.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private bool _disposed;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IReadRepository<T> Read<T>() where T : class
            => new ReadRepository<T>(_context);

        public IWriteRepository<T> Write<T>() where T : class
            => new WriteRepository<T>(_context);

        public ISoftDeleteRepository<T> SoftDelete<T>() where T : BaseEntity
            => new SoftDeleteRepository<T>(_context);

        public async Task<int> SaveChangesAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(UnitOfWork));
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _context.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
