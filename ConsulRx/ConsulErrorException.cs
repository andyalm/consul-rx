using System;

namespace ConsulRx
{
    public class ConsulErrorException : Exception
    {
        public ConsulErrorException(QueryResult result)
        {
            Result = result;
        }

        public QueryResult Result { get; }
    }
}