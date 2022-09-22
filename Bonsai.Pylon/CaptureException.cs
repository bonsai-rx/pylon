using System;
using System.Runtime.Serialization;

namespace Bonsai.Pylon
{
    [Serializable]
    public class CaptureException : Exception
    {
        public CaptureException()
        {
        }

        public CaptureException(string message)
            : base(message)
        {
        }

        public CaptureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CaptureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
