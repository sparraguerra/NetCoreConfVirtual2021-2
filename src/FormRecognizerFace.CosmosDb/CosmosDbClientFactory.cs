namespace FormRecognizerFace.CosmosDb
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Cosmos;
    using Microsoft.Extensions.Options;

    public class CosmosDbClientFactory : ICosmosDbClientFactory
    {
        readonly IEnumerable<string> collectionNames;
        readonly CosmosDbRepositoryOptions configuration;

        public CosmosDbClientFactory(IOptions<CosmosDbRepositoryOptions> configuration)
            : this(configuration.Value)
        {
        }

        public CosmosDbClientFactory(CosmosDbRepositoryOptions configuration)
        {
            this.configuration = configuration ?? throw new ArgumentException(nameof(configuration));
            collectionNames = configuration.CollectioNames ?? throw new ArgumentException("collectioNames");
            Database = configuration.Database ?? throw new ArgumentException("database");
        }

        public string Database { get; set; }

        public CosmosClient GetClient(string collectionName)
        {
            if (!collectionNames.Contains(collectionName))
            {
                throw new ArgumentException($"Unable to find collection: {collectionName}");
            }

            var clientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Direct,
                MaxRetryAttemptsOnRateLimitedRequests = configuration.MaxRetriesOnThrottling,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(configuration.MaxRetryWaitTimeInSeconds)
            };
            return new CosmosClient(configuration.Endpoint, configuration.AuthKey, clientOptions);
        }
    }
}
