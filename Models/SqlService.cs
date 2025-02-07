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

        public List<T> SelectAll()
        {
            return _entity.ToList();
        }

        public virtual List<T> SelectAllByClause(
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

            return orderBy != null ? orderBy(query).ToList() : query.ToList();
        }

        public T? SelectById(object id)
        {
            return _entity.Find(id);
        }

        public T? SelectSingle()
        {
            return _entity.SingleOrDefault();
        }

        public T Insert(T obj)
        {
            _entity.Add(obj);
            _db.SaveChanges();
            return obj;
        }

        public T Update(T obj)
        {
            _db.Entry(obj).State = EntityState.Modified;
            _db.SaveChanges();
            return obj;
        }

        public T? Delete(object id)
        {
            var existing = _entity.Find(id);
            if (existing != null)
            {
                _entity.Remove(existing);
                _db.SaveChanges();
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
