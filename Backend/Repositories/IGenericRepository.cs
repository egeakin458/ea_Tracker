using System.Linq.Expressions;

namespace ea_Tracker.Repositories
{
    /// <summary>
    /// Generic repository interface providing common CRUD operations.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public interface IGenericRepository<T> where T : class
    {
        /// <summary>
        /// Gets all entities of type T.
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Gets entities with optional filtering, ordering, and included properties.
        /// </summary>
        Task<IEnumerable<T>> GetAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            string includeProperties = "");

        /// <summary>
        /// Gets a single entity by its primary key.
        /// </summary>
        Task<T?> GetByIdAsync(object id);

        /// <summary>
        /// Gets the first entity matching the specified filter.
        /// </summary>
        Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter);

        /// <summary>
        /// Adds a new entity.
        /// </summary>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// Adds multiple entities.
        /// </summary>
        Task AddRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        void Update(T entity);

        /// <summary>
        /// Removes an entity.
        /// </summary>
        void Remove(T entity);

        /// <summary>
        /// Removes multiple entities.
        /// </summary>
        void RemoveRange(IEnumerable<T> entities);

        /// <summary>
        /// Gets the count of entities matching the filter.
        /// </summary>
        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);

        /// <summary>
        /// Checks if any entity matches the filter.
        /// </summary>
        Task<bool> AnyAsync(Expression<Func<T, bool>> filter);

        /// <summary>
        /// Saves all pending changes to the database.
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}