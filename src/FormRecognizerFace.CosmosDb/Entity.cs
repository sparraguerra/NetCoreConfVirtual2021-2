namespace FormRecognizerFace.CosmosDb
{
    using Newtonsoft.Json;

    public abstract class Entity
    {
        [JsonProperty("id")]
        public virtual string Id { get; set; }
    }
}
