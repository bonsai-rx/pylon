using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
