using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace PlusApi.Models
{
    public class SqlService<T> : ISqlService<T>, IDisposable where T : class
    {
        private readonly DbContext _db;
        private readonly DbSet<T> _entity;

        public SqlService(AppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _entity = db.Set<T>() ?? throw new InvalidOperationException("DbSet cannot be null.");
        }

        public async Task<List<T>> SelectAllAsync()
        {
            return await _entity.ToListAsync(); 
        }

        public async Task<List<T>> SelectAllByClauseAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string? includeProperties = null)
        {
            IQueryable<T> query = _entity;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                query = includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
            }

            return orderBy != null ? await orderBy(query).ToListAsync() : await query.ToListAsync(); // Asynchronous fetch
        }

        public async Task<T?> SelectByIdAsync(object id)
        {
            return await _entity.FindAsync(id); 
        }

        public async Task<T?> SelectSingleAsync()
        {
            return await _entity.SingleOrDefaultAsync();
        }

        public async Task<T> InsertAsync(T obj)
        {
            _entity.Add(obj);
            await _db.SaveChangesAsync(); 
            return obj;
        }

        public async Task<T> UpdateAsync(T obj)
        {
            _db.Entry(obj).State = EntityState.Modified;
            await _db.SaveChangesAsync(); 
            return obj;
        }

        public async Task<T?> DeleteAsync(object id)
        {
            var existing = await _entity.FindAsync(id); 
            if (existing != null)
            {
                _entity.Remove(existing);
                await _db.SaveChangesAsync(); 
            }
            return existing;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
