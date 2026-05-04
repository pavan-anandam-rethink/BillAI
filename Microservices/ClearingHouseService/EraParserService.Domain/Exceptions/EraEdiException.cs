using System;

namespace EraParserService.Domain.Exceptions
{
    public class EraEdiException : Exception
    {
        public EraEdiException(string msg)
            : base(msg)
        {

        }
        public EraEdiException(string msg, Exception innerException)
       : base(msg, innerException) { }
    }
}
