namespace FormRecognizerFace.CosmosDb
{
    using Microsoft.Azure.Cosmos;

    public interface ICosmosDbClientFactory
    {
        string Database { get; set; }

        CosmosClient GetClient(string collectionName);
    }
}
