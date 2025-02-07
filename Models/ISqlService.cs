using System.Linq.Expressions;

namespace PlusApi.Models {
    public interface ISqlService<T> where T : class
    {
        List<T> SelectAll();
        List<T> SelectAllByClause(
            Expression<Func<T, bool>>? filter = null,  
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, 
            string? includeProperties = null 
        );
        T Insert(T obj);
        T Update(T obj);
        T? SelectById(object id);
        T? SelectSingle();
        T? Delete(object id);
    }
}