using System.Linq.Expressions;
using Inventory_Management_System.Entities;

namespace Inventory_Management_System.Shared.Repository
{
    public interface IBaseRepository<T> where T : BaseEntity
    {
        Task<T?> GetAsync(Expression<Func<T, bool>> predicate,bool asNoTracking = false,
       CancellationToken cancellationToken = default);
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task AddAsync(T entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
