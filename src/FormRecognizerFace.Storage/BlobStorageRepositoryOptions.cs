namespace FormRecognizerFace.Storage
{
    public class BlobStorageRepositoryOptions
    {
        public string ConnectionString { get; set; }
        public int MinutesSasExpire { get; set; }
    }
}
