using Microsoft.EntityFrameworkCore;
using SmartTracker.Application.Interfaces;
using SmartTracker.Domain.Common;
using SmartTracker.Infrastructure.Persistence;

namespace SmartTracker.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IReadRepository<T> Read<T>() where T : class
            => new ReadRepository<T>(_context);

        public IWriteRepository<T> Write<T>() where T : class
            => new WriteRepository<T>(_context);

        public ISoftDeleteRepository<T> SoftDelete<T>()
            where T : BaseEntity
            => new SoftDeleteRepository<T>(_context);

        public async Task<int> SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }

}
