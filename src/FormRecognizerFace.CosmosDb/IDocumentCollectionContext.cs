namespace FormRecognizerFace.CosmosDb
{
    using Microsoft.Azure.Cosmos;

    public interface IDocumentCollectionContext<in T>
        where T : Entity
    {
        string CollectionName { get; }

        string GenerateId(T entity);

        PartitionKey ResolvePartitionKey(string entityId);
    }
}
