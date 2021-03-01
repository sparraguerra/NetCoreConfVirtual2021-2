using FormRecognizerFace.CosmosDb;

namespace FormRecognizerFace.WebApp
{
    public class Company : Entity
    {
        public string Name { get; set; }

        public string FormRecognizerModelId { get; set; }
    }

    public class FormRecognizerCosmosDbRepository : CosmosDbRepository<Company>
    {
        public FormRecognizerCosmosDbRepository(ICosmosDbClientFactory cosmosDbClientFactory)
            : base(cosmosDbClientFactory)
        {
        }

        public override string CollectionName => "companies";
    }
}
