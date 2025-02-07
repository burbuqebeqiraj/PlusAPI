using System.Linq.Expressions;

namespace PlusApi.Models
{
    public interface ISqlService<T> where T : class
    {
        Task<List<T>> SelectAllAsync(); 
        Task<List<T>> SelectAllByClauseAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string? includeProperties = null
        );

        Task<T> InsertAsync(T obj);
        Task<T> UpdateAsync(T obj); 
        Task<T?> SelectByIdAsync(object id); 
        Task<T?> SelectSingleAsync();
        Task<T?> DeleteAsync(object id);
    }
}
