namespace FormRecognizerFace.CosmosDb
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class CosmosDbException : Exception
    {
        public CosmosDbException()
        {
        }

        public CosmosDbException(string message)
            : base(message)
        {
        }

        public CosmosDbException(string message, Exception innerExcepotion)
           : base(message, innerExcepotion)
        {
        }

        protected CosmosDbException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context) => base.GetObjectData(info, context);
    }
}
