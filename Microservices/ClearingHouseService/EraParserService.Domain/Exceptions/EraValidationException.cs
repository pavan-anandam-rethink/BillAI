using System;

namespace EraParserService.Domain.Exceptions
{
    public class EraValidationException : Exception
    {
        public EraValidationException(string msg)
            : base(msg)
        {

        }
    }
}
