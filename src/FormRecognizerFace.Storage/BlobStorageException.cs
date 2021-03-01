using System;
using System.Runtime.Serialization;

namespace FormRecognizerFace.Storage
{
    [Serializable]
    public class BlobStorageException : Exception
    {
        public BlobStorageException()
        {
        }

        public BlobStorageException(string message)
            : base(message)
        {
        }

        public BlobStorageException(string message, Exception innerExcepotion)
           : base(message, innerExcepotion)
        {
        }

        protected BlobStorageException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context) => base.GetObjectData(info, context);
    }
}
