namespace FormRecognizerFace.CosmosDb
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;

    public abstract class CosmosDbRepository<TEntity> : ICosmosDbRepository<TEntity>, IDocumentCollectionContext<TEntity>
        where TEntity : Entity
    {
        readonly Container container;

        protected CosmosDbRepository(ICosmosDbClientFactory cosmosDbClientFactory)
        {
            if (cosmosDbClientFactory == null)
            {
                throw new ArgumentException("cosmosDbClientFactory");
            }
            if (string.IsNullOrWhiteSpace(CollectionName))
            {
                throw new ArgumentException("collectionName");
            }

            container = cosmosDbClientFactory.GetClient(CollectionName).GetContainer(cosmosDbClientFactory.Database, CollectionName);
        }

        public abstract string CollectionName { get; }

        public virtual string GenerateId(TEntity entity) => Guid.NewGuid().ToString();

        public virtual PartitionKey ResolvePartitionKey(string entityId)
        {
            var partition = default(PartitionKey);
            if (entityId != null)
            {
                partition = new PartitionKey(entityId);
            }

            return partition;
        }

        public async Task<TEntity> AddAsync(TEntity entity)
        {
            try
            {
                return await container.CreateItemAsync<TEntity>(entity);
            }
            catch (Exception ex)
            {
                throw new CosmosDbException($"{entity} - Error while adding or updating a document.", ex);
            }
        }

        public async Task<TEntity> ExecuteStoredProcedureAsync(string storedProcedure, string partitionKey, dynamic[] procedureParams)
        {
            try
            {
                return await container.Scripts.ExecuteStoredProcedureAsync<TEntity>(storedProcedure, ResolvePartitionKey(partitionKey), procedureParams);
            }
            catch (CosmosException ex)
            {
                throw new CosmosDbException($"{storedProcedure} - Error whilesexecuting stored procedure", ex);
            }
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync() => await Task.Run(() => Get(GetAll()));

        public async Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate)
                    => await Task.Run(() => Get(GetAll(predicate)));

        public async Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, dynamic>> order, bool descending)
                    => await Task.Run(() => Get(GetAll(predicate, order, descending)));

        public async Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, dynamic>> order, bool descending, int take)
                    => await Task.Run(() => Get(GetAll(predicate, order, descending, take)));

        public async Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, dynamic>> order, bool descending, int take, int skip)
                    => await Task.Run(() => Get(GetAll(predicate, order, descending, take, skip)));

        public async Task<TEntity> GetByIdAsync(string itemId)
        {
            try
            {
                return await container.ReadItemAsync<TEntity>(itemId, ResolvePartitionKey(itemId));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }
            catch (CosmosException ex)
            {
                throw new CosmosDbException($"{itemId} - Error while getting a document by id", ex);
            }
        }

        public async Task<bool> RemoveAsync(string itemId)
        {
            try
            {
                await container.DeleteItemAsync<TEntity>(itemId, ResolvePartitionKey(itemId));
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (CosmosException ex)
            {
                throw new CosmosDbException($"{itemId} - Error while getting a document by id", ex);
            }
        }

        public async Task<bool> UpdateAsync(TEntity entity)
        {
            try
            {
                _ = await container.UpsertItemAsync<TEntity>(entity);
                return true;
            }
            catch (Exception ex)
            {
                throw new CosmosDbException($"{entity} - Error while adding or updating a document.", ex);
            }
        }

        IOrderedQueryable<TEntity> GetAll() => container.GetItemLinqQueryable<TEntity>(true);

        IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate) => GetAll().Where(predicate);

        IOrderedQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, dynamic>> order, bool descending)
        {
            if (descending)
            {
                return GetAll(predicate).OrderByDescending(order);
            }
            else
            {
                return GetAll(predicate).OrderBy(order);
            }
        }

        IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, dynamic>> order, bool descending, int take) => GetAll(predicate, order, descending).Take(take);

        IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, dynamic>> order, bool descending, int take, int skip) => GetAll(predicate, order, descending, take).Skip(skip);

        IEnumerable<TEntity> Get(IQueryable<TEntity> query)
        {
            var results = new List<TEntity>();
            results.AddRange(query);

            return results;
        }
    }
}
