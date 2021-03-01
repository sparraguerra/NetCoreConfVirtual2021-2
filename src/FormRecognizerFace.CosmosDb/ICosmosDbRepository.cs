namespace FormRecognizerFace.CosmosDb
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public interface ICosmosDbRepository<TEntity>
        where TEntity : Entity
    {
        Task<TEntity> AddAsync(TEntity entity);

        Task<TEntity> ExecuteStoredProcedureAsync(string storedProcedure, string partitionKey, dynamic[] procedureParams);

        Task<IEnumerable<TEntity>> GetAllAsync();

        Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate);

        Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, dynamic>> order, bool descending);

        Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, dynamic>> order, bool descending, int take);

        Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, dynamic>> order, bool descending, int take, int skip);

        Task<TEntity> GetByIdAsync(string itemId);

        Task<bool> RemoveAsync(string itemId);

        Task<bool> UpdateAsync(TEntity entity);
    }
}
