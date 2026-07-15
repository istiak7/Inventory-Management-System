using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;

namespace Inventory_Management_System.Shared.Repository
{
    public class BaseRepository<T>(AppDbContext context) : IBaseRepository<T> where T : BaseEntity
    {
        private readonly AppDbContext _context = context;

        #region GET

        public async Task<T?> GetAsync(
            Expression<Func<T, bool>> predicate,
            bool asNoTracking = false,
            CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _context.Set<T>();

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(predicate, cancellationToken);
        }
        public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<T>().FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<T>().ToListAsync(cancellationToken);
        }

        #endregion

        #region POST
        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _context.Set<T>().AddAsync(entity, cancellationToken);
        }

        public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            _context.Set<T>().Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<T>().Remove(entity);

            }
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        #endregion
    }
}
