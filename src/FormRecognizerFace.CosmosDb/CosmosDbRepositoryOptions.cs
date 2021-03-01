namespace FormRecognizerFace.CosmosDb
{
    using System.Collections.Generic;

    public class CosmosDbRepositoryOptions
    {
        public string Endpoint { get; set; }

        public string AuthKey { get; set; }

        public int MaxRetriesOnThrottling { get; set; }

        public int MaxRetryWaitTimeInSeconds { get; set; }

        public string Database { get; set; }

        public IEnumerable<string> CollectioNames { get; set; }
    }
}